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

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    /// <summary>
    /// This class implements IDslModel, but it may return empty or partly initialized list of concepts, as opposed to
    /// DslModel class (also implements IDslModel) that always makes sure that the DSL model is fully initialized
    /// before returning the concepts.
    /// </summary>
    public class DslContainer : IDslModel
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly Stopwatch _addConceptsStopwatch = new Stopwatch();

        private readonly List<IConceptInfo> _resolvedConcepts = new List<IConceptInfo>();
        private readonly Dictionary<string, IConceptInfo> _resolvedConceptsByKey = new Dictionary<string, IConceptInfo>();
        private readonly Dictionary<string, IConceptInfo> _unresolvedConceptsByKey = new Dictionary<string, IConceptInfo>();
        private readonly List<IDslModelIndex> _dslModelIndexes;
        private readonly Dictionary<Type, IDslModelIndex> _dslModelIndexesByType;

        public DslContainer(ILogProvider logProvider, IPluginsContainer<IDslModelIndex> dslModelIndexPlugins)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslContainer");
            _dslModelIndexes = dslModelIndexPlugins.GetPlugins().ToList();
            _dslModelIndexesByType = _dslModelIndexes.ToDictionary(index => index.GetType());
        }

        #region IDslModel filters implementation

        public IEnumerable<IConceptInfo> Concepts
        {
            get { return _resolvedConcepts; }
        }

        public IConceptInfo FindByKey(string conceptKey)
        {
            IConceptInfo result = null;
            _resolvedConceptsByKey.TryGetValue(conceptKey, out result);
            return result;
        }

        public T GetIndex<T>() where T : IDslModelIndex
        {
            IDslModelIndex index;
            if (!_dslModelIndexesByType.TryGetValue(typeof(T), out index))
                throw new FrameworkException("There is no registered IDslModelIndex plugin of type '" + typeof(T).FullName + "'.");
            return (T)index;
        }

        #endregion

        public class AddNewConceptsReport
        {
            /// <summary>A subset of given new concepts.</summary>
            public List<IConceptInfo> NewUniqueConcepts;
            /// <summary>May include previously given concepts that have been resolved now.</summary>
            public List<IConceptInfo> NewlyResolvedConcepts;
        }

        /// <summary>
        /// Updates concept references to reference existing instances from DslModel matched by the concept key.
        /// Returns new unique concepts that did not previously exist in DslModel
        /// (note that some of the returned concepts might not have their references resolved yet).
        /// </summary>
        public AddNewConceptsReport AddNewConceptsAndReplaceReferences(IEnumerable<IConceptInfo> newConcepts)
        {
            _addConceptsStopwatch.Start();
            var report = new AddNewConceptsReport();
            report.NewUniqueConcepts = new List<IConceptInfo>();

            foreach (var conceptInfo in newConcepts)
            {
                string conceptKey = conceptInfo.GetKey();
                IConceptInfo existingConcept;

                if (!_resolvedConceptsByKey.TryGetValue(conceptKey, out existingConcept) && !_unresolvedConceptsByKey.TryGetValue(conceptKey, out existingConcept))
                {
                    report.NewUniqueConcepts.Add(conceptInfo);
                    _unresolvedConceptsByKey.Add(conceptKey, conceptInfo);
                }
                else
                    if (existingConcept != conceptInfo && existingConcept.GetFullDescription() != conceptInfo.GetFullDescription())
                        throw new DslSyntaxException(string.Format(
                            "Concept with same key is described twice with different values.\r\nValue 1: {0}\r\nValue 2: {1}\r\nSame key: {2}",
                            existingConcept.GetFullDescription(),
                            conceptInfo.GetFullDescription(),
                            conceptKey));
            }

            var newlyResolved = ReplaceReferencesWithFullConcepts(errorOnUnresolvedReference: false);
            report.NewlyResolvedConcepts = newlyResolved.ToList();

            _addConceptsStopwatch.Stop();
            return report;
        }

        /// <summary>
        /// Since DSL parser returns stub references, this function replaces each reference with actual instance of the referenced concept.
        /// Function returns concepts that have newly resolved references.
        /// </summary>
		private IEnumerable<IConceptInfo> ReplaceReferencesWithFullConcepts(bool errorOnUnresolvedReference)
        {
            var dependencies = new List<Tuple<string, string>>();
            var newUnresolved = new List<string>();

            foreach (var concept in _unresolvedConceptsByKey)
            {
                foreach (ConceptMember member in ConceptMembers.Get(concept.Value))
                    if (member.IsConceptInfo)
                    {
                        var reference = (IConceptInfo)member.GetValue(concept.Value);

                        if (reference == null)
                        {
                            string errorMessage = "Property '" + member.Name + "' is not initialized.";
                            if (concept.Value is IAlternativeInitializationConcept)
                                errorMessage = errorMessage + string.Format(
                                    " Check if the InitializeNonparsableProperties function of IAlternativeInitializationConcept implementation at {0} is implemented properly.",
                                    concept.Value.GetType().Name);
                            throw new DslSyntaxException(concept.Value, errorMessage);
                        }

                        string referencedKey = reference.GetKey();

                        dependencies.Add(Tuple.Create(referencedKey, concept.Key));

                        IConceptInfo referencedConcept;
                        if (!_resolvedConceptsByKey.TryGetValue(referencedKey, out referencedConcept)
                            && !_unresolvedConceptsByKey.TryGetValue(referencedKey, out referencedConcept))
                            {
                                if (errorOnUnresolvedReference)
                                    throw new DslSyntaxException(concept.Value, string.Format(
                                        "Referenced concept is not defined in DSL scripts: '{0}'.",
                                        reference.GetUserDescription()));

                                newUnresolved.Add(concept.Key);
                            }
                        else
                            member.SetMemberValue(concept.Value, referencedConcept);
                    }
            }

            // Unresolved concepts should also include any concept with resolved references that references an unresolved concept.
            newUnresolved = Graph.IncludeDependents(newUnresolved, dependencies);

            var unresolvedIndex = new HashSet<string>(newUnresolved);
            var newlyResolved = _unresolvedConceptsByKey
                .Where(concept => !unresolvedIndex.Contains(concept.Key))
                .ToList();

            foreach (var concept in newlyResolved)
            {
                _logger.Trace(() => "New concept with resolved references: " + concept.Key);

                _unresolvedConceptsByKey.Remove(concept.Key);

                _resolvedConcepts.Add(concept.Value);
                _resolvedConceptsByKey.Add(concept.Key, concept.Value);

                foreach (var index in _dslModelIndexes)
                    index.Add(concept.Value);
            }

            return newlyResolved.Select(concept => concept.Value);
        }

        public int UnresolvedConceptsCount()
        {
            return _unresolvedConceptsByKey.Count();
        }

        public void ReportErrorForUnresolvedConcepts()
        {
            _performanceLogger.Write(_addConceptsStopwatch, "DslContainer.AddNewConceptsAndReplaceReferences total time.");
            ReplaceReferencesWithFullConcepts(errorOnUnresolvedReference: true);
        }

        public void SortReferencesBeforeUsingConcept()
        {
            List<IConceptInfo> sortedList = new List<IConceptInfo>();
            Dictionary<IConceptInfo, bool> processed = _resolvedConcepts.ToDictionary(ci => ci, ci => false);

            foreach (var concept in _resolvedConcepts)
                AddReferencesBeforeConcept(concept, sortedList, processed);

            if (sortedList.Count != _resolvedConcepts.Count)
                throw new FrameworkException(string.Format("Unexpected inner state: sortedList.Count {0} != concepts.Count {1}.", sortedList.Count, _resolvedConcepts.Count));
            _resolvedConcepts.Clear();
            _resolvedConcepts.AddRange(sortedList);
        }

        private static void AddReferencesBeforeConcept(IConceptInfo concept, List<IConceptInfo> sortedList, Dictionary<IConceptInfo, bool> processed)
        {
            if (!processed.ContainsKey(concept))
                throw new FrameworkException(string.Format(
                    "Unexpected inner state: Referenced concept {0} is not found in list of all concepts.",
                    concept.GetUserDescription()));
            if (processed[concept]) // eliminates duplication of referenced concepts and stops circular references from infinite recursion
                return;
            processed[concept] = true;
            foreach (ConceptMember member in ConceptMembers.Get(concept))
                if (member.IsConceptInfo)
                    AddReferencesBeforeConcept((IConceptInfo)member.GetValue(concept), sortedList, processed);
            sortedList.Add(concept);
        }
    }
}
