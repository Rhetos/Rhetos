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

using Rhetos.Logging;
using System;
using System.Collections.Generic;
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

        public DslContainer(ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslContainer");
        }

        private readonly List<IConceptInfo> _resolvedConcepts = new List<IConceptInfo>();
        private readonly Dictionary<string, IConceptInfo> _resolvedConceptsByKey = new Dictionary<string, IConceptInfo>();
        private readonly Dictionary<string, IConceptInfo> _unresolvedConceptsByKey = new Dictionary<string, IConceptInfo>();

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

        #endregion

        /// <summary>
        /// Result include new concepts that did not previously exist in DslModel (a subset of the given concepts enumerable and also
        /// concepts that where previously added in DslModel but their references could not be resolved until now).
        /// Updates concept references to reference existing instances from DslMode with the same concept key.
        /// Result does not include new concepts which references could not be resolved.
        /// </summary>
        public List<IConceptInfo> AddNewConceptsAndReplaceReferences(IEnumerable<IConceptInfo> newConcepts)
        {
            foreach (var conceptInfo in newConcepts)
            {
                string key = conceptInfo.GetKey();

                IConceptInfo existingConcept;

                if (!_resolvedConceptsByKey.TryGetValue(key, out existingConcept) && !_unresolvedConceptsByKey.TryGetValue(key, out existingConcept))
                    _unresolvedConceptsByKey.Add(key, conceptInfo);
                else
                    if (existingConcept != conceptInfo && existingConcept.GetFullDescription() != conceptInfo.GetFullDescription())
                        throw new DslSyntaxException(string.Format(
                            "Concept with same key is described twice with different values.\r\nValue 1: {0}\r\nValue 2: {1}\r\nSame key: {2}",
                            existingConcept.GetFullDescription(),
                            conceptInfo.GetFullDescription(),
                            key));
            }

            return ReplaceReferencesWithFullConcepts(errorOnUnresolvedReference: false);
        }

        /// <summary>
        /// Since DSL parser returns stub references, this function replaces each reference with actual instance of the referenced concept.
        /// Function returns concepts that have newly resolved references.
        /// </summary>
        private List<IConceptInfo> ReplaceReferencesWithFullConcepts(bool errorOnUnresolvedReference)
        {
            var conceptsWithNewlyResolvedReferences = new List<IConceptInfo>();

            var _unresolvedConceptsCopy = _unresolvedConceptsByKey.ToList(); // The copy is needed because _unresolvedConceptsByKey will be modified inside the loop.
            foreach (var concept in _unresolvedConceptsCopy)
            {
                bool resolved = true;
                foreach (ConceptMember member in ConceptMembers.Get(concept.Value))
                    if (member.IsConceptInfo)
                        resolved &= TryResolveReferenceMember(concept.Value, member, errorOnUnresolvedReference);

                if (resolved)
                {
                    _unresolvedConceptsByKey.Remove(concept.Key);
                    _resolvedConceptsByKey.Add(concept.Key, concept.Value);
                    _resolvedConcepts.Add(concept.Value);

                    conceptsWithNewlyResolvedReferences.Add(concept.Value);
                    _logger.Trace(() => "New concept with resolved references: " + concept.Key);
                }
            }

            return conceptsWithNewlyResolvedReferences;
        }

        private bool TryResolveReferenceMember(IConceptInfo ci, ConceptMember member, bool errorOnUnresolvedReference)
        {
            var reference = (IConceptInfo)member.GetValue(ci);

            if (reference == null)
            {
                if (errorOnUnresolvedReference)
                    throw new DslSyntaxException(ci, string.Format(
                        "Property '{1}' is not initialized. Check if the InitializeNonparsableProperties function on class {0} is implemented properly.",
                        ci.GetType().Name,
                        member.Name));
                else
                    return false;
            }

            string referencedKey = reference.GetKey();

            IConceptInfo referencedConcept;
            if (!_resolvedConceptsByKey.TryGetValue(referencedKey, out referencedConcept))
            {
                if (errorOnUnresolvedReference)
                    throw new DslSyntaxException(ci, string.Format(
                        "Referenced concept is not defined in DSL scripts. '{0}' references undefined contept '{1}' (type {2}).",
                        ci.GetUserDescription(),
                        reference.GetUserDescription(),
                        reference.GetType().Name));
                else
                    return false;
            }

            member.SetMemberValue(ci, referencedConcept);
            return true;
        }

        public void ReportErrorForUnresolvedConcepts()
        {
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
