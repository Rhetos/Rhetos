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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class GraphTest
    {
        [TestMethod]
        public void IncludeDependenciesTest()
        {
            var list = new[] {1, 2, 3};
            var dependencies = new[]
            {
                Tuple.Create(1, 2),
                Tuple.Create(4, 10),
                Tuple.Create(4, 9),
                Tuple.Create(2, 4),
                Tuple.Create(5, 6),
                Tuple.Create(0, 2)
            };

            var expected = new[] {1, 2, 3, 4, 9, 10};

            var actual = Graph.IncludeDependents(list, dependencies);

            Assert.AreEqual(DumpSet(expected), DumpSet(actual));
        }

        private static string DumpSet(IEnumerable<int> expected)
        {
            string s = "(" + string.Join(",", expected.OrderBy(x => x)) + ")";
            Console.WriteLine(s);
            return s;
        }

        //=======================================================================

        private static void TestTopologicalSort(string list, string dependencies, string expected)
        {
            List<string> actual = list.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Graph.TopologicalSort(
                actual,
                dependencies.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(pair => Tuple.Create(pair.Split('-')[0], pair.Split('-')[1])).ToList());
            Assert.AreEqual(expected, string.Join(" ", actual));
        }


        private static void ExpectException(string list, string dependencies, string circular)
        {
            List<string> actual = list.Split(' ').ToList();
            Exception exception = null;
            try
            {
                Graph.TopologicalSort(
                    actual,
                    dependencies.Split(' ').Select(pair => Tuple.Create(pair.Split('-')[0], pair.Split('-')[1])).ToList());
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.IsNotNull(exception, "Exception expected.");

            System.Diagnostics.Debug.WriteLine(exception.Message);
            List<string> circ = circular.Split(' ').ToList();
            foreach (var c in circ)
                if (!exception.Message.Contains(c))
                    Assert.Fail("Exception message should contain: " + c);
            foreach (var s in actual.Except(circ))
                if (exception.Message.Contains(s))
                    Assert.Fail("Exception message should NOT contain: " + s);
        }

        [TestMethod]
        public void TopologicalSort_SimpleTest()
        {
            TestTopologicalSort("4 3 2 1", "1-2 2-3 3-4", "1 2 3 4");
            TestTopologicalSort("1 2 3 4", "1-2 2-3 3-4", "1 2 3 4");
            TestTopologicalSort("1 2 4 3", "1-2 2-3 3-4", "1 2 3 4");
            TestTopologicalSort("1 3 2 4", "1-2 2-3 3-4", "1 2 3 4");
            TestTopologicalSort("1 3 4 2", "1-2 2-3 3-4", "1 2 3 4");
            TestTopologicalSort("1 4 2 3", "1-2 2-3 3-4", "1 2 3 4");
            TestTopologicalSort("1 4 3 2", "1-2 2-3 3-4", "1 2 3 4");
        }

        [TestMethod]
        public void TopologicalSort_StableSortTest()
        {
            // Should keep order where not defined otherwise.

            TestTopologicalSort("1 2 3 4", "", "1 2 3 4");
            TestTopologicalSort("4 3 2 1", "", "4 3 2 1");
            TestTopologicalSort("4 3 2 1", "1-2", "4 3 1 2");
            TestTopologicalSort("4 3 2 1", "3-4", "3 4 2 1");
            TestTopologicalSort("5 4 3 2 1", "2-3 3-4", "5 2 3 4 1");
        }

        [TestMethod]
        public void TopologicalSort_CircularReference()
        {
            ExpectException("11 22 33 44 55", "22-33 33-22", "22 33");
            ExpectException("11 22 33 44 55", "22-33 33-44 44-22", "22 33 44");
            ExpectException("11 22 33 44 55", "22-22", "22");
        }

        [TestMethod]
        public void TopologicalSort_ReferenceOutsideTheSystem()
        {
            // Should ignore references that are not related to given list. It simplifies use when sorting different subgroups in a system.

            TestTopologicalSort("3 2 1", "1-2 2-3 4-1", "1 2 3");
            TestTopologicalSort("3 2 1", "1-2 2-3 1-4", "1 2 3");
        }

        //=======================================================================

        /// <param name="dependencies">Second item depends on first item in a relation.</param>
        private static void TestRemovableLeaves(string candidates, string dependencies, string expectedToBeRemovable)
        {
            Console.WriteLine("TEST: " + candidates + ", " + dependencies + ", " + expectedToBeRemovable + ".");
            var actual = Graph.RemovableLeaves(
                candidates.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                dependencies.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(pair => Tuple.Create(pair.Split('-')[0], pair.Split('-')[1])).ToList());
            Assert.AreEqual(expectedToBeRemovable, string.Join(" ", actual.OrderBy(x => x)));
        }

        [TestMethod]
        public void RemovableLeaves_SimpleTest()
        {
            TestRemovableLeaves("", "", "");
            TestRemovableLeaves("1", "", "1");

            TestRemovableLeaves("2", "2-1", "");
            TestRemovableLeaves("1", "2-1", "1");
            TestRemovableLeaves("", "2-1", "");
            TestRemovableLeaves("1 2", "2-1", "1 2");

            TestRemovableLeaves("2", "3-2 2-1", "");
            TestRemovableLeaves("2 3", "3-2 2-1", "");
            TestRemovableLeaves("1 2", "3-2 2-1", "1 2");
            TestRemovableLeaves("1 3", "3-2 2-1", "1");

            TestRemovableLeaves("3", "3-1 3-2", "");
            TestRemovableLeaves("2 3", "3-1 3-2", "2");
            TestRemovableLeaves("1 2", "3-1 3-2", "1 2");
            TestRemovableLeaves("1 2 3", "3-1 3-2", "1 2 3");

            TestRemovableLeaves("2 3", "2-1 3-1", "");
            TestRemovableLeaves("1 2", "2-1 3-1", "1 2");
            TestRemovableLeaves("1 2 3", "2-1 3-1", "1 2 3");
            TestRemovableLeaves("1", "2-1 3-1", "1");
        }

        [TestMethod]
        public void RemovableLeaves_Duplicates()
        {
            TestRemovableLeaves("1 2 2 1 2", "1-2 1-3 1-2 1-3", "2");
            TestRemovableLeaves("1 2 3 3 3 3", "1-2 1-3 1-2 1-3", "1 2 3");
            TestRemovableLeaves("1 1 1 1", "1-2 1-3 1-2 1-3", "");
        }

        [TestMethod]
        public void RemovableLeaves_CircularReferences()
        {
            TestRemovableLeaves("", "1-2 2-3 3-1", "");
            TestRemovableLeaves("2 3", "1-2 2-3 3-1", "");
            // TestRemovableLeaves("1 2 3", "1-2 2-3 3-1", "1 2 3"); This feature is not implemented. Current algorithm cannot detect all possible removable leaves in a case of circular dependencies.

            TestRemovableLeaves("4", "1-2 2-3 3-1 1-4", "4");
            TestRemovableLeaves("4 1 2", "1-2 2-3 3-1 1-4", "4");
            TestRemovableLeaves("1 2 3", "1-2 2-3 3-1 1-4", "");
            //TestRemovableLeaves("1 2 3 4", "1-2 2-3 3-1 1-4", "1 2 3 4"); This feature is not implemented. Current algorithm cannot detect all possible removable leaves in a case of circular dependencies.
        }

        //=======================================================================

        void TestGetIndirectRelations(string directRelationsString, string expectedIndirectRelationsString)
        {
            var directRelations = directRelationsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(relation =>
            {
                var items = relation.Split('-');
                return Tuple.Create(items[0], items[1]);
            }).ToList();

            Console.WriteLine("Parsed input: " + string.Join(" ",
                directRelations.Select(relation => relation.Item1 + "-" + relation.Item2)));

            var indirectRelations = Graph.GetIndirectRelations(directRelations);

            string indirectRelationsReport = string.Join(" ",
                indirectRelations.GroupBy(relation => relation.Item1).Select(
                    group => group.Key + '-' + string.Join("", group.Select(relation => relation.Item2).OrderBy(x => x)))
                    .OrderBy(x => x));

            Console.WriteLine("Output: " + expectedIndirectRelationsString);
            Assert.AreEqual(expectedIndirectRelationsString, indirectRelationsReport);
        }

        [TestMethod]
        public void GetIndirectRelations_Simple()
        {
            TestGetIndirectRelations("a-b", "a-b");
            TestGetIndirectRelations("a-b b-c", "a-bc b-c");
            TestGetIndirectRelations("b-c a-b", "a-bc b-c");
            TestGetIndirectRelations("a-b a-c", "a-bc");
            TestGetIndirectRelations("a-c a-b", "a-bc");
            TestGetIndirectRelations("b-a c-a", "b-a c-a");
            TestGetIndirectRelations("c-a b-a", "b-a c-a");
        }

        [TestMethod]
        public void GetIndirectRelations_Empty()
        {
            TestGetIndirectRelations("", "");
        }

        [TestMethod]
        public void GetIndirectRelations_Cyclic()
        {
            TestGetIndirectRelations("a-b b-a", "a-b b-a");
            TestGetIndirectRelations("a-b b-c c-a", "a-bc b-ac c-ab");
            TestGetIndirectRelations("a-b b-c c-a b-z", "a-bcz b-acz c-abz");
            TestGetIndirectRelations("b-z a-b b-c c-a", "a-bcz b-acz c-abz");
        }

        [TestMethod]
        public void GetIndirectRelations_Complex()
        {
            TestGetIndirectRelations("b-a a-b a-c b-d c-d d-e", "a-bcde b-acde c-de d-e");
            TestGetIndirectRelations("d-e c-d b-d a-c a-b b-a", "a-bcde b-acde c-de d-e");
        }

        [TestMethod]
        public void GetIndirectRelations_ExplicitSelfReference()
        {
            TestGetIndirectRelations("a-b b-a b-b", "a-b b-ab");
            TestGetIndirectRelations("a-a", "a-a");
            TestGetIndirectRelations("a-b b-c b-b a-a c-c", "a-abc b-bc c-c");
        }

        [TestMethod]
        public void GetIndirectRelations_Redundant()
        {
            TestGetIndirectRelations("a-b b-c c-b c-c a-c c-c a-c", "a-bc b-c c-bc");
        }

        [TestMethod]
        public void SortByGivenOrder()
        {
            TestSortByGivenOrder(4, "a x2 x1 c x3 x0 b", "2 1 3 0");
            TestSortByGivenOrder(4, "x0 x1 x2 x3", "0 1 2 3");
            TestSortByGivenOrder(4, "a x1 c x3 x0 b", "-", "does not contain key 'x2'");
            TestSortByGivenOrder(0, "", "");
        }

        private void TestSortByGivenOrder(int itemsCount, string orderedKeysString, string expectedOrderedItemsString, params string[] errorMessages)
        {
            var items = Enumerable.Range(0, itemsCount);
            var orderedKeys = orderedKeysString.Split(' ');
            var expectedOrder = expectedOrderedItemsString.Split(' ');

            {
                var itemsArray = items.ToArray();
                if (errorMessages.Length == 0)
                {
                    Graph.SortByGivenOrder(itemsArray, orderedKeys, item => "x" + item);
                    Assert.AreEqual(TestUtility.Dump(expectedOrder), TestUtility.Dump(itemsArray));
                }
                else
                    TestUtility.ShouldFail(() => Graph.SortByGivenOrder(itemsArray, orderedKeys, item => "x" + item), errorMessages);
            }

            {
                var itemsList = items.ToArray();
                if (errorMessages.Length == 0)
                {
                    Graph.SortByGivenOrder(itemsList, orderedKeys, item => "x" + item);
                    Assert.AreEqual(TestUtility.Dump(expectedOrder), TestUtility.Dump(itemsList));
                }
                else
                    TestUtility.ShouldFail(() => Graph.SortByGivenOrder(itemsList, orderedKeys, item => "x" + item), errorMessages);
            }
        }
    }
}
