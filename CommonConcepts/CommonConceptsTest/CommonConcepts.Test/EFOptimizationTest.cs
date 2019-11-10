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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using System.Linq.Expressions;
using Rhetos.TestCommon;
using System.Text.RegularExpressions;
using Rhetos.Persistence;
using System.Collections;

namespace CommonConcepts.Test
{
    [TestClass]
    public class EFOptimizationTest
    {
        [TestMethod]
        public void WhereContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var record = repository.Test12.Entity3.Query().WhereContains(new List<Guid>() { id1, id2 }, x => x.Entity2.Entity1.ID).Single();
                Assert.AreEqual(id3, record.ID);
            }
        }

        [TestMethod]
        public void MultipleWhereContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var records = repository.Test12.Entity3.Query()
                    .WhereContains(new List<Guid>() { id1 }, e3 => e3.Entity2.Entity1.ID)
                    .WhereContains(new List<Guid>() { id2 }, e3 => e3.Entity2.ID)
                    .Select(e3 => e3.ID);
                Console.WriteLine(records.Expression.ToString());
                Console.WriteLine(records.ToString());
                Assert.AreEqual(id3.ToString(), TestUtility.DumpSorted(records));
            }
        }

        [TestMethod]
        public void WhereEmptyContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var records = repository.Test12.Entity3.Query()
                    .WhereContains(new List<Guid>() { }, e3 => e3.Entity2.Entity1.ID)
                    .Select(e3 => e3.ID);
                Console.WriteLine(records.Expression.ToString());
                Console.WriteLine(records.ToString());
                Assert.AreEqual("", TestUtility.DumpSorted(records));
            }
        }

        [TestMethod]
        public void OptimizeContainsIdsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var listIds = new List<Guid> { id1 };

                var containsExpression = EFExpression.OptimizeContains<Common.Queryable.Test12_Entity3>(x => listIds.Contains(x.Entity2.Entity1ID.Value));
                var whereContainsSql = repository.Test12.Entity3.Query().Where(containsExpression).ToString();
                Console.WriteLine(whereContainsSql);
                Assert.IsFalse(whereContainsSql.ToLower().Contains(id1.ToString().ToLower()));
                Assert.AreEqual(id3, repository.Test12.Entity3.Query().Where(containsExpression).Single().ID);
            }
        }

        [TestMethod]
        public void OptimizeContainsNullableIdsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var listIds = new List<Guid?> { id1 };

                var containsExpression = EFExpression.OptimizeContains<Common.Queryable.Test12_Entity3>(x => listIds.Contains(x.Entity2.Entity1ID.Value));
                var whereContainsSql = repository.Test12.Entity3.Query().Where(containsExpression).ToString();
                Assert.IsFalse(whereContainsSql.ToLower().Contains(id1.ToString().ToLower()));
                Assert.AreEqual(id3, repository.Test12.Entity3.Query().Where(containsExpression).Single().ID);

                var containsExpression2 = EFExpression.OptimizeContains<Common.Queryable.Test12_Entity3>(x => listIds.Contains(x.Entity2.Entity1ID));
                var whereContainsSql2 = repository.Test12.Entity3.Query().Where(containsExpression2).ToString();
                Assert.IsFalse(whereContainsSql2.ToLower().Contains(id1.ToString().ToLower()));
                Assert.AreEqual(id3, repository.Test12.Entity3.Query().Where(containsExpression2).Single().ID);
            }
        }

        [TestMethod]
        public void OptimizeContainsNullableWithNullValueTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                    });

                var listWithNullValue = new List<Guid?> { null };

                var record1 = repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains<Common.Queryable.Test12_Entity1>(x => listWithNullValue.Contains(x.GuidProperty))).Single();
                Assert.AreEqual(id1, record1.ID);
            }
        }

        [TestMethod]
        public void OptimizeContainsNullableWithMixedNullValueTest()
        {
            foreach (bool useDatabaseNullSemantics in new[] { false, true })
                using (var container = new RhetosTestContainer())
                {
                    container.SetUseDatabaseNullSemantics(useDatabaseNullSemantics);
                    var repository = container.Resolve<Common.DomRepository>();

                    var id1 = Guid.NewGuid();
                    var id2 = Guid.NewGuid();
                    container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                        {
                            $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                            $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id2}';",
                        });
                    Console.WriteLine($"ID1: {id1}");
                    Console.WriteLine($"ID2: {id2}");

                    var listWithNullValue = new List<Guid?> { null, id1 };

                    Expression<Func<Common.Queryable.Test12_Entity1, bool>> containsId = x => listWithNullValue.Contains(x.ID);
                    CompareQueries(
                        basicQuery: repository.Test12.Entity1.Query().Where(containsId),
                        optimizedQuery: repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains(containsId)),
                        testDescription: $"on ID dbNull={useDatabaseNullSemantics}");

                    Expression<Func<Common.Queryable.Test12_Entity1, bool>> containsGuidProperty = x => listWithNullValue.Contains(x.GuidProperty);
                    CompareQueries(
                        basicQuery: repository.Test12.Entity1.Query().Where(containsGuidProperty),
                        optimizedQuery: repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains(containsGuidProperty)),
                        testDescription: $"on GuidProperty dbNull={useDatabaseNullSemantics}");

                    Expression<Func<Common.Queryable.Test12_Entity1, bool>> notContainsId = x => !listWithNullValue.Contains(x.ID);
                    CompareQueries(
                        basicQuery: repository.Test12.Entity1.Query().Where(notContainsId),
                        optimizedQuery: repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains(notContainsId)),
                        testDescription: $"notin on ID dbNull={useDatabaseNullSemantics}");

                    Expression<Func<Common.Queryable.Test12_Entity1, bool>> notContainsGuidProperty = x => !listWithNullValue.Contains(x.GuidProperty);
                    CompareQueries(
                        basicQuery: repository.Test12.Entity1.Query().Where(notContainsGuidProperty),
                        optimizedQuery: repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains(notContainsGuidProperty)),
                        testDescription: $"notin on GuidProperty dbNull={useDatabaseNullSemantics}");
                }
        }

        [TestMethod]
        public void OptimizeEnumerableContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var guidArray = new Guid[] { Guid.NewGuid() };
                var sqlQuery = repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains<Common.Queryable.Test12_Entity1>(x => guidArray.Contains(x.ID))).ToString();
                Assert.IsFalse(sqlQuery.ToLower().Contains(guidArray[0].ToString().ToLower()));

                Expression<Func<Common.Queryable.Test12_Entity1, bool>> expression = x => repository.Test12.Entity2.Subquery.Select(y => y.Entity1ID.Value).Contains(x.ID);
                Assert.AreEqual(expression, EFExpression.OptimizeContains(expression), "EFExpression.OptimizeContains should not try to optimize the method Queryable<T>.Contains(T item)");

                var enumerableMock = new IEnumerableMock<Guid>(guidArray.ToList());
                Expression<Func<Common.Queryable.Test12_Entity1, bool>> expression2 = x => enumerableMock.Contains(x.ID);
                Assert.AreEqual(expression2, EFExpression.OptimizeContains(expression2), "EFExpression.OptimizeContains should not try to optimize the method Enumerable<Guid>.Contains(IEnumerable<Guid> items, Guid item) if the items argument is not of type IList<Guid>");
            }
        }

        [TestMethod]
        public void NestedPropertyContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var c = new { Property1 = new {
                    Property2 = new List<Guid> { Guid.NewGuid() } }
                };
                Expression<Func<Common.Queryable.Test12_Entity1, bool>> expression = x => c.Property1.Property2.Contains(x.ID);
                Assert.AreEqual(expression, EFExpression.OptimizeContains(expression), "A nested propty currently is not considered in the optimization process.");
            }
        }

        private void CompareQueries(IQueryable<Common.Queryable.Test12_Entity1> basicQuery, IQueryable<Common.Queryable.Test12_Entity1> optimizedQuery, string testDescription)
        {
            Console.WriteLine($"basicQuery {testDescription}:\r\n{basicQuery}");
            Console.WriteLine($"optimizedQuery {testDescription}:\r\n{optimizedQuery}");

            Assert.AreEqual(
                TestUtility.DumpSorted(basicQuery, item => item.ID),
                TestUtility.DumpSorted(optimizedQuery, item => item.ID),
                $"Query with select: {testDescription}.");

            Assert.AreEqual(
                TestUtility.DumpSorted(basicQuery.ToList(), item => item.ID),
                TestUtility.DumpSorted(optimizedQuery.ToList(), item => item.ID),
                $"Loading all: {testDescription}.");

            Assert.AreEqual(
                IgnoreContains(basicQuery.ToString(), $"basicQuery {testDescription}"),
                IgnoreContains(optimizedQuery.ToString(), $"optimizedQuery {testDescription}"),
                $"Comparing generated SQL queries: {testDescription}.");
        }

        private string IgnoreContains(string sql, string testDescription)
        {
            // Ignore SQL code for "contains", that is generated by EF:
            const string guidList = @"(cast\('[\w-]+' as uniqueidentifier\)(, )?)*";
            var basicContainsIdsSubquery = new Regex($@"\[\w+\]\.\[\w+\] IN \({guidList}\)");
            sql = basicContainsIdsSubquery.Replace(sql, "<CONTAINS>");

            // Ignore SQL code for "contains", that is generated by EFExpression.OptimizeContains:
            var optimizedContainsIdsSubquery = new Regex($@"\[{EntityFrameworkMapping.StorageModelNamespace}\]\.\[{EFExpression.ContainsIdsFunction}\]\((?<id>.+?), (?<concatenatedIds>.*?)\)", RegexOptions.Singleline);
            sql = optimizedContainsIdsSubquery.Replace(sql, "<CONTAINS>");

            // Ignore "AND argument IS NOT NULL" that is sometimes added by EF, but not by EFExpression.OptimizeContains.
            // (see explanation in ReplaceContainsVisitor.VisitMethodCall)
            if (testDescription == "basicQuery on GuidProperty dbNull=False"
                || testDescription == "basicQuery notin on GuidProperty dbNull=False")
                sql = sql.Replace(
                    "((<CONTAINS>) AND ([Extent1].[GuidProperty] IS NOT NULL))",
                    "(<CONTAINS>)");

            // Ignore different negative and positive variants:
            sql = sql.Replace("NOT (<CONTAINS>)", "<NOT CONTAINS>");
            sql = sql.Replace("(<CONTAINS>) <> 1", "<NOT CONTAINS>");
            sql = sql.Replace("(<CONTAINS>) = 1", "<CONTAINS>");
            sql = sql.Replace("  ", " ");

            return sql;
        }

        [TestMethod]
        public void UsingListConstructorWotChangeExpressionTest()
        {
            var id1 = Guid.NewGuid();
            Expression<Func<Common.Queryable.Test12_Entity1, bool>> originalExpression = x => new List<Guid?> { id1 }.Contains(x.GuidProperty);
            var optimizedExpression = EFExpression.OptimizeContains(originalExpression);
            Assert.AreEqual(originalExpression, optimizedExpression);
        }

        private class IEnumerableMock<T> : IEnumerable<T>
        {
            List<T> _items;

            public IEnumerableMock()
            {
                _items = new List<T>();
            }

            public IEnumerableMock(List<T> items)
            {
                _items = items;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }
        }

        [TestMethod]
        public void DontEvaluateEnumerable()
        {
            IEnumerable<Guid> items = Enumerable.Range(0, 1).Select<int, Guid>(x => throw new Rhetos.FrameworkException("Enumeration should not be evaluated during optimization."));
            Expression<Func<Guid, bool>> expression = id => items.Contains(id);

            Console.WriteLine(expression);
            expression = EFExpression.OptimizeContains(expression);
            Console.WriteLine(expression);

            TestUtility.ShouldFail<Rhetos.FrameworkException>(
                () => Assert.AreEqual(1, items.Count()),
                "Enumeration should not be evaluated during optimization.");
        }
    }
}
