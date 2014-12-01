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

namespace Rhetos.Dsl
{
    public class DslModel : IDslModel
    {
        private readonly IDslParser _dslParser;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly ILogger _dslModelConceptsLogger;
        private readonly DslContainer _dslContainer;

        public DslModel(IDslParser dslParser, ILogProvider logProvider)
        {
            _dslParser = dslParser;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslModel");
            _dslModelConceptsLogger = logProvider.GetLogger("DslModelConcepts");
            _dslContainer = new DslContainer(logProvider);
        }

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
            var createdConcepts = new List<IConceptInfo>();
            int iteration = 0;
            while (true)
            {
                iteration++;
                _logger.Trace("Expanding macro concepts, pass {0}.", iteration);

                createdConcepts.Clear();

                var resolvedMacroConcepts = _dslContainer.ConceptsByKey
                    .Where(item => !_dslContainer.UnresolvedConceptReferencesByKey.ContainsKey(item.Key))
                    .Select(item => item.Value)
                    .OfType<IMacroConcept>().ToArray();

                foreach (IMacroConcept macroConcept in resolvedMacroConcepts)
                {
                    // Evaluate macro concept:

                    var macroCreatedConcepts = macroConcept.CreateNewConcepts(_dslContainer.Concepts);
                    Materialize(ref macroCreatedConcepts);
                    createdConcepts.AddRange(macroCreatedConcepts);

                    var logConcept = macroConcept;
                    _logger.Trace("Macro concept {0} generated: {1}.", logConcept.GetShortDescription(), string.Join(", ", macroCreatedConcepts.Select(c => c.GetShortDescription())));

                    // Alternative initialization of the created concepts:

                    var alternativeInitializationCreatedConcepts = new List<IConceptInfo>();
                    foreach (var macroCreatedAlternativeInitializationConcept in macroCreatedConcepts.OfType<IAlternativeInitializationConcept>())
                    {
                        IEnumerable<IConceptInfo> aicc = AlternativeInitialization.InitializeNonparsablePropertiesRecursive(macroCreatedAlternativeInitializationConcept);
                        if (aicc != null)
                            alternativeInitializationCreatedConcepts.AddRange(aicc);
                    }
                    createdConcepts.AddRange(alternativeInitializationCreatedConcepts);

                    if (alternativeInitializationCreatedConcepts.Count() > 0)
                        _logger.Trace("Macro concept {0} generated through alternative initialization: {1}.", logConcept.GetShortDescription(), string.Join(", ", alternativeInitializationCreatedConcepts.Select(c => c.GetShortDescription())));
                }

                createdConcepts = _dslContainer.AddNewConceptsAndReplaceReferences(createdConcepts);

                _performanceLogger.Write(sw, "DslModel.ExpandMacroConcepts " + iteration);

                if (createdConcepts.Count == 0)
                    break;

                if (iteration > MacroIterationLimit)
                    throw new DslSyntaxException(string.Format(
                        "Possible infinite loop detected with recursive macro concept {1}. Iteration limit ({0}) exceeded while expanding macro.",
                        MacroIterationLimit,
                        createdConcepts.First().GetShortDescription()));
            }
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
