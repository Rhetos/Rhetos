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
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DslContainer : IDslContainer
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;

        public DslContainer(ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslContainer");
        }

        private readonly List<IConceptInfo> _concepts = new List<IConceptInfo>();
        private readonly Dictionary<string, IConceptInfo> _conceptsByKey = new Dictionary<string, IConceptInfo>();
        private readonly Dictionary<string, IConceptInfo> _unresolvedConceptReferencesByKey = new Dictionary<string, IConceptInfo>();

        public IEnumerable<IConceptInfo> Concepts { get { return _concepts; } }
        public IDictionary<string, IConceptInfo> ConceptsByKey { get { return _conceptsByKey; } }
        public IDictionary<string, IConceptInfo> UnresolvedConceptReferencesByKey { get { return _unresolvedConceptReferencesByKey; } }


        /// <summary>
        /// Returns new concepts that did not previously exist in DslModel (a subset of the given concepts enumerable).
        /// Updates concept references to reference existing instances from DslMode with the same concept key.
        /// Result includes concepts that where previously added in DslModel but their references could not be resolved until now.
        /// Result does not include new concepts which references could not be resolved.
        /// </summary>
        public List<IConceptInfo> AddNewConceptsAndReplaceReferences(IEnumerable<IConceptInfo> concepts)
        {
            var newConcepts = new List<IConceptInfo>(concepts.Count());

            // Check for duplicate concepts by key:
            foreach (var conceptInfo in concepts)
            {
                string key = conceptInfo.GetKey();

                IConceptInfo existingConcept;
                if (!_conceptsByKey.TryGetValue(key, out existingConcept))
                {
                    _conceptsByKey.Add(key, conceptInfo);
                    newConcepts.Add(conceptInfo);
                }
                else
                    if (existingConcept != conceptInfo
                        && existingConcept.GetFullDescription() != conceptInfo.GetFullDescription())
                        throw new DslSyntaxException(string.Format(
                            "Concept with same key is described twice with different values.\r\nValue 1: {0}\r\nValue 2: {1}\r\nSame key: {2}",
                            existingConcept.GetFullDescription(),
                            conceptInfo.GetFullDescription(),
                            key));
            }

            _concepts.AddRange(newConcepts);
            var newConceptsWithNewResolvedReferences = ReplaceReferencesWithFullConcepts(newConcepts);

            return newConceptsWithNewResolvedReferences;
        }

        private List<IConceptInfo> ReplaceReferencesWithFullConcepts(IEnumerable<IConceptInfo> newConcepts, bool errorOnUnresolvedReference = false)
        {
            var newConceptsWithNewResolvedReferences = new List<IConceptInfo>();
            var newUnresolvedReferences = new List<IConceptInfo>();

            foreach (IConceptInfo ci in newConcepts.Concat(_unresolvedConceptReferencesByKey.Values))
            {
                try
                {
                    bool resolved = true;

                    foreach (ConceptMember member in ConceptMembers.Get(ci))
                        if (member.IsConceptInfo)
                        {
                            var reference = (IConceptInfo)member.GetValue(ci);

                            if (reference == null)
                            {
                                if (errorOnUnresolvedReference)
                                    throw new DslSyntaxException(string.Format(
                                        "Error in concept info {0}: property '{1}' is not initialized. Check if the InitializeNonparsableProperties function on class {0} is implemented properly. Instance: {2}.",
                                        ci.GetType().Name, member.Name, ci.GetErrorDescription()));
                                else
                                {
                                    resolved = false;
                                    continue;
                                }
                            }

                            string referencedKey = reference.GetKey();

                            IConceptInfo referencedConcept;
                            if (!_conceptsByKey.TryGetValue(referencedKey, out referencedConcept))
                            {
                                if (errorOnUnresolvedReference)
                                    throw new DslSyntaxException(string.Format(
                                        "Referenced concept is not defined in DSL scripts. '{0}' references undefined contept '{1}' (type {2}).",
                                        ci.GetUserDescription(),
                                        ((IConceptInfo)member.GetValue(ci)).GetUserDescription(),
                                        member.GetValue(ci).GetType().Name));
                                else
                                {
                                    resolved = false;
                                    continue;
                                }
                            }

                            member.SetMemberValue(ci, referencedConcept);
                        }

                    if (resolved)
                        newConceptsWithNewResolvedReferences.Add(ci);
                    else
                        newUnresolvedReferences.Add(ci);
                }
                catch (Exception ex)
                {
                    _logger.Write(EventType.Error, () => ex.GetType().Name + " while analyzing concept " + ci.GetErrorDescription() + ".");
                    throw;
                }
            }

            foreach (var ci in newConceptsWithNewResolvedReferences)
            {
                string ciKey = ci.GetKey();
                if (_unresolvedConceptReferencesByKey.ContainsKey(ciKey))
                    _unresolvedConceptReferencesByKey.Remove(ciKey);
                _logger.Trace(() => "New concept with resolved references: " + ciKey);
            }
            foreach (var ci in newUnresolvedReferences)
            {
                string ciKey = ci.GetKey();
                if (!_unresolvedConceptReferencesByKey.ContainsKey(ciKey))
                    _unresolvedConceptReferencesByKey.Add(ciKey, ci);
            }

            return newConceptsWithNewResolvedReferences;
        }

        public void ReportErrorForUnresolvedConcepts()
        {
            ReplaceReferencesWithFullConcepts(new IConceptInfo[] { }, true);
        }

        public void SortReferencesBeforeUsingConcept()
        {
            List<IConceptInfo> sortedList = new List<IConceptInfo>();
            Dictionary<IConceptInfo, bool> processed = _concepts.ToDictionary(ci => ci, ci => false);

            foreach (var concept in _concepts)
                AddReferencesBeforeConcept(concept, sortedList, processed);

            if (sortedList.Count != _concepts.Count)
                throw new FrameworkException(string.Format("Unexpected inner state: sortedList.Count {0} != concepts.Count {1}.", sortedList.Count, _concepts.Count));
            _concepts.Clear();
            _concepts.AddRange(sortedList);
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
