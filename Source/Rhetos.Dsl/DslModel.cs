/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Autofac.Features.Indexed;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DslModel : IDslModel
    {
        private readonly IDslParser _dslParser;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly ILogger _evaluatorsOrderLogger;
        private readonly ILogger _dslModelConceptsLogger;
        private readonly DslContainer _dslContainer;
        private readonly IIndex<Type, IEnumerable<IConceptMacro>>  _macros;
        private readonly IEnumerable<Type> _macroTypes;
        private readonly IEnumerable<Type> _conceptTypes;
        private readonly IMacroOrderRepository _macroOrderRepository;
        private readonly IDslModelFile _dslModelFile;

        public DslModel(
            IDslParser dslParser,
            ILogProvider logProvider,
            DslContainer dslContainer,
            IIndex<Type, IEnumerable<IConceptMacro>> macros,
            IEnumerable<IConceptMacro> macroPrototypes,
            IEnumerable<IConceptInfo> conceptPrototypes,
            IMacroOrderRepository macroOrderRepository,
            IDslModelFile dslModelFile)
        {
            _dslParser = dslParser;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslModel");
            _evaluatorsOrderLogger = logProvider.GetLogger("MacroEvaluatorsOrder");
            _dslModelConceptsLogger = logProvider.GetLogger("DslModelConcepts");
            _dslContainer = dslContainer;
            _macros = macros;
            _macroTypes = macroPrototypes.Select(macro => macro.GetType());
            _conceptTypes = conceptPrototypes.Select(conceptInfo => conceptInfo.GetType());
            _macroOrderRepository = macroOrderRepository;
            _dslModelFile = dslModelFile;
        }

        #region IDslModel implementation

        public IEnumerable<IConceptInfo> Concepts
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _dslContainer.Concepts;
            }
        }

        public IConceptInfo FindByKey(string conceptKey)
        {
            if (!_initialized)
                Initialize();
            return _dslContainer.FindByKey(conceptKey);
        }

        public IEnumerable<IConceptInfo> FindByType(Type conceptType)
        {
            if (!_initialized)
                Initialize();
            return _dslContainer.FindByType(conceptType);
        }

        public T GetIndex<T>() where T : IDslModelIndex
        {
            if (!_initialized)
                Initialize();
            return _dslContainer.GetIndex<T>();
        }

        #endregion

        private bool _initialized;
        private readonly object _initializationLock = new object();

        private void Initialize()
        {
            if (!_initialized)
                lock (_initializationLock)
                    if (!_initialized)
                    {
                        var sw = Stopwatch.StartNew();

                        var parsedConcepts = _dslParser.ParsedConcepts;
                        _dslContainer.AddNewConceptsAndReplaceReferences(parsedConcepts);
                        ExpandMacroConcepts();
                        _dslContainer.ReportErrorForUnresolvedConcepts();
                        CheckSemantics();
                        _dslContainer.SortReferencesBeforeUsingConcept();
                        _performanceLogger.Write(sw, "DslModel.Initialize (" + _dslContainer.Concepts.Count() + " concepts).");

                        LogDslModel();
                        _dslModelFile.SaveConcepts(_dslContainer.Concepts);
                        _initialized = true;
                    }
        }

        private void LogDslModel()
        {
            var sw = Stopwatch.StartNew();

            // It is important to avoid generating XMLs if the logger is not enabled.
            var xmlUtility = new Lazy<XmlUtility>(() => new XmlUtility(null));
            var sortedConceptsXml = new Lazy<List<string>>(() => _dslContainer.Concepts
                .Select(c => xmlUtility.Value.SerializeToXml(c, c.GetType()))
                .OrderBy(xml => xml).ToList());

            const int chunkSize = 10000; // Keeping message size under NLog memory limit.
            for (int start = 0; start < _dslContainer.Concepts.Count(); start += chunkSize)
                _dslModelConceptsLogger.Trace(() => string.Join("\r\n", sortedConceptsXml.Value.Skip(start).Take(chunkSize)));

            _performanceLogger.Write(sw, "DslModel.LogDslModel.");
        }

        private const int MacroIterationLimit = 200;

        private class MacroEvaluator
        {
            public string Name;
            public Func<IConceptInfo, IDslModel, IEnumerable<IConceptInfo>> Evaluate;
            public Type Implements;
            public bool ImplementsDerivations;
        }

        private void ExpandMacroConcepts()
        {
            var swTotal = Stopwatch.StartNew();
            var sw = Stopwatch.StartNew();

            int iteration = 0;
            var iterationCreatedConcepts = new List<IConceptInfo>();
            int lastResolvedConceptTime = 0;
            var lastResolvedConceptTimeByIteration = new List<int>();
            var lastResolvedConceptTimeByMacro = new Dictionary<string, int>();
            var recommendedMacroOrder = _macroOrderRepository.Load().ToDictionary(m => m.EvaluatorName, m => m.EvaluatorOrder);
            var macroEvaluators = ListMacroEvaluators(recommendedMacroOrder);
            var macroStopwatches = macroEvaluators.ToDictionary(macro => macro.Name, macro => new Stopwatch());
            _performanceLogger.Write(sw, "DslModel.ExpandMacroConcepts initialization ("
                + macroEvaluators.Count + " evaluators, "
                + _dslContainer.Concepts.Count() + " parsed concepts resolved, "
                + _dslContainer.UnresolvedConceptsCount() + " unresolved).");

            do
            {
                iteration++;
                if (iteration > MacroIterationLimit)
                    throw new DslSyntaxException(string.Format(
                        "Possible infinite loop detected with recursive macro concept {1}. Iteration limit ({0}) exceeded while expanding macro.",
                        MacroIterationLimit,
                        iterationCreatedConcepts.First().GetShortDescription()));

                iterationCreatedConcepts.Clear();
                _logger.Trace("Expanding macro concepts, iteration {0}.", iteration);

                foreach (var macroEvaluator in macroEvaluators)
                {
                    macroStopwatches[macroEvaluator.Name].Start();
                    foreach (var conceptInfo in _dslContainer.FindByType(macroEvaluator.Implements, macroEvaluator.ImplementsDerivations).ToList())
                    {
                        var macroCreatedConcepts = macroEvaluator.Evaluate(conceptInfo, _dslContainer);
                        CsUtility.Materialize(ref macroCreatedConcepts);

                        if (macroCreatedConcepts != null && macroCreatedConcepts.Count() > 0)
                        {
                            var aiCreatedConcepts = AlternativeInitialization.InitializeNonparsableProperties(macroCreatedConcepts, _logger);

                            var newConceptsReport = _dslContainer.AddNewConceptsAndReplaceReferences(
                                aiCreatedConcepts.Concat(macroCreatedConcepts));

                            _logger.Trace(() => LogCreatedConcepts(macroEvaluator, conceptInfo, macroCreatedConcepts, newConceptsReport));

                            iterationCreatedConcepts.AddRange(newConceptsReport.NewUniqueConcepts);
                            if (newConceptsReport.NewlyResolvedConcepts.Count > 0)
                                lastResolvedConceptTimeByMacro[macroEvaluator.Name] = ++lastResolvedConceptTime;
                        }
                    }
                    macroStopwatches[macroEvaluator.Name].Stop();
                };

                lastResolvedConceptTimeByIteration.Add(lastResolvedConceptTime);

                _performanceLogger.Write(sw, "DslModel.ExpandMacroConcepts iteration " + iteration + " ("
                    + iterationCreatedConcepts.Count + " new concepts, "
                    + _dslContainer.UnresolvedConceptsCount() + " left unresolved).");

            } while (iterationCreatedConcepts.Count > 0);

            _evaluatorsOrderLogger.Trace(() => swTotal.Elapsed + "\r\n"
                + ReportLastEvaluationOrder(lastResolvedConceptTimeByMacro, lastResolvedConceptTimeByIteration));
            SaveMacroEvaluationOrder(lastResolvedConceptTimeByMacro);

            foreach (var macroStopwatch in macroStopwatches.OrderByDescending(msw => msw.Value.Elapsed.TotalSeconds).Take(5))
                _performanceLogger.Write(macroStopwatch.Value, () => "DslModel.ExpandMacroConcepts total time for " + macroStopwatch.Key + ".");

            _performanceLogger.Write(swTotal, "DslModel.ExpandMacroConcepts.");
        }

        private string LogCreatedConcepts(MacroEvaluator macroEvaluator, IConceptInfo conceptInfo, IEnumerable<IConceptInfo> macroCreatedConcepts, DslContainer.AddNewConceptsReport newConceptsReport)
        {
            var report = new StringBuilder();
            report.Append(macroEvaluator.Name + " on " + conceptInfo.GetShortDescription()
                + " created: " + string.Join(", ", macroCreatedConcepts.Select(c => c.GetShortDescription())) + ".");

            var newUniqueIndex = new HashSet<string>(newConceptsReport.NewUniqueConcepts.Select(c => c.GetKey()));
            LogConcepts(report, "New resolved", newConceptsReport.NewlyResolvedConcepts.Where(c => newUniqueIndex.Contains(c.GetKey())));
            LogConcepts(report, "Old resolved", newConceptsReport.NewlyResolvedConcepts.Where(c => !newUniqueIndex.Contains(c.GetKey())));
            LogConcepts(report, "New unresolved", newConceptsReport.NewUniqueConcepts.Where(c => _dslContainer.FindByKey(c.GetKey()) == null));

            return report.ToString();
        }

        private void LogConcepts(StringBuilder report, string reportName, IEnumerable<IConceptInfo> concepts)
        {
            CsUtility.Materialize(ref concepts);
            if (concepts != null && concepts.Count() > 0)
                report.Append("\r\n  " + reportName + ": " + string.Join(", ", concepts.Select(c => c.GetShortDescription())) + ".");
        }

        private List<MacroEvaluator> ListMacroEvaluators(Dictionary<string, decimal> recommendedMacroOrder)
        {
            var macroEvaluators = new List<MacroEvaluator>();

            foreach (Type macroConceptType in _conceptTypes.Where(type => typeof(IMacroConcept).IsAssignableFrom(type)))
                macroEvaluators.Add(new MacroEvaluator
                {
                    Name = "IMacroConcept " + macroConceptType.FullName,
                    Evaluate = (conceptInfo, dslContainer) => ((IMacroConcept)conceptInfo).CreateNewConcepts(dslContainer.Concepts),
                    Implements = macroConceptType,
                    ImplementsDerivations = false
                });

            var detectedConceptMacros = new List<Type>();
            foreach (Type conceptType in _conceptTypes)
                foreach (IConceptMacro macro in _macros[conceptType])
                {
                    macroEvaluators.Add(new MacroEvaluator
                    {
                        Name = "IConceptMacro " + macro.GetType().FullName + " for " + conceptType.FullName,
                        Evaluate = (conceptInfo, dslContainer) => macro.CreateNewConcepts(conceptInfo, dslContainer),
                        Implements = conceptType,
                        ImplementsDerivations = true
                    });

                    detectedConceptMacros.Add(macro.GetType());
                }

            var undetectedConceptMacro = _macroTypes.Except(detectedConceptMacros).FirstOrDefault();
            if (undetectedConceptMacro != null)
                throw new DslSyntaxException("Macro " + undetectedConceptMacro + " is not registered properly."
                    + " Check if the concept that the macro implements is registered by attribute Export(typeof(IConceptInfo)).");

            return macroEvaluators
                .OrderBy(evaluator => GetOrderOrDefault(recommendedMacroOrder, evaluator.Name, 0.5m))
                .ThenBy(evaluator => evaluator.Name)
                .ToList();
        }

        private decimal GetOrderOrDefault(IDictionary<string, decimal> recommendedMacroOrder, string key, decimal defaultValue)
        {
            const int databaseLengthLimit = 256;
            if (key.Length > databaseLengthLimit)
                key = key.Substring(0, databaseLengthLimit);

            decimal value;
            if (!recommendedMacroOrder.TryGetValue(key, out value))
            {
                value = defaultValue;
                if (!_noRecommendedOrderReported.Contains(key))
                {
                    _noRecommendedOrderReported.Add(key);
                    _logger.Trace("GetOrderOrDefault: No recommended macro order for " + key + ".");
                }
            }
            return value;
        }

        private static HashSet<string> _noRecommendedOrderReported = new HashSet<string>();

        private string ReportLastEvaluationOrder(Dictionary<string, int> lastResolvedConceptTimeByMacro, List<int> lastResolvedConceptTimeByIteration)
        {
            var orderedMacros = lastResolvedConceptTimeByMacro
                .OrderBy(lastEval => lastEval.Value)
                .Select(lastEval => new { Name = lastEval.Key, Time = lastEval.Value })
                .ToList();

            var report = new StringBuilder();

            int previousIterationTime = 0;
            for (int i = 0; i < lastResolvedConceptTimeByIteration.Count(); i++)
            {
                report.AppendLine("Iteration " + (i + 1) + ":");
                foreach (var evaluator in orderedMacros)
                    if (evaluator.Time > previousIterationTime && evaluator.Time <= lastResolvedConceptTimeByIteration[i])
                        report.AppendLine(evaluator.Name);

                previousIterationTime = lastResolvedConceptTimeByIteration[i];
            }

            return report.ToString();
        }

        private void SaveMacroEvaluationOrder(Dictionary<string, int> lastResolvedConceptTimeByMacro)
        {
            var orderedMacros = lastResolvedConceptTimeByMacro.OrderBy(lastEval => lastEval.Value).Select(lastEval => lastEval.Key).ToList();
            var macroOrders = orderedMacros.Select((macro, index) => new MacroOrder
                {
                    EvaluatorName = macro,
                    EvaluatorOrder = ((decimal)index + 0.5m) / orderedMacros.Count
                });
            _macroOrderRepository.Save(macroOrders);
        }

        private void CheckSemantics()
        {
            var sw = Stopwatch.StartNew();
            foreach (var conceptValidation in _dslContainer.Concepts.OfType<IValidationConcept>())
                conceptValidation.CheckSemantics(_dslContainer.Concepts);
            _performanceLogger.Write(sw, "DslModel.CheckSemantics");
        }
    }
}
