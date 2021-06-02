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
using Rhetos.CommonConcepts.Test.Mocks;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Processing.DefaultCommands;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class GenericFilterHelperTest
    {
        class C
        {
            public string Name { get; set; }
        }

        /// <summary>
        /// String comparison should be case insensitive, to match the default MS SQL Server settings.
        /// </summary>
        [TestMethod]
        public void StringCaseInsensitiveTest()
        {
            var items = "a1, A2, a3, A4, b1, B2"
                .Split(new[] { ", " }, StringSplitOptions.None)
                .Select(name => new C { Name = name }).ToList();
            items.Add(new C { Name = null });

            Assert.AreEqual("b1, B2", TestFilterByName(items, "startswith", "b"));
            Assert.AreEqual("b1, B2", TestFilterByName(items, "startswith", "B"));
            Assert.AreEqual("b1, B2", TestFilterByName(items, "contains", "b"));
            Assert.AreEqual("b1, B2", TestFilterByName(items, "contains", "B"));
            Assert.AreEqual("A2", TestFilterByName(items, "equal", "a2"));
            Assert.AreEqual("<null>, a1, a3, A4, b1, B2", TestFilterByName(items, "notequal", "a2"));
            Assert.AreEqual("a1, A2", TestFilterByName(items, "less", "A3"));
            Assert.AreEqual("a1, A2, a3", TestFilterByName(items, "lessequal", "A3"));
            Assert.AreEqual("a3, A4, b1, B2", TestFilterByName(items, "greater", "a2"));
            Assert.AreEqual("A2, a3, A4, b1, B2", TestFilterByName(items, "greaterequal", "a2"));

            items.Add(new C { Name = "" });

            Assert.AreEqual("", TestFilterByName(items, "equal", ""));
            Assert.AreEqual("<null>", TestFilterByName(items, "equal", null));
            Assert.AreEqual("<null>, a1, A2, a3, A4, b1, B2", TestFilterByName(items, "notequal", ""));
            Assert.AreEqual(", a1, A2, a3, A4, b1, B2", TestFilterByName(items, "notequal", null));
        }

        private static string TestFilterByName(IEnumerable<C> items, string operation, object value)
        {
            var genericFilter = new FilterCriteria("Name", operation, value);
            Console.WriteLine("genericFilter: " + genericFilter.Property + " " + genericFilter.Operation + " " + genericFilter.Value);

            var genericFilterHelper = new GenericFilterHelper(new DomainObjectModelMock(), new DataStructureReadParametersStub());
            var filterObject = genericFilterHelper.ToFilterObjects(typeof(C).FullName, new FilterCriteria[] { genericFilter }).Single();
            Console.WriteLine("filterObject.FilterType: " + filterObject.FilterType.FullName);
            var filterExpression = genericFilterHelper.ToExpression<C>((IEnumerable<PropertyFilter>)filterObject.Parameter);

            var filteredItems = items.AsQueryable().Where(filterExpression).ToList();
            return TestUtility.DumpSorted(filteredItems, item => item.Name ?? "<null>");
        }

        [TestMethod]
        public void SortAndPaginateKeepsOriginalQueryableType()
        {
            var readCommand = new ReadCommandInfo
            {
                OrderByProperties = new[] { new OrderByProperty { Property = "Name", Descending = true } },
                Skip = 1,
                Top = 2,
            };

            IQueryable<object> query = new[] { "a", "b", "c", "d" }.AsQueryable().Select(name => new C { Name = name });
            Console.WriteLine(query.GetType());
            Assert.IsTrue(query is IQueryable<C>);

            var result = GenericFilterHelper.SortAndPaginate<object>(query, readCommand);
            Assert.AreEqual("c, b", TestUtility.Dump(result, item => ((C)item).Name));
            Console.WriteLine(result.GetType());
            Assert.IsTrue(result is IQueryable<C>);
        }

        class Entity2
        {
            public string Name { get; set; }
            public int Size { get; set; }
            public int Ignore { get; set; }
        }

        [TestMethod]
        public void Sort()
        {
            var readCommand = new ReadCommandInfo
            {
                OrderByProperties = new[]
                {
                    new OrderByProperty { Property = "Name", Descending = true },
                    new OrderByProperty { Property = "Size", Descending = false },
                }
            };

            IQueryable<Entity2> query = new[]
            {
                new Entity2 { Name = "b", Size = 2, Ignore = 1 },
                new Entity2 { Name = "b", Size = 2, Ignore = 2 },
                new Entity2 { Name = "b", Size = 3, Ignore = 3 },
                new Entity2 { Name = "b", Size = 1, Ignore = 4 },
                new Entity2 { Name = "a", Size = 1, Ignore = 5 },
                new Entity2 { Name = "c", Size = 3, Ignore = 6 }
            }.AsQueryable();

            query = query.OrderBy(item => item.Ignore); // SortAndPaginate should ignore previous ordering, not append to it.

            var result = GenericFilterHelper.SortAndPaginate(query, readCommand);
            Console.WriteLine(result.ToString());
            Assert.AreEqual(
                "c3, b1, b2, b2, b3, a1",
                TestUtility.Dump(result, item => item.Name + item.Size));
        }

        [TestMethod]
        public void OperationInString()
        {
            var items = "a1, A2, a3, A4, b1, B2"
                .Split(new[] { ", " }, StringSplitOptions.None)
                .Select(name => new C { Name = name }).ToList();
            items.Add(new C { Name = null });
            items.Add(new C { Name = "" });

            Assert.AreEqual("b1, B2", TestFilterByName(items, "in", new[] { "b1", "B2", "B2", "b3" }));
            Assert.AreEqual("b1, B2", TestFilterByName(items, "in", new List<string> { "b1", "B2", "B2", "b3" }));
            Assert.AreEqual(", <null>, a1, A2, a3, A4", TestFilterByName(items, "NotIn", new[] { "b1", "B2", "B2", "b3" }));
            Assert.AreEqual("<null>, a1, A2, a3, A4", TestFilterByName(items, "NotIn", new[] { "b1", "B2", "B2", "b3", "" }));
            Assert.AreEqual(", a1, A2, a3, A4", TestFilterByName(items, "NotIn", new[] { "b1", "B2", "B2", "b3", null }));
        }

        [TestMethod]
        public void OperationInGuid()
        {
            var idnull = (Guid?)null;
            var id1 = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id2 = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id3 = new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id4 = new Guid(4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var items = new[]
            {
                new { ID = id1, RefID = idnull, Name = "1" },
                new { ID = id2, RefID = (Guid?)id2, Name = "2" },
                new { ID = id3, RefID = (Guid?)id3, Name = "3" },
            };

            // Guid property, Guid array:
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("ID", "In", new Guid[] { id1, id2, id2, id4 }, items), item => item.Name));
            Assert.AreEqual("3", TestUtility.DumpSorted(TestFilter("ID", "NotIn", new Guid[] { id1, id2, id2, id4 }, items), item => item.Name));

            // Guid property, Guid list:
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("ID", "In", new List<Guid> { id1, id2, id2, id4 }, items), item => item.Name));
            Assert.AreEqual("3", TestUtility.DumpSorted(TestFilter("ID", "NotIn", new List<Guid> { id1, id2, id2, id4 }, items), item => item.Name));

            // Guid property, Guid? array:
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("ID", "In", new Guid?[] { idnull, id1, id2, id2, id4 }, items), item => item.Name));
            Assert.AreEqual("3", TestUtility.DumpSorted(TestFilter("ID", "NotIn", new Guid?[] { idnull, id1, id2, id2, id4 }, items), item => item.Name));

            // Guid property, string array:
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("ID", "In", new string[] { null, "", id1.ToString(), id2.ToString(), id2.ToString(), id4.ToString() }, items), item => item.Name));
            Assert.AreEqual("3", TestUtility.DumpSorted(TestFilter("ID", "NotIn", new string[] { null, "", id1.ToString(), id2.ToString(), id2.ToString(), id4.ToString() }, items), item => item.Name));

            // Guid? property, Guid array:
            Assert.AreEqual("2", TestUtility.DumpSorted(TestFilter("RefID", "In", new Guid[] { id1, id2, id2, id4 }, items), item => item.Name));
            Assert.AreEqual("1, 3", TestUtility.DumpSorted(TestFilter("RefID", "NotIn", new Guid[] { id1, id2, id2, id4 }, items), item => item.Name));

            // Guid? property, string array:
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("RefID", "In", new string[] { null, id1.ToString(), id2.ToString(), id2.ToString(), id4.ToString() }, items), item => item.Name));
            Assert.AreEqual("3", TestUtility.DumpSorted(TestFilter("RefID", "NotIn", new string[] { null, id1.ToString(), id2.ToString(), id2.ToString(), id4.ToString() }, items), item => item.Name));
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("RefID", "In", new string[] { "", id1.ToString(), id2.ToString(), id2.ToString(), id4.ToString() }, items), item => item.Name));
            Assert.AreEqual("3", TestUtility.DumpSorted(TestFilter("RefID", "NotIn", new string[] { "", id1.ToString(), id2.ToString(), id2.ToString(), id4.ToString() }, items), item => item.Name));
        }

        private IEnumerable<T> TestFilter<T>(string property, string operation, object value, IEnumerable<T> items)
        {
            var genericFilter = new FilterCriteria(property, operation, value);
            var genericFilterHelper = new GenericFilterHelper(new DomainObjectModelMock(), new DataStructureReadParametersStub());
            var filterObject = genericFilterHelper.ToFilterObjects(typeof(T).FullName, new FilterCriteria[] { genericFilter }).Single();
            var filterExpression = genericFilterHelper.ToExpression<T>((IEnumerable<PropertyFilter>)filterObject.Parameter);
            var filteredItems = items.AsQueryable().Where(filterExpression).ToList();
            return filteredItems;
        }

        [TestMethod]
        public void ErrorOnInvalidType()
        {
            var idnull = (Guid?)null;
            var id1 = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var items = new[]
            {
                new { ID = id1, RefID = idnull, Name = "1" },
            };

            TestUtility.ShouldFail<ClientException>(() => TestFilter("ID", "In", new int[] { 1 }, items), "Invalid generic filter", "'In'", "'ID'", "Int32", "Guid");
            TestUtility.ShouldFail<ClientException>(() => TestFilter("RefID", "In", new int[] { 1 }, items), "Invalid generic filter", "'In'", "'RefID'", "Int32", "Guid");
            TestUtility.ShouldFail<ClientException>(() => TestFilter("Name", "In", new int[] { 1 }, items), "Invalid generic filter", "'In'", "'Name'", "Int32", "String");
            TestUtility.ShouldFail<ClientException>(() => TestFilter("ID", "In", 2, items), "Invalid generic filter", "'In'", "'ID'", "Int32", "Guid");
            TestUtility.ShouldFail<ClientException>(() => TestFilter("Name", "In", null, items), "Invalid generic filter", "'In'", "'Name'", "null", "String");
        }

        [TestMethod]
        public void TolerateInvalidTypeOfEmptyArray()
        {
            // Element type cannot be detected for empty JSON array, therefore the application should
            // tolerate invalid element type of the array is empty.

            var idnull = (Guid?)null;
            var id1 = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id2 = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id3 = new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var items = new[]
            {
                new { ID = id1, RefID = idnull, Name = "1" },
                new { ID = id2, RefID = (Guid?)id2, Name = "2" },
                new { ID = id3, RefID = (Guid?)id3, Name = "3" },
            };

            Assert.AreEqual("", TestUtility.DumpSorted(TestFilter("ID", "In", new int[] { }, items), item => item.Name));
            Assert.AreEqual("1, 2, 3", TestUtility.DumpSorted(TestFilter("ID", "NotIn", new int[] { }, items), item => item.Name));
            Assert.AreEqual("", TestUtility.DumpSorted(TestFilter("RefID", "In", new int[] { }, items), item => item.Name));
            Assert.AreEqual("1, 2, 3", TestUtility.DumpSorted(TestFilter("RefID", "NotIn", new int[] { }, items), item => item.Name));
            Assert.AreEqual("", TestUtility.DumpSorted(TestFilter("Name", "In", new int[] { }, items), item => item.Name));
            Assert.AreEqual("1, 2, 3", TestUtility.DumpSorted(TestFilter("Name", "NotIn", new int[] { }, items), item => item.Name));
            Assert.AreEqual("", TestUtility.DumpSorted(TestFilter("Name", "In", new C[] { }, items), item => item.Name));
            Assert.AreEqual("1, 2, 3", TestUtility.DumpSorted(TestFilter("Name", "NotIn", new C[] { }, items), item => item.Name));
        }

        [TestMethod]
        public void InQueryable()
        {
            var idnull = (Guid?)null;
            var id1 = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id2 = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id3 = new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id4 = new Guid(4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var items = new[]
            {
                new { ID = id1, RefID = idnull, Name = "1" },
                new { ID = id2, RefID = (Guid?)id2, Name = "2" },
                new { ID = id3, RefID = (Guid?)id3, Name = "3" },
            };

            int queryExecutions = 0;
            var query = new[] { 1 }
                .SelectMany(x =>
                {
                    queryExecutions++;
                    return new Guid[] { id1, id2, id2, id4 }.AsQueryable();
                }).AsQueryable();

            Assert.AreEqual(0, queryExecutions);
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("ID", "In", query, items), item => item.Name));
            Assert.AreEqual(items.Length, queryExecutions);
            Assert.AreEqual("3", TestUtility.DumpSorted(TestFilter("ID", "NotIn", query, items), item => item.Name));
            Assert.AreEqual(2 * items.Length, queryExecutions);
            Assert.AreEqual("2", TestUtility.DumpSorted(TestFilter("RefID", "In", query, items), item => item.Name));
            Assert.AreEqual(3 * items.Length, queryExecutions);
            Assert.AreEqual("1, 2", TestUtility.DumpSorted(TestFilter("ID", "In", query.Cast<Guid?>(), items), item => item.Name));
            Assert.AreEqual(4 * items.Length, queryExecutions);
        }

        [TestMethod]
        public void EqualsSimpleFilter()
        {
            var tests = new (string Filter1, string Filter2, bool ExpectedEqual)[]
            {
                ("Common.RowPermissionsReadItems", "Common.RowPermissionsReadItems, Bookstore.Service, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", true),
                ("string[]", "string[],", true),
                ("string[]", "string[],1", true),
                ("string[]", "string,", false),
                ("string[]", "string", false),
                (null, "string", false),
                (null, "string,", false),
                (null, "string,1", false),
                ("", "string", false),
                ("", "string,", false),
                ("", "string,1", false),
            };

            foreach (var test in tests)
            {
                var testInfo = $"Testing: {test}";
                Console.WriteLine(testInfo);
                Assert.AreEqual(test.ExpectedEqual, GenericFilterHelper.EqualsSimpleFilter(new FilterCriteria { Filter = test.Filter1 }, test.Filter2), testInfo);
                Assert.AreEqual(test.ExpectedEqual, GenericFilterHelper.EqualsSimpleFilter(new FilterCriteria { Filter = test.Filter2 }, test.Filter1), testInfo);
                Assert.AreEqual(test.ExpectedEqual, GenericFilterHelper.EqualsSimpleFilter(new FilterCriteria { Filter = test.Filter1, Operation = "Matches" }, test.Filter2), testInfo);
                Assert.AreEqual(test.ExpectedEqual, GenericFilterHelper.EqualsSimpleFilter(new FilterCriteria { Filter = test.Filter2, Operation = "Matches" }, test.Filter1), testInfo);
            }
        }

        [TestMethod]
        public void EqualsSimpleFilterUnsupported()
        {
            const string filterName = "Test.Filter";
            Assert.IsFalse(GenericFilterHelper.EqualsSimpleFilter(new FilterCriteria { Filter = filterName, Operation = "Matches", Value = "parameter" }, filterName));
            Assert.IsFalse(GenericFilterHelper.EqualsSimpleFilter(new FilterCriteria { Filter = filterName, Operation = "Matches", Value = "parameter" }, filterName));
            Assert.IsFalse(GenericFilterHelper.EqualsSimpleFilter(new FilterCriteria { Property = filterName, Operation = "Equals", Value = "Value" }, filterName));
        }
    }
}
