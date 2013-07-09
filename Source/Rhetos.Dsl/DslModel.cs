/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Dsl
{
    public class DslModel : IDslModel
    {
        private readonly IDslParser _dslParser;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly DslContainer _dslContainer;

        public DslModel(IDslParser dslParser, ILogProvider logProvider)
        {
            _dslParser = dslParser;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslModel");
            _dslContainer = new DslContainer(logProvider);
        }

        public IEnumerable<IConceptInfo> Concepts
        {
            get { if (!_initialized) Initialize(); return _dslContainer.Concepts; }
        }

        public IConceptInfo FindByKey(string conceptKey)
        {
            if (!_initialized)
                Initialize();

            IConceptInfo result;
            _dslContainer.ConceptsByKey.TryGetValue(conceptKey, out result);
            if (result == null)
                throw new FrameworkException("There is no concept instance with key '"
                    + conceptKey + "'. See ConceptInfoHelper.GetKey() function description for expected format.");
            return result;
        }

        private bool _initialized;
        private readonly object _initializationLock = new object();

        private void Initialize()
        {
            lock (_initializationLock)
            {
                if (_initialized)
                    return;
                var sw = Stopwatch.StartNew();

                _dslContainer.AddNewConceptsAndReplaceReferences(_dslParser.ParsedConcepts);
                ExpandMacroConcepts();
                _dslContainer.ReportErrorForUnresolvedConcepts();
                CheckSemantics();
                _dslContainer.SortReferencesBeforeUsingConcept();

                _performanceLogger.Write(sw, "DslModel.Initialize");
                _initialized = true;
            }
        }

        private const int MacroIterationLimit = 200;

        private void ExpandMacroConcepts()
        {
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

                    var macroCreatedConcepts = macroConcept.CreateNewConcepts(_dslContainer.Concepts) ?? new IConceptInfo[] { };
                    createdConcepts.AddRange(macroCreatedConcepts);

                    var logConcept = macroConcept;
                    _logger.Trace("Macro concept {0} generated: {1}.", logConcept.GetShortDescription(), string.Join(", ", macroCreatedConcepts.Select(c => c.GetShortDescription())));

                    // Alternative initialization of the created concepts:

                    var alternativeInitializationCreatedConcepts = new List<IConceptInfo>();
                    foreach (var macroCreatedAlternativeInitializationConcept in macroCreatedConcepts.OfType<IAlternativeInitializationConcept>())
                    {
                        IEnumerable<IConceptInfo> aicc;
                        macroCreatedAlternativeInitializationConcept.InitializeNonparsableProperties(out aicc);
                        if (aicc != null)
                            alternativeInitializationCreatedConcepts.AddRange(aicc);
                    }
                    createdConcepts.AddRange(alternativeInitializationCreatedConcepts);

                    if (alternativeInitializationCreatedConcepts.Count() > 0)
                        _logger.Trace("Macro concept {0} generated through alternative initialization: {1}.", logConcept.GetShortDescription(), string.Join(", ", alternativeInitializationCreatedConcepts.Select(c => c.GetShortDescription())));
                }

                createdConcepts = _dslContainer.AddNewConceptsAndReplaceReferences(createdConcepts);

                if (createdConcepts.Count == 0)
                    break;

                if (iteration > MacroIterationLimit)
                    throw new DslSyntaxException(string.Format(
                        "Possible infinite loop detected with recursive macro concept {1}. Iteration limit ({0}) exceeded while expanding macro.",
                        MacroIterationLimit,
                        createdConcepts.First().GetShortDescription()));
            }
        }

        private void CheckSemantics()
        {
            foreach (var conceptValidation in _dslContainer.Concepts.OfType<IValidationConcept>())
                conceptValidation.CheckSemantics(_dslContainer.Concepts);
        }
    }
}
