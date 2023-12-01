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
        private readonly Stopwatch _validateDuplicateStopwatch = new Stopwatch();

        private readonly Dictionary<string, ConceptDescription> _givenConceptsByKey = new Dictionary<string, ConceptDescription>();
        private readonly List<IConceptInfo> _resolvedConcepts = new List<IConceptInfo>();
        private readonly Dictionary<string, IConceptInfo> _resolvedConceptsByKey = new Dictionary<string, IConceptInfo>();
        private readonly MultiDictionary<string, UnresolvedReference> _unresolvedConceptsByReference = new MultiDictionary<string, UnresolvedReference>();
        private readonly List<IDslModelIndex> _dslModelIndexes;
        private readonly Dictionary<Type, IDslModelIndex> _dslModelIndexesByType;

        private class ConceptDescription
        {
            public readonly IConceptInfo Concept;
            public readonly string Key;
            public int UnresolvedDependencies;

            public ConceptDescription(IConceptInfo concept, string key)
            {
                Concept = concept;
                Key = key;
                UnresolvedDependencies = 0;
            }
        }

        private class UnresolvedReference
        {
            public readonly ConceptDescription Dependant;
            /// <summary>A member property on the Dependant concept that references another concept.</summary>
            public readonly ConceptMember Member;
            public readonly IConceptInfo ReferencedStub;
            public readonly string ReferencedKey;

            public UnresolvedReference(ConceptDescription dependant, ConceptMember referenceMember)
            {
                Dependant = dependant;
                Member = referenceMember;
                ReferencedStub = (IConceptInfo)Member.GetValue(Dependant.Concept);
                ReferencedKey = ReferencedStub?.GetKey();
            }
        }

        public DslContainer(ILogProvider logProvider, IPluginsContainer<IDslModelIndex> dslModelIndexPlugins)
        {
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
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

        /// <summary>
        /// Updates concept references to reference existing instances from DslModel matched by the concept key.
        /// Returns new unique concepts that did not previously exist in DslModel
        /// (note that some of the returned concepts might not have their references resolved yet).
        /// </summary>
        public List<IConceptInfo> AddNewConceptsAndReplaceReferences(IEnumerable<IConceptInfo> newConcepts)
        {
            _addConceptsStopwatch.Start();
            var newUniqueConcepts = new List<IConceptInfo>();

            foreach (var concept in newConcepts)
            {
                string conceptKey = concept.GetKey();
                if (!_givenConceptsByKey.TryGetValue(conceptKey, out ConceptDescription existingConcept))
                {
                    var conceptDesc = new ConceptDescription(concept, conceptKey);
                    _givenConceptsByKey.Add(conceptDesc.Key, conceptDesc);
                    ReplaceReferencesWithFullConcepts(conceptDesc);
                    newUniqueConcepts.Add(conceptDesc.Concept);
                }
                else
                    ValidateNewConceptSameAsExisting(concept, existingConcept.Concept);
            }

            _addConceptsStopwatch.Stop();
            return newUniqueConcepts;
        }

        // The new concept is allowed to be a simple version (base class) of the existing concept, even if it is not the same.
        // This will allow some macro concepts to create simplified new concept that will be ignored if more specific version is already implemented.
        // Note: Unfortunately this logic should not simply be reversed to also ignore old concept if the new concept is a derivation of the old one,
        // because other macros might have already used the old concept to generate a different business logic.
        private void ValidateNewConceptSameAsExisting(IConceptInfo newConcept, IConceptInfo existingConcept)
        {
            _validateDuplicateStopwatch.Start();

            if (!ConteptsValueEqualOrBase(newConcept, existingConcept))
                throw new DslSyntaxException(
                    "Concept with same key is described twice with different values."
                    + "\r\nValue 1: " + existingConcept.GetFullDescription()
                    + "\r\nValue 2: " + newConcept.GetFullDescription()
                    + "\r\nSame key: " + newConcept.GetKey());

            _validateDuplicateStopwatch.Stop();
        }

        private bool ConteptsValueEqualOrBase(IConceptInfo newConcept, IConceptInfo existingConcept)
        {
            if (object.ReferenceEquals(newConcept, existingConcept))
                return true;
            else if (newConcept.GetKey() != existingConcept.GetKey())
                return false;
            else if (!newConcept.GetType().IsInstanceOfType(existingConcept))
                return false;
            else
            {
                var newConceptMemebers = ConceptMembers.Get(newConcept);
                foreach (ConceptMember member in newConceptMemebers)
                {
                    if (member.IsKey)
                        continue;

                    if (!IsConceptMemberEqual(newConcept, existingConcept, member))
                        return false;
                }
            }
            return true;
        }

        private bool IsConceptMemberEqual(IConceptInfo newConcept, IConceptInfo existingConcept, ConceptMember conceptMemeber)
        {
            if (conceptMemeber.IsConceptInfo)
            {
                if ((conceptMemeber.GetValue(existingConcept) as IConceptInfo).GetKey() !=
                (conceptMemeber.GetValue(newConcept) as IConceptInfo).GetKey())
                    return false;
                else
                    return true;
            }
            else
            {
                var value1 = conceptMemeber.GetValue(existingConcept);
                var value2 = conceptMemeber.GetValue(newConcept);

                if (value1 == null)
                    return value2 == null;
                else if (value2 == null)
                    return false;
                else
                    return value1.Equals(value2);
            }
        }

        /// <summary>
        /// Since DSL parser and macro evaluators return stub references, this function replaces each reference with actual instance of the referenced concept.
        /// Note: This method could handle circular dependencies between the concepts, but for simplicity of the implementation this is currently not supported.
        /// </summary>
        private void ReplaceReferencesWithFullConcepts(ConceptDescription conceptDesc)
        {
            var references = ConceptMembers.Get(conceptDesc.Concept).Where(member => member.IsConceptInfo)
                .Select(member => new UnresolvedReference(conceptDesc, member));

            foreach (var reference in references)
                ReplaceReferenceWithFullConceptOrMarkUnresolved(reference);

            if (conceptDesc.UnresolvedDependencies == 0)
                MarkResolvedConcept(conceptDesc);
        }

        private void ReplaceReferenceWithFullConceptOrMarkUnresolved(UnresolvedReference reference)
        {
            if (reference.ReferencedKey == null)
            {
                string errorMessage = $"Property '{reference.Member.Name}' is not initialized.";

                if (reference.Dependant.Concept is IAlternativeInitializationConcept)
                    errorMessage += $" Check if the InitializeNonparsableProperties function of IAlternativeInitializationConcept implementation at {reference.Dependant.Concept.GetType().Name} is implemented properly.";

                throw new DslConceptSyntaxException(reference.Dependant.Concept, errorMessage);
            }

            IConceptInfo referencedConcept;
            if (_resolvedConceptsByKey.TryGetValue(reference.ReferencedKey, out referencedConcept))
                reference.Member.SetMemberValue(reference.Dependant.Concept, referencedConcept);
            else
            {
                _unresolvedConceptsByReference.Add(reference.ReferencedKey, reference);
                reference.Dependant.UnresolvedDependencies++;
            }
        }

        /// <summary>
        /// Resolves concepts that were waiting for this concept to be resolved.
        /// </summary>
        private void MarkResolvedConcept(ConceptDescription resolved)
        {
            _logger.Trace(() => "New concept with resolved references: " + resolved.Key);

            _resolvedConcepts.Add(resolved.Concept);
            _resolvedConceptsByKey.Add(resolved.Key, resolved.Concept);
            foreach (var index in _dslModelIndexes)
                index.Add(resolved.Concept);

            List<UnresolvedReference> unresolvedReferences;
            if (_unresolvedConceptsByReference.TryGetValue(resolved.Key, out unresolvedReferences))
            {
                foreach (var unresolved in unresolvedReferences)
                {
                    if (unresolved.Dependant.UnresolvedDependencies <= 0)
                        throw new FrameworkException($"Internal error while resolving references of '{unresolved.Dependant.Concept.GetUserDescription()}'."
                            + $" The concept has {unresolved.Dependant.UnresolvedDependencies} unresolved dependencies,"
                            + $" but it is marked as unresolved dependency to '{unresolved.ReferencedStub.GetUserDescription()}'.");

                    unresolved.Member.SetMemberValue(unresolved.Dependant.Concept, resolved.Concept);

                    if (--unresolved.Dependant.UnresolvedDependencies == 0)
                        MarkResolvedConcept(unresolved.Dependant);
                }

                _unresolvedConceptsByReference.Remove(resolved.Key);
            }
        }

        public int UnresolvedConceptsCount()
        {
            return _unresolvedConceptsByReference
                .SelectMany(concepts => concepts.Value.Select(concept => concept.Dependant.Key))
                .Distinct().Count();
        }

        public void ReportErrorForUnresolvedConcepts()
        {
            _performanceLogger.Write(_addConceptsStopwatch, "AddNewConceptsAndReplaceReferences total time.");
            _performanceLogger.Write(_validateDuplicateStopwatch, "ValidateNewConceptSameAsExisting total time.");

            var unresolvedConcepts = _unresolvedConceptsByReference.SelectMany(ucbr => ucbr.Value);
            if (unresolvedConcepts.Any())
            {
                _logger.Trace(() => string.Join("\r\n",
                    unresolvedConcepts.Select(u =>
                        $"Unresolved dependency to '{u.ReferencedKey}' <= '{u.Dependant.Concept.GetUserDescription()}',"
                        + $" {u.Member.Name} = '{u.ReferencedStub.GetUserDescription()}' ({u.Dependant.UnresolvedDependencies} left)")));

                var internalError = unresolvedConcepts.FirstOrDefault(u => u.Dependant.UnresolvedDependencies <= 0);
                if (internalError != null)
                    throw new FrameworkException($"Internal error while resolving references of '{internalError.Dependant.Concept.GetUserDescription()}'."
                        + $" The concept has {internalError.Dependant.UnresolvedDependencies} unresolved dependencies,"
                        + $" but it is marked as unresolved dependency to '{internalError.ReferencedStub.GetUserDescription()}'.");

                var rootUnresolved = GetRootUnresolvedConcept(unresolvedConcepts.First());
                throw new DslConceptSyntaxException(rootUnresolved.Dependant.Concept,
                    $"Referenced concept is not defined in DSL scripts: '{rootUnresolved.ReferencedStub.GetUserDescription()}'.");
            }
        }

        private UnresolvedReference GetRootUnresolvedConcept(UnresolvedReference unresolved, List<string> trail = null)
        {
            if (trail == null)
                trail = new List<string>();

            if (!_givenConceptsByKey.ContainsKey(unresolved.ReferencedKey))
                return unresolved;

            var referencedUnresolvedConcept = _unresolvedConceptsByReference.SelectMany(ucbr => ucbr.Value)
                .FirstOrDefault(u => u.Dependant.Key == unresolved.ReferencedKey);

            if (referencedUnresolvedConcept == null)
                throw new FrameworkException($"Internal error when resolving concept's references: '{unresolved.Dependant.Concept.GetUserDescription()}' has unresolved reference to '{unresolved.ReferencedStub.GetUserDescription()}', but the referenced concept is not marked as unresolved.");

            if (trail.Contains(referencedUnresolvedConcept.Dependant.Key))
            {
                var circularGroup = trail
                    .Skip(trail.IndexOf(referencedUnresolvedConcept.Dependant.Key))
                    .Concat(new[] { referencedUnresolvedConcept.Dependant.Key });
                throw new DslConceptSyntaxException(referencedUnresolvedConcept.Dependant.Concept,
                    "Circular dependency detected in concept's references: " + string.Join(" => ", circularGroup) + ".");
            }

            trail.Add(referencedUnresolvedConcept.Dependant.Key);
            return GetRootUnresolvedConcept(referencedUnresolvedConcept, trail);
        }

        /// <summary>
        /// This method sorts concepts so that if concept A references concept B,
        /// in the resulting list B will be positioned somewhere before A.
        /// This will allow code generators to safely assume that the code for referenced concept B
        /// is already generated before the concept A inserts an additional code snippet into it.
        /// </summary>
        /// <param name="initialSort">
        /// Initial sorting will reduce variations in the generated application source
        /// that are created by different macro evaluation order on each deployment.
        /// </param>
        public void SortReferencesBeforeUsingConcept(InitialConceptsSort initialSort)
        {
            var sw = Stopwatch.StartNew();

            // Initial sort:

            if (initialSort != InitialConceptsSort.None)
            {
                var sortComparison = new Dictionary<InitialConceptsSort, Comparison<IConceptInfo>>
                {
                    { InitialConceptsSort.Key, (a, b) => a.GetKey().CompareTo(b.GetKey()) },
                    { InitialConceptsSort.KeyDescending, (a, b) => -a.GetKey().CompareTo(b.GetKey()) },
                };

                _resolvedConcepts.Sort(sortComparison[initialSort]);
                _performanceLogger.Write(sw, $"SortReferencesBeforeUsingConcept: Initial sort by {initialSort}.");
            }

            // Extract dependencies:

            var dependencies = new List<(IConceptInfo dependsOn, IConceptInfo dependent)>(_resolvedConcepts.Count);
            foreach (var concept in _resolvedConcepts)
                foreach (ConceptMember member in ConceptMembers.Get(concept))
                    if (member.IsConceptInfo)
                        dependencies.Add(((IConceptInfo)member.GetValue(concept), concept));

            var dependents = dependencies.ToMultiDictionary(d => d.dependsOn, d => d.dependent);
            var countOfDependencies = dependencies.GroupBy(d => d.dependent).ToDictionary(group => group.Key, group => group.Count());
            var conceptsWithoutDependencies = _resolvedConcepts.Where(c => !countOfDependencies.ContainsKey(c)).ToList();
            conceptsWithoutDependencies = conceptsWithoutDependencies.Where(c => c is InitializationConcept)
                .Concat(conceptsWithoutDependencies.Where(c => !(c is InitializationConcept)))
                .ToList();
            var newWithoutDependencies = new List<IConceptInfo>(conceptsWithoutDependencies.Count / 2);

            // Sort by dependencies:

            var sortedList = new List<IConceptInfo>(_resolvedConcepts.Count);

            while (sortedList.Count < _resolvedConcepts.Count)
            {
                if (!conceptsWithoutDependencies.Any())
                {
                    var unresolvedConcepts = countOfDependencies.Where(cd => cd.Value > 0).Select(c => c.Key).ToList();
                    int reportConcepts = 5;
                    throw new FrameworkException($"Circular dependency detected while sorting concepts." +
                        $" Unresolved {unresolvedConcepts.Count} concepts:" +
                        $" {string.Join(", ", unresolvedConcepts.Take(reportConcepts).Select(c => c.GetUserDescription()))}" +
                        (unresolvedConcepts.Count > reportConcepts ? ", ..." : "."));
                }

                // Using a top-down breadth-first sorting, instead of a recursion, to provide more stable sort.
                // For example, adding a filter that references an entity should not change the ordering of the entities.
                newWithoutDependencies.Clear();
                foreach (var concept in conceptsWithoutDependencies)
                {
                    sortedList.Add(concept);
                    foreach (var dependent in dependents.Get(concept))
                    {
                        int remaining = --countOfDependencies[dependent];
                        if (remaining == 0)
                            newWithoutDependencies.Add(dependent);
                    }
                }

                conceptsWithoutDependencies.Clear();
                conceptsWithoutDependencies.AddRange(newWithoutDependencies);
            }

            // Result:

            if (conceptsWithoutDependencies.Any())
                throw new FrameworkException($"Unexpected internal state: Remaining {conceptsWithoutDependencies.Count} {nameof(conceptsWithoutDependencies)} to resolve.");
            if (countOfDependencies.Any(c => c.Value != 0))
                throw new FrameworkException($"Unexpected internal state: Remaining {nameof(countOfDependencies)} != 0.");

            _resolvedConcepts.Clear();
            _resolvedConcepts.AddRange(sortedList);
            _performanceLogger.Write(sw, "SortReferencesBeforeUsingConcept.");
        }
    }
}
