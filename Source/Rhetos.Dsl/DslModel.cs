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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rhetos.Logging;
using System.Collections;
using Rhetos.Utilities;
using Rhetos.Extensibility;

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
        private readonly IPluginsContainer<IConceptMacro> _macros;

        public DslModel(IDslParser dslParser, ILogProvider logProvider, IPluginsContainer<IConceptMacro> macros)
        {
            _dslParser = dslParser;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslModel");
            _evaluatorsOrderLogger = logProvider.GetLogger("MacroEvaluatorsOrder");
            _dslModelConceptsLogger = logProvider.GetLogger("DslModelConcepts");
            _dslContainer = new DslContainer(logProvider);
            _macros = macros;
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

        public IEnumerable<T> FindByType<T>()
        {
            if (!_initialized)
                Initialize();
            return _dslContainer.FindByType<T>();
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

                        _dslModelConceptsLogger.Trace(LogConcepts);
                        _performanceLogger.Write(sw, "DslModel.Initialize");
                        _initialized = true;
                    }
        }

        private string LogConcepts()
        {
            var xmlUtility = new XmlUtility(null);
            return string.Join("\r\n", _dslContainer.Concepts.Select(c => xmlUtility.SerializeToXml(c, c.GetType())).OrderBy(x => x));
        }

        private const int MacroIterationLimit = 200;

        private class MacroEvaluator
        {
            public string Name;
            public Func<IDslModel, IEnumerable<IConceptInfo>> Evaluate;
            public IConceptInfo ConceptInfo;
        }

        private void ExpandMacroConcepts()
        {
            var swTotal = Stopwatch.StartNew();
            var sw = Stopwatch.StartNew();
            var iterationCreatedConcepts = new List<IConceptInfo>();
            int iteration = 0;

            _performanceLogger.Write(sw, "DslModel.ExpandMacroConcepts start ("
                + _dslContainer.Concepts.Count() + " parsed concepts resolved, "
                +  _dslContainer.UnresolvedConceptsCount() + " unresolved).");

            var lastEvaluation = new Dictionary<string, int>();
            int lastEvaluationCounter = 0;
            var lastEvaluationByIteration = new List<int>();

            do
            {
                iteration++;
                if (iteration > MacroIterationLimit)
                    throw new DslSyntaxException(string.Format(
                        "Possible infinite loop detected with recursive macro concept {1}. Iteration limit ({0}) exceeded while expanding macro.",
                        MacroIterationLimit,
                        iterationCreatedConcepts.First().GetShortDescription()));

                iterationCreatedConcepts.Clear();
                _logger.Trace("Expanding macro concepts, pass {0}.", iteration);

                var macroEvaluators = ListMacroEvaluators();
                _performanceLogger.Write(sw, "DslModel.ExpandMacroConcepts prepared evaluators (" + macroEvaluators.Count + ").");

                foreach (var macroEvaluator in macroEvaluators)
                {
                    var macroCreatedConcepts = macroEvaluator.Evaluate(_dslContainer);
                    Materialize(ref macroCreatedConcepts);

                    if (macroCreatedConcepts != null && macroCreatedConcepts.Count() > 0)
                    {
                        _logger.Trace(() => macroEvaluator.Name + " on " + macroEvaluator.ConceptInfo.GetShortDescription() + " generated: "
                            + string.Join(", ", macroCreatedConcepts.Select(c => c.GetShortDescription())) + ".");

                        var aiCreatedConcepts = AlternativeInitialization.InitializeNonparsableProperties(macroCreatedConcepts, _logger);
                        var newUniqueConcepts = _dslContainer.AddNewConceptsAndReplaceReferences(macroCreatedConcepts.Concat(aiCreatedConcepts));
                        if (newUniqueConcepts.Count > 0)
                        {
                            lastEvaluation[macroEvaluator.Name] = ++lastEvaluationCounter;
                            iterationCreatedConcepts.AddRange(newUniqueConcepts);
                        }
                    }
                };

                lastEvaluationByIteration.Add(lastEvaluationCounter);

                _performanceLogger.Write(sw, "DslModel.ExpandMacroConcepts iteration " + iteration + " ("
                    + iterationCreatedConcepts.Count + " new concepts, "
                    + _dslContainer.UnresolvedConceptsCount() + " left unresolved).");

            } while (iterationCreatedConcepts.Count > 0);

            _evaluatorsOrderLogger.Trace(() => "\r\n" + ReportLastEvaluationOrder(lastEvaluation, lastEvaluationByIteration));
            _performanceLogger.Write(swTotal, "DslModel.ExpandMacroConcepts.");
        }

        private List<MacroEvaluator> ListMacroEvaluators()
        {
            var macroEvaluators = new List<MacroEvaluator>();

            foreach (IMacroConcept macroConcept in _dslContainer.FindByType<IMacroConcept>().ToList())
                macroEvaluators.Add(new MacroEvaluator
                {
                    Name = "IMacroConcept " + macroConcept.GetType().FullName,
                    Evaluate = dslContainer => macroConcept.CreateNewConcepts(dslContainer.Concepts),
                    ConceptInfo = macroConcept
                });

            foreach (IConceptInfo conceptInfo in _dslContainer.Concepts.ToList())
                foreach (IConceptMacro macro in _macros.GetImplementations(conceptInfo.GetType()))
                    macroEvaluators.Add(new MacroEvaluator
                    {
                        Name = "IConceptMacro " + macro.GetType().FullName + " for " + conceptInfo.GetType().FullName,
                        Evaluate = dslContainer => macro.CreateNewConcepts(conceptInfo, dslContainer),
                        ConceptInfo = conceptInfo
                    });

            return macroEvaluators;
        }

        private static void Materialize(ref IEnumerable<IConceptInfo> items)
        {
            if (items != null && !(items is IList))
                items = items.ToList();
        }

        private string ReportLastEvaluationOrder(Dictionary<string, int> lastEvaluation, List<int> lastEvaluationByIteration)
        {
            var evaluatorsOrderedByLastEvaluation = lastEvaluation
                .OrderBy(lastEval => lastEval.Value)
                .Select(lastEval => new { Name = lastEval.Key, Time = lastEval.Value })
                .ToList();

            var report = new StringBuilder();

            int previosIterationTime = 0;
            for (int i = 0; i < lastEvaluationByIteration.Count(); i++)
            {
                report.AppendLine("Iteration " + (i + 1) + ":");
                foreach (var evaluator in evaluatorsOrderedByLastEvaluation)
                    if (evaluator.Time > previosIterationTime && evaluator.Time <= lastEvaluationByIteration[i])
                        report.AppendLine(evaluator.Name);

                previosIterationTime = lastEvaluationByIteration[i];
            }

            return report.ToString();
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
