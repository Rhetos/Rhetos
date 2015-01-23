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
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    public static class Graph
    {
        /// <summary>
        /// Return a list that contains all elements from the given list and all elements that depend on them.
        /// </summary>
        /// <param name="dependencies">Dependency: Item2 depends on Item1.</param>
        public static List<T> IncludeDependents<T>(IEnumerable<T> list, IEnumerable<Tuple<T, T>> dependencies)
        {
            var result = new List<T>(list);
            var alreadyInserted = new HashSet<T>(list);

            var dependents = new MultiDictionary<T, T>();
            foreach (var dependency in dependencies)
                dependents.Add(dependency.Item1, dependency.Item2);

            foreach (var element in list)
                AddDependents(
                    element,
                    result,
                    dependents,
                    alreadyInserted);

            return result;
        }

        private static void AddDependents<T>(
            T element,
            List<T> result,
            Dictionary<T, List<T>> dependents,
            HashSet<T> alreadyInserted)
        {
            if (dependents.ContainsKey(element))
                foreach (var dependent in dependents[element])
                    if (!alreadyInserted.Contains(dependent))
                    {
                        result.Add(dependent);
                        alreadyInserted.Add(dependent);
                        AddDependents(dependent, result, dependents, alreadyInserted);
                    }
        }

        //==============================================================================

        /// <summary>
        /// Sorts a partially ordered set (directed acyclic graph).
        /// </summary>
        /// <param name="dependencies">Dependency: Item2 depends on Item1.</param>
        public static void TopologicalSort<T>(List<T> list, IEnumerable<Tuple<T, T>> dependencies)
        {
            var dependenciesByDependent = new Dictionary<T, List<T>>();
            foreach (var relation in dependencies)
            {
                List<T> group;
                if (!dependenciesByDependent.TryGetValue(relation.Item2, out group))
                {
                    group = new List<T>();
                    dependenciesByDependent.Add(relation.Item2, group);
                }
                group.Add(relation.Item1);
            }

            var result = new List<T>();
            var givenList = new HashSet<T>(list);
            var processed = new HashSet<T>();
            var analysisStarted = new List<T>();
            foreach (var element in list)
                AddDependenciesBeforeElement(element, result, givenList, dependenciesByDependent, processed, analysisStarted);
            list.Clear();
            list.AddRange(result);
        }

        private static void AddDependenciesBeforeElement<T>(T element, List<T> result, HashSet<T> givenList, Dictionary<T, List<T>> dependencies, HashSet<T> processed, List<T> analysisStarted)
        {
            if (!processed.Contains(element) && givenList.Contains(element))
            {
                if (analysisStarted.Contains(element))
                {
                    int circularReferenceIndex = analysisStarted.IndexOf(element);
                    throw new FrameworkException(String.Format(
                        "Circular dependency detected on elements:\r\n{0}.",
                        String.Join(",\r\n", analysisStarted.GetRange(circularReferenceIndex, analysisStarted.Count - circularReferenceIndex))));
                }
                analysisStarted.Add(element);

                if (dependencies.ContainsKey(element))
                    foreach (T dependency in dependencies[element])
                        AddDependenciesBeforeElement(dependency, result, givenList, dependencies, processed, analysisStarted);

                analysisStarted.RemoveAt(analysisStarted.Count - 1);
                processed.Add(element);
                result.Add(element);
            }
        }

        //==============================================================================

        /// <summary>
        /// Returns a list of nodes (a subset of 'candidates') that can be safely removed in a way
        /// that no other remaining node depends (directly or inderectly) on removed nodes.
        /// </summary>
        /// <param name="candidates">Nodes to be removed.</param>
        /// <param name="dependencies">Dependency: Item2 depends on Item1.</param>
        public static List<T> RemovableLeaves<T>(IEnumerable<T> candidates, IEnumerable<Tuple<T, T>> dependencies)
        {
            CsUtility.Materialize(ref candidates);
            CsUtility.Materialize(ref dependencies);

            dependencies = dependencies.Distinct().ToArray();
            var all = candidates.Union(dependencies.Select(d => d.Item1)).Union(dependencies.Select(d => d.Item2)).ToArray();

            var numberOfDependents = all.ToDictionary(node => node, node => 0);
            foreach (var relation in dependencies)
                numberOfDependents[relation.Item1]++;

            var dependsOn = all.ToDictionary(node => node, node => new List<T>());
            foreach (var relation in dependencies)
                dependsOn[relation.Item2].Add(relation.Item1);

            var removed = new HashSet<T>();
            var candidatesIndex = new HashSet<T>(candidates);
            foreach (var cand in candidates)
                if (numberOfDependents[cand] == 0 && !removed.Contains(cand))
                    RemoveLeaf(cand, removed, numberOfDependents, dependsOn, candidatesIndex);
            return removed.ToList();
        }

        private static void RemoveLeaf<T>(T leaf, HashSet<T> removed, Dictionary<T, int> numberOfDependents, Dictionary<T, List<T>> dependsOn, HashSet<T> candidatesIndex)
        {
            removed.Add(leaf);
            foreach (var dep in dependsOn[leaf])
                if (candidatesIndex.Contains(dep) && numberOfDependents[dep] > 0)
                {
                    int newCount = --numberOfDependents[dep];
                    if (newCount == 0)
                        RemoveLeaf(dep, removed, numberOfDependents, dependsOn, candidatesIndex);
                }
        }

        //==============================================================================

        public static void SortByGivenOrder<TItem, TKey>(TItem[] items, IEnumerable<TKey> expectedKeyOrder, Func<TItem, TKey> itemKeySelector)
        {
            var positionByKey = GetPositionByKey(expectedKeyOrder);
            var itemsOrder = items.Select(item => positionByKey.GetValue(itemKeySelector(item), CannotFindKeyError<TItem>)).ToArray();
            Array.Sort(itemsOrder, items);
        }

        public static void SortByGivenOrder<TItem, TKey>(List<TItem> items, IEnumerable<TKey> expectedKeyOrder, Func<TItem, TKey> itemKeySelector)
        {
            var positionByKey = GetPositionByKey(expectedKeyOrder);
            var itemsComparer = new IndirectComparer<TItem, TKey>(positionByKey, itemKeySelector, CannotFindKeyError<TItem>);
            items.Sort(itemsComparer);
        }

        public static Dictionary<TKey, int> GetPositionByKey<TKey>(IEnumerable<TKey> expectedKeyOrder)
        {
            return expectedKeyOrder.Select((key, index) => new { key, index }).ToDictionary(item => item.key, item => item.index);
        }

        public static string CannotFindKeyError<TItem>()
        {
            return "Given array expectedKeyOrder does not contain key '{0}' that is present in given items (" + typeof(TItem).FullName + ").";
        }

        class IndirectComparer<TItem, TKey> : IComparer<TItem>
        {
            Dictionary<TKey, int> _positionByKey;
            Func<TItem, TKey> _itemKeySelector;
            Func<string> _cannotFindKeyError;

            public IndirectComparer(Dictionary<TKey, int> positionByKey, Func<TItem, TKey> itemKeySelector, Func<string> cannotFindKeyError)
            {
                _positionByKey = positionByKey;
                _itemKeySelector = itemKeySelector;
                _cannotFindKeyError = cannotFindKeyError;
            }

            public int Compare(TItem x, TItem y)
            {
                return _positionByKey.GetValue(_itemKeySelector(x), _cannotFindKeyError) - _positionByKey.GetValue(_itemKeySelector(y), _cannotFindKeyError);
            }
        }

        //==============================================================================

        /// <summary>
        /// Returns given direct relations and all the indirect relations that can be achieved by combining two or more direct relations.
        /// See: reachability, transitive closure.
        /// </summary>
        public static List<Tuple<T, T>> GetIndirectRelations<T>(ICollection<Tuple<T, T>> directRelations)
        {
            var allItems = directRelations.Select(r => r.Item1).Concat(directRelations.Select(r => r.Item2)).Distinct();

            var targetsBySource = new Dictionary<T, HashSet<T>>();
            foreach (var item in allItems)
                targetsBySource[item] = new HashSet<T>(new[] { item });

            foreach (var relation in directRelations)
                foreach (var targets in targetsBySource.Where(t => t.Value.Contains(relation.Item1)))
                    targets.Value.UnionWith(targetsBySource[relation.Item2]);

            var allRelations = targetsBySource
                .SelectMany(targets => targets.Value
                    .Where(target => !(targets.Key.Equals(target)))
                    .Select(target => Tuple.Create(targets.Key, target)))
                .Concat(directRelations.Where(r => r.Item1.Equals(r.Item2)).Distinct())
                .ToList();

            return allRelations;
        }
    }
}
