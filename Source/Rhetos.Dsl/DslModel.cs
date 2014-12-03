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
        private readonly ILogger _dslModelConceptsLogger;
        private readonly DslContainer _dslContainer;
        private readonly IPluginsContainer<IConceptMacro> _macros;

        public DslModel(IDslParser dslParser, ILogProvider logProvider, IPluginsContainer<IConceptMacro> macros)
        {
            _dslParser = dslParser;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslModel");
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

        private void ExpandMacroConcepts()
        {
            var sw = Stopwatch.StartNew();
            var iterationCreatedConcepts = new List<IConceptInfo>();
            int iteration = 0;
            while (true)
            {
                iteration++;
                _logger.Trace("Expanding macro concepts, pass {0}.", iteration);

                iterationCreatedConcepts.Clear();

                foreach (IMacroConcept macroConcept in _dslContainer.FindByType<IMacroConcept>())
                    AddNewConcepts(
                        macroConcept.CreateNewConcepts(_dslContainer.Concepts),
                        iterationCreatedConcepts,
                        () => "Macro concept " + macroConcept.GetShortDescription());

                foreach (IConceptInfo conceptInfo in _dslContainer.Concepts)
                    foreach (IConceptMacro macro in _macros.GetImplementations(conceptInfo.GetType()))
                        AddNewConcepts(
                            macro.CreateNewConcepts(conceptInfo, _dslContainer),
                            iterationCreatedConcepts,
                            () => "Macro " + macro.GetType().Name + " for concept " + conceptInfo.GetShortDescription());

                iterationCreatedConcepts = _dslContainer.AddNewConceptsAndReplaceReferences(iterationCreatedConcepts);

                _performanceLogger.Write(sw, "DslModel.ExpandMacroConcepts " + iteration);

                if (iterationCreatedConcepts.Count == 0)
                    break;

                if (iteration > MacroIterationLimit)
                    throw new DslSyntaxException(string.Format(
                        "Possible infinite loop detected with recursive macro concept {1}. Iteration limit ({0}) exceeded while expanding macro.",
                        MacroIterationLimit,
                        iterationCreatedConcepts.First().GetShortDescription()));
            }
        }

        private void AddNewConcepts(IEnumerable<IConceptInfo> macroCreatedConcepts, List<IConceptInfo> iterationCreatedConcepts, Func<string> logDescription)
        {
            if (macroCreatedConcepts == null || macroCreatedConcepts.Count() == 0)
                return;

            // Evaluate macro concept:

            Materialize(ref macroCreatedConcepts);
            iterationCreatedConcepts.AddRange(macroCreatedConcepts);

            _logger.Trace("{0} generated: {1}.", logDescription(), string.Join(", ", macroCreatedConcepts.Select(c => c.GetShortDescription())));

            // Alternative initialization of the created concepts:

            var alternativeInitializationCreatedConcepts = new List<IConceptInfo>();
            foreach (var macroCreatedAlternativeInitializationConcept in macroCreatedConcepts.OfType<IAlternativeInitializationConcept>())
            {
                IEnumerable<IConceptInfo> aicc = AlternativeInitialization.InitializeNonparsablePropertiesRecursive(macroCreatedAlternativeInitializationConcept);
                if (aicc != null)
                    alternativeInitializationCreatedConcepts.AddRange(aicc);
            }
            iterationCreatedConcepts.AddRange(alternativeInitializationCreatedConcepts);

            if (alternativeInitializationCreatedConcepts.Count() > 0)
                _logger.Trace("{0} generated by alternative initialization: {1}.", logDescription(), string.Join(", ", alternativeInitializationCreatedConcepts.Select(c => c.GetShortDescription())));
        }

        private static readonly IConceptInfo[] emptyConceptsArray = new IConceptInfo[] { };

        private static void Materialize(ref IEnumerable<IConceptInfo> items)
        {
            if (items == null)
                items = emptyConceptsArray;
            else if (!(items is IList))
                items = items.ToList();
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
