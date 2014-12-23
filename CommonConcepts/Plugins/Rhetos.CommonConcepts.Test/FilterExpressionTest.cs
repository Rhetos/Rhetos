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
    public class FilterExpressionTest
    {
        class MockQuery
        {
            public int CallCount = 0;

            public IQueryable<int> Query()
            {
                return new[] { 0 }.AsQueryable()
                    .SelectMany(x => f());
            }

            IEnumerable<int> f()
            {
                CallCount++;
                return new[] { 1, 2, 3, 4 };
            }

            public string GetWherePart(IQueryable<int> query)
            {
                string expression = query.Expression.ToString();
                string expectedPrefix = Query().Expression.ToString();
                if (expression.StartsWith(expectedPrefix))
                    return expression.Substring(expectedPrefix.Length);
                else
                    return "New query: " + expression;
            }
        }

        [TestMethod]
        public void TestMockQuery()
        {
            var mockQuery = new MockQuery();
            var q = mockQuery.Query();
            Assert.AreEqual(0, mockQuery.CallCount);

            Assert.AreEqual(10, q.Sum());
            Assert.AreEqual(1, mockQuery.CallCount);

            Assert.AreEqual(4, q.Count());
            Assert.AreEqual(2, mockQuery.CallCount);

            Assert.AreEqual("1, 2, 3, 4", TestUtility.Dump(q));
            Assert.AreEqual(3, mockQuery.CallCount);

            Assert.AreEqual("2, 3, 4", TestUtility.Dump(q.Where(x => x >= 2)));
            Assert.AreEqual(4, mockQuery.CallCount);
        }

        private IQueryable<int> GetOpt(Expression<Func<int, bool>> expression, IQueryable<int> query)
        {
            return FilterExpression<int>.OptimizedWhere(query, expression);
        }

        [TestMethod]
        public void EmptyFilter()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("New query: System.Int32[]", mockQuery.GetWherePart(filteredQuery), "Result should be a newly constructed empty array");
            Assert.AreEqual("", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(0, mockQuery.CallCount, "Empty filter should be optimized to not use original query.");
        }


        [TestMethod]
        public void AllowNone()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x => false);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("New query: System.Int32[]", mockQuery.GetWherePart(filteredQuery), "Result should be a newly constructed empty array");
            Assert.AreEqual("", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(0, mockQuery.CallCount, "Empty filter should be optimized to not use original query.");
        }

        [TestMethod]
        public void AllowAll()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => x1 > 2);
            filterExpression.Include(x2 => true);
            filterExpression.Include(x3 => x3 > 3);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("", mockQuery.GetWherePart(filteredQuery), "Optimized - No where part");
            Assert.AreEqual("1, 2, 3, 4", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(1, mockQuery.CallCount);
        }

        [TestMethod]
        public void AllowSome2()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => x1 >= 4);
            filterExpression.Include(x2 => x2 <= 2);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual(".Where(x1 => ((x1 >= 4) OrElse (x1 <= 2)))", mockQuery.GetWherePart(filteredQuery));
            Assert.AreEqual("1, 2, 4", TestUtility.DumpSorted(filteredQuery));
            Assert.AreEqual(1, mockQuery.CallCount);
        }

        [TestMethod]
        public void DenyNone()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Exclude(x => false);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("New query: System.Int32[]", mockQuery.GetWherePart(filteredQuery), "Result should be a newly constructed empty array");
            Assert.AreEqual("", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(0, mockQuery.CallCount, "Empty filter should be optimized to not use original query.");
        }

        [TestMethod]
        public void AllowAllDenyNone()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => true);
            filterExpression.Exclude(x2 => false);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("", mockQuery.GetWherePart(filteredQuery), "Optimized - No where part");
            Assert.AreEqual("1, 2, 3, 4", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(1, mockQuery.CallCount);
        }

        [TestMethod]
        public void AllowAllDenyAll()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => true);
            filterExpression.Exclude(x2 => true);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("New query: System.Int32[]", mockQuery.GetWherePart(filteredQuery), "Result should be a newly constructed empty array");
            Assert.AreEqual("", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(0, mockQuery.CallCount, "Empty filter should be optimized to not use original query.");
        }

        [TestMethod]
        public void AllowSomeDenyNone()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => x1 >= 3);
            filterExpression.Exclude(x2 => false);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual(".Where(x1 => (x1 >= 3))", mockQuery.GetWherePart(filteredQuery));
            Assert.AreEqual("3, 4", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(1, mockQuery.CallCount);
        }

        [TestMethod]
        public void AllowSome2DenyAll()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => x1 >= 3);
            filterExpression.Exclude(x2 => true);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("New query: System.Int32[]", mockQuery.GetWherePart(filteredQuery), "Result should be a newly constructed empty array");
            Assert.AreEqual("", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(0, mockQuery.CallCount, "Empty filter should be optimized to not use original query.");
        }

        [TestMethod]
        public void DenySome()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Exclude(x => x >= 3);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual("New query: System.Int32[]", mockQuery.GetWherePart(filteredQuery), "Result should be a newly constructed empty array");
            Assert.AreEqual("", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(0, mockQuery.CallCount, "Empty filter should be optimized to not use original query.");
        }

        [TestMethod]
        public void AllowAllDenySome2()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => true);
            filterExpression.Exclude(x2 => x2 > 2);
            filterExpression.Exclude(x3 => x3 < 2);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual(".Where(x2 => (Not((x2 > 2)) AndAlso Not((x2 < 2))))", mockQuery.GetWherePart(filteredQuery));
            Assert.AreEqual("2", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(1, mockQuery.CallCount);
        }

        [TestMethod]
        public void AllowSome2DenySome2()
        {
            var mockQuery = new MockQuery();
            var query = mockQuery.Query();

            var filterExpression = new FilterExpression<int>();
            filterExpression.Include(x1 => x1 >= 2);
            filterExpression.Include(x2 => x2 >= 4);
            filterExpression.Exclude(x3 => x3 >= 4);
            filterExpression.Exclude(x4 => x4 >= 5);
            var filteredQuery = GetOpt(filterExpression.GetFilter(), query);

            Assert.AreEqual(".Where(x1 => (((x1 >= 2) OrElse (x1 >= 4)) AndAlso (Not((x1 >= 4)) AndAlso Not((x1 >= 5)))))", mockQuery.GetWherePart(filteredQuery));
            Assert.AreEqual("2, 3", TestUtility.Dump(filteredQuery));
            Assert.AreEqual(1, mockQuery.CallCount);
        }
    }
}
