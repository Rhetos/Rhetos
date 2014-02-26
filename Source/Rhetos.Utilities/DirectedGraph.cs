﻿/*
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
    public static class DirectedGraph
    {
        /// <summary>
        /// Return a list that contains all elements from the given list and all elements that depend on them.
        /// </summary>
        /// <param name="dependencies">Dependency: Item2 depends on Item1.</param>
        public static List<T> IncludeDependents<T>(IEnumerable<T> list, IEnumerable<Tuple<T, T>> dependencies)
        {
            var result = new List<T>(list);
            var alreadyInserted = new HashSet<T>(list);

            var dependents = dependencies
                .GroupBy(dep => dep.Item1)
                .ToDictionary(g => g.Key, g => g.Select(dep => dep.Item2).ToList());

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
            var processed = new HashSet<T>();
            var analysisStarted = new List<T>();
            foreach (var element in list)
                AddDependenciesBeforeElement(element, result, list, dependenciesByDependent, processed, analysisStarted);
            list.Clear();
            list.AddRange(result);
        }

        private static void AddDependenciesBeforeElement<T>(T element, List<T> result, List<T> list, Dictionary<T, List<T>> dependencies, HashSet<T> processed, List<T> analysisStarted)
        {
            if (!processed.Contains(element) && list.Contains(element))
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
                        AddDependenciesBeforeElement(dependency, result, list, dependencies, processed, analysisStarted);

                analysisStarted.RemoveAt(analysisStarted.Count - 1);
                processed.Add(element);
                result.Add(element);
            }
        }

        /// <summary>
        /// Returns a list of nodes (a subset of 'candidates') that can be safely removed in a way
        /// that no other remaining node depends (directly or inderectly) on removed nodes.
        /// </summary>
        /// <param name="candidates">Nodes to be removed.</param>
        /// <param name="dependencies">Dependency: Item2 depends on Item1.</param>
        public static List<T> RemovableLeaves<T>(List<T> candidates, IEnumerable<Tuple<T, T>> dependencies)
        {
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

        public static void SortByGivenOrder<TItem, TKey>(TItem[] items, TKey[] expectedKeyOrder, Func<TItem, TKey> itemKeySelector)
        {
            var expectedIndex = expectedKeyOrder.Select((key, index) => new { key, index }).ToDictionary(item => item.key, item => item.index);

            string cannotFindKeyError = "Given array expectedKeyOrder does not contain key '{0}' that is present in given items (" + typeof(TItem).FullName + ").";
            var itemsOrder = items.Select(item => expectedIndex.GetValue(itemKeySelector(item), cannotFindKeyError)).ToArray();

            Array.Sort(itemsOrder, items);
        }
    }
}
