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

            TestFilterByName("b1, B2", items, "startswith", "b");
            TestFilterByName("b1, B2", items, "startswith", "B");
            TestFilterByName("b1, B2", items, "contains", "b");
            TestFilterByName("b1, B2", items, "contains", "B");
            TestFilterByName("A2", items, "equal", "a2");
            TestFilterByName("<null>, a1, a3, A4, b1, B2", items, "notequal", "a2");
            TestFilterByName("a1, A2", items, "less", "A3");
            TestFilterByName("a1, A2, a3", items, "lessequal", "A3");
            TestFilterByName("a3, A4, b1, B2", items, "greater", "a2");
            TestFilterByName("A2, a3, A4, b1, B2", items, "greaterequal", "a2");

            items.Add(new C { Name = "" });

            TestFilterByName("", items, "equal", "");
            TestFilterByName("<null>", items, "equal", null);
            TestFilterByName("<null>, a1, A2, a3, A4, b1, B2", items, "notequal", "");
            TestFilterByName(", a1, A2, a3, A4, b1, B2", items, "notequal", null);
        }

        private static void TestFilterByName(string expected, IEnumerable<C> items, string operation, object value)
        {
            var genericFilter = new FilterCriteria("Name", operation, value);
            Console.WriteLine("genericFilter: " + genericFilter.Property + " " + genericFilter.Operation + " " + genericFilter.Value);

            var genericFilterHelper = new GenericFilterHelper(new DomainObjectModelMock());
            var filterObject = genericFilterHelper.ToFilterObjects(new FilterCriteria[] { genericFilter }).Single();
            Console.WriteLine("filterObject.FilterType: " + filterObject.FilterType.FullName);
            var filterExpression = genericFilterHelper.ToExpression<C>((IEnumerable<PropertyFilter>)filterObject.Parameter);

            var filteredItems = items.AsQueryable().Where(filterExpression).ToList();
            Assert.AreEqual(expected, TestUtility.DumpSorted(filteredItems, item => item.Name ?? "<null>"), "Testing '" + operation + " " + value + "'.");
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

            TestFilterByName("b1, B2", items, "in", new[] { "b1", "B2", "B2", "b3" });
            TestFilterByName("b1, B2", items, "in", new List<string> { "b1", "B2", "B2", "b3" });
            TestFilterByName(", <null>, a1, A2, a3, A4", items, "NotIn", new[] { "b1", "B2", "B2", "b3" });
            TestFilterByName("<null>, a1, A2, a3, A4", items, "NotIn", new[] { "b1", "B2", "B2", "b3", "" });
            TestFilterByName(", a1, A2, a3, A4", items, "NotIn", new[] { "b1", "B2", "B2", "b3", null });
        }

        [TestMethod]
        public void OperationInGuid()
        {
            var idnull = (Guid?)null;
            var id1 = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id2 = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id3 = new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            var id4 = new Guid(4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var items = new ListOfTuples<Guid, Guid?, string>
            {
                { id1, idnull, "1" },
                { id2, id2, "2" },
                { id3, id3, "3" }
            }.Select(item => new
            {
                ID = item.Item1,
                RefID = item.Item2,
                Name = item.Item3
            }).ToList();

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
            var genericFilterHelper = new GenericFilterHelper(new DomainObjectModelMock());
            var filterObject = genericFilterHelper.ToFilterObjects(new FilterCriteria[] { genericFilter }).Single();
            var filterExpression = genericFilterHelper.ToExpression<T>((IEnumerable<PropertyFilter>)filterObject.Parameter);
            var filteredItems = items.AsQueryable().Where(filterExpression).ToList();
            return filteredItems;
        }
    }
}
