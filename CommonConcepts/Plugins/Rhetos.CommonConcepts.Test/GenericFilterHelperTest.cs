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
using Rhetos.TestCommon;
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
            var filterObject = genericFilterHelper.ToFilterObjects(new FilterCriteria[] { genericFilter }, typeof(C)).Single();
            Console.WriteLine("filterObject.FilterType: " + filterObject.FilterType.FullName);
            var filterExpression = (Expression<Func<C, bool>>)filterObject.Parameter;

            var filteredItems = items.AsQueryable().Where(filterExpression).ToList();
            Assert.AreEqual(expected, TestUtility.DumpSorted(filteredItems, item => item.Name ?? "<null>"), "Testing '" + operation + " " + value + "'.");
        }
    }
}
