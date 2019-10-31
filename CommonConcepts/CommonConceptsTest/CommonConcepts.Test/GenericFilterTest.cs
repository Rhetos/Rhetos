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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System.Linq.Expressions;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos;

namespace CommonConcepts.Test
{
    [TestClass]
    public class GenericFilterTest
    {
        class TestClaim
        {
            public virtual string ClaimResource { get; set; }
            public virtual string ClaimRight { get; set; }

            public static readonly List<TestClaim> TestData = new List<TestClaim>
            {
                new TestClaim { ClaimResource = "res1", ClaimRight = "cr1"},
                new TestClaim { ClaimResource = "res2", ClaimRight = "cr2"}
            };
            public static IQueryable<TestClaim> Query() { return TestData.AsQueryable(); }
        }

        class TestPrincipal
        {
            public virtual string Name { get; set; }

            public static readonly List<TestPrincipal> TestData = new List<TestPrincipal>
            {
                new TestPrincipal { Name = "pri1 "},
                new TestPrincipal { Name = "pri2 "}
            };
            public static IQueryable<TestPrincipal> Query() { return TestData.AsQueryable(); }
        }

        class TestPermission
        {
            public virtual TestPrincipal Principal { get; set; }
            public virtual TestClaim Claim { get; set; }
            public virtual bool IsAuthorized { get; set; }

            public static readonly List<TestPermission> TestData = new List<TestPermission>
            {
                new TestPermission { Claim = TestClaim.TestData[0], Principal = TestPrincipal.TestData[0], IsAuthorized = true },
                new TestPermission { Claim = TestClaim.TestData[0], Principal = TestPrincipal.TestData[1], IsAuthorized = false },
                new TestPermission { Claim = TestClaim.TestData[1], Principal = TestPrincipal.TestData[0], IsAuthorized = false },
                new TestPermission { Claim = TestClaim.TestData[1], Principal = TestPrincipal.TestData[1], IsAuthorized = true }
            };
            public static IQueryable<TestPermission> Query() { return TestData.AsQueryable(); }
        }

        class TestPermissionBrowse
        {
            public virtual string ClaimResource { get; set; }
            public virtual string ClaimRight { get; set; }
            public virtual string Principal { get; set; }
            public virtual bool IsAuthorized { get; set; }

            public static readonly List<TestPermissionBrowse> TestData = TestPermission.TestData.Select(p => new TestPermissionBrowse
            {
                ClaimResource = p.Claim.ClaimResource,
                ClaimRight = p.Claim.ClaimRight,
                Principal = p.Principal.Name,
                IsAuthorized = p.IsAuthorized
            }).ToList();
            public static IQueryable<TestPermissionBrowse> Query() { return TestData.AsQueryable(); }
        }

        private static Expression<Func<TEntity, bool>> GenericFilterHelperToExpression<TEntity>(IEnumerable<FilterCriteria> propertyFiltersCriteria)
        {
            if (!propertyFiltersCriteria.Any())
                return item => true;

            using (var container = new RhetosTestContainer())
            {
                var gfh = container.Resolve<GenericFilterHelper>();
                var filters = gfh.ToFilterObjects(propertyFiltersCriteria);
                var propertyFilters = (IEnumerable<PropertyFilter>)filters.Single().Parameter;
                var propertyFilterExpression = (Expression<Func<TEntity, bool>>)gfh.ToExpression(propertyFilters, typeof(TEntity));
                return propertyFilterExpression;
            }
        }

        private static IQueryable<TEntity> GenericFilterHelperFilter<TEntity>(IQueryable<TEntity> items, IEnumerable<FilterCriteria> propertyFilters)
        {
            var expr = GenericFilterHelperToExpression<TEntity>(propertyFilters);
            return items.Where(expr);
        }

        [TestMethod]
        public void FilterByProperties()
        {
            IQueryable<TestPermissionBrowse> browse = TestPermissionBrowse.Query();

            Console.WriteLine(browse.Count());
            var propertyFilters = new[]
                {
                    new FilterCriteria { Property = "Principal", Value = "Domain Users", Operation = "Equal" },
                    new FilterCriteria { Property = "ClaimResource", Value = "Common.Log", Operation = "Equal" }
                };

            browse = browse.Where(GenericFilterHelperToExpression<TestPermissionBrowse>(propertyFilters));
            Assert.AreEqual(0, browse.Count());
        }

        [TestMethod]
        public void FilterByReferenceProperties()
        {
            IQueryable<TestPermission> permissions = TestPermission.Query();

            Console.WriteLine(permissions.Count());
            var propertyFilters = new[]
                {
                    new FilterCriteria { Property = "Claim.ClaimRight", Value = "Read", Operation = "Equal" }
                };

            permissions = permissions.Where(GenericFilterHelperToExpression<TestPermission>(propertyFilters));
            Assert.AreEqual(0, permissions.Count());
        }

        [TestMethod]
        public void FilterByEmptyConditions()
        {
            IQueryable<TestPermission> permissions = TestPermission.Query();

            Console.WriteLine(permissions.Count());
            List<FilterCriteria> propertyFilters = new List<FilterCriteria>();

            permissions = GenericFilterHelperFilter(permissions, propertyFilters);
            Assert.AreEqual(4, permissions.Count());
        }

        //==================================================================

        private static void FilterName(Common.DomRepository repository, string operation, string value, string expectedCodes)
        {
            Console.WriteLine("TEST NAME: " + operation + " " + value);
            var source = repository.TestGenericFilter.Simple.Query();
            var result = GenericFilterHelperFilter(source, new[] { new FilterCriteria { Property = "Name", Operation = operation, Value = value } });
            Assert.AreEqual(expectedCodes, TestUtility.DumpSorted(result, item => item.Code.ToString()));
        }

        private static readonly string[] SameTypeOperations = new[] { "equals", "equal", "notequals", "notequal", "less", "lessequal", "greater", "greaterequal" };

        [TestMethod]
        public void FilterStringOperations()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestGenericFilter.Simple;",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 1, 'abc1';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 2, 'abc2';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 3, 'def3';",
                    });

                var repository = container.Resolve<Common.DomRepository>();
                FilterName(repository, "equals", "abc2", "2");
                FilterName(repository, "notequals", "abc2", "1, 3");
                FilterName(repository, "less", "abc2", "1");
                FilterName(repository, "lessequal", "abc2", "1, 2");
                FilterName(repository, "less", "a", "");
                FilterName(repository, "lessequal", "a", "");
                FilterName(repository, "less", "d", "1, 2");
                FilterName(repository, "lessequal", "d", "1, 2");
                FilterName(repository, "greater", "abc2", "3");
                FilterName(repository, "greaterequal", "abc2", "2, 3");
                FilterName(repository, "startswith", "a", "1, 2");
                FilterName(repository, "startswith", "abc", "1, 2");
                FilterName(repository, "startswith", "abc2", "2");
                FilterName(repository, "startswith", "", "1, 2, 3");
                FilterName(repository, "endswith", "a", "");
                FilterName(repository, "endswith", "1", "1");
                FilterName(repository, "endswith", "def3", "3");
                FilterName(repository, "endswith", "", "1, 2, 3");
                FilterName(repository, "contains", "b", "1, 2");
                FilterName(repository, "contains", "c2", "2");
                FilterName(repository, "contains", "abc2", "2");
                FilterName(repository, "contains", "d", "3");
                FilterName(repository, "contains", "x", "");
                FilterName(repository, "contains", "", "1, 2, 3");
                FilterName(repository, "notcontains", "b", "3");
                FilterName(repository, "notcontains", "c2", "1, 3");
                FilterName(repository, "notcontains", "abc2", "1, 3");
                FilterName(repository, "notcontains", "d", "1, 2");
                FilterName(repository, "notcontains", "", "");
            }
        }

        [TestMethod]
        public void FilterIntOperations()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestGenericFilter.Simple;",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 1, 'abc1';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 2, 'abc2';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 12, 'abc12';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 3, 'def3';",
                    });

                var repository = container.Resolve<Common.DomRepository>();
                FilterCode(repository, "equals", "2", "2");
                FilterCode(repository, "notequals", "2", "1, 12, 3");
                FilterCode(repository, "less", "2", "1");
                FilterCode(repository, "lessequal", "2", "1, 2");
                FilterCode(repository, "greater", "2", "12, 3");
                FilterCode(repository, "greaterequal", "2", "12, 2, 3");
                FilterCode(repository, "startswith", "1", "1, 12");
                FilterCode(repository, "startswith", "12", "12");
                FilterCode(repository, "startswith", "", "1, 12, 2, 3");
                FilterCode(repository, "startswith", "7", "");
                FilterCode(repository, "endswith", "2", "12, 2");
                FilterCode(repository, "endswith", "12", "12");
                FilterCode(repository, "endswith", "", "1, 12, 2, 3");
                FilterCode(repository, "endswith", "7", "");
                FilterCode(repository, "contains", "2", "12, 2");
                FilterCode(repository, "contains", "12", "12");
                FilterCode(repository, "contains", "", "1, 12, 2, 3");
                FilterCode(repository, "contains", "7", "");
                FilterCode(repository, "notcontains", "2", "1, 3");
                FilterCode(repository, "notcontains", "12", "1, 2, 3");
                FilterCode(repository, "notcontains", "", "");
                FilterCode(repository, "notcontains", "7", "1, 12, 2, 3");
            }
        }

        private static void FilterCode(Common.DomRepository repository, string operation, string value, string expectedCodes)
        {
            Console.WriteLine("TEST CODE: " + operation + " " + value);
            var source = repository.TestGenericFilter.Simple.Query();
            var result = GenericFilterHelperFilter(source, new[] { new FilterCriteria { Property = "Code", Operation = operation, Value =
                SameTypeOperations.Contains(operation) ? (object) int.Parse(value) : value } });
            Assert.AreEqual(expectedCodes, TestUtility.DumpSorted(result, item => item.Code.ToString()));
        }

        private static void FilterStartDateIn(Common.DomRepository repository, string value, string expectedCodes)
        {
            Console.WriteLine("TEST DateIn: " + value);
            var source = repository.TestGenericFilter.Simple.Query();
            var result = GenericFilterHelperFilter(source, new[] { new FilterCriteria { Property = "Start", Operation = "DateIn", Value = value } });
            Assert.AreEqual(expectedCodes, TestUtility.DumpSorted(result, item => item.Code.ToString()));
        }

        [TestMethod]
        public void FilterDateIn()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestGenericFilter.Simple;",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 1, '2011-12-31';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 2, '2012-01-31';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 3, '2012-02-02';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 4, '2012-02-02 01:02:03';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 5, '2012-02-29';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 6, '2012-03-01';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 7, '2013-01-01';",
                    });

                var repository = container.Resolve<Common.DomRepository>();

                FilterStartDateIn(repository, "2010", "");
                FilterStartDateIn(repository, "2011", "1");
                FilterStartDateIn(repository, "2012", "2, 3, 4, 5, 6");
                FilterStartDateIn(repository, "2013", "7");
                FilterStartDateIn(repository, "2014", "");

                FilterStartDateIn(repository, "2011-02", "");
                FilterStartDateIn(repository, "2012-01", "2");
                FilterStartDateIn(repository, "2012-02", "3, 4, 5");
                FilterStartDateIn(repository, "2012-03", "6");

                FilterStartDateIn(repository, "2012-01-31", "2");
                FilterStartDateIn(repository, "2012-02-01", "");
                FilterStartDateIn(repository, "2012-02-02", "3, 4");
                FilterStartDateIn(repository, "2012-02-03", "");
                FilterStartDateIn(repository, "2012-02-29", "5");
                FilterStartDateIn(repository, "2012-03-01", "6");
                FilterStartDateIn(repository, "2012-12-31", "");

                // Valid alternative formats:
                FilterStartDateIn(repository, "2012-3", "6");
                FilterStartDateIn(repository, "2012-03-1", "6");
                FilterStartDateIn(repository, "2012-3-01", "6");
                FilterStartDateIn(repository, "2012-3-1", "6");

                // Error handling:
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "123", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "12345", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-123", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-1-", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-12-", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-11-11-", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-11-11-11", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-1-1-1", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-234-12", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "1234-12-234", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "12345-1", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "12345-11", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "12345-1-1", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "12345-11-11", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "11-11-11", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "11-11-1112", ""), "invalid format");

                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "2011-02-29", ""), "un-representable DateTime");
                TestUtility.ShouldFail(() => FilterStartDateIn(repository, "2011-13-01", ""), "un-representable DateTime");
            }
        }

        private static void FilterStartDateNotIn(Common.DomRepository repository, string value, string expectedCodes)
        {
            Console.WriteLine("TEST DateIn: " + value);
            var source = repository.TestGenericFilter.Simple.Query();
            var result = GenericFilterHelperFilter(source, new[] { new FilterCriteria { Property = "Start", Operation = "DateNotIn", Value = value } });
            Assert.AreEqual(expectedCodes, TestUtility.DumpSorted(result, item => item.Code.ToString()));
        }

        [TestMethod]
        public void FilterDateNotIn()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestGenericFilter.Simple;",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 1, '2011-12-31';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 2, '2012-01-31';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 3, '2012-02-02';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 4, '2012-02-02 01:02:03';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 5, '2012-02-29';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 6, '2012-03-01';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 7, '2013-01-01';",
                    });

                var repository = container.Resolve<Common.DomRepository>();

                FilterStartDateNotIn(repository, "2010", "1, 2, 3, 4, 5, 6, 7");
                FilterStartDateNotIn(repository, "2011", "2, 3, 4, 5, 6, 7");
                FilterStartDateNotIn(repository, "2012", "1, 7");
                FilterStartDateNotIn(repository, "2013", "1, 2, 3, 4, 5, 6");
                FilterStartDateNotIn(repository, "2014", "1, 2, 3, 4, 5, 6, 7");

                FilterStartDateNotIn(repository, "2011-02", "1, 2, 3, 4, 5, 6, 7");
                FilterStartDateNotIn(repository, "2012-01", "1, 3, 4, 5, 6, 7");
                FilterStartDateNotIn(repository, "2012-02", "1, 2, 6, 7");
                FilterStartDateNotIn(repository, "2012-03", "1, 2, 3, 4, 5, 7");

                FilterStartDateNotIn(repository, "2012-01-31", "1, 3, 4, 5, 6, 7");
                FilterStartDateNotIn(repository, "2012-02-01", "1, 2, 3, 4, 5, 6, 7");
                FilterStartDateNotIn(repository, "2012-02-02", "1, 2, 5, 6, 7");
                FilterStartDateNotIn(repository, "2012-02-03", "1, 2, 3, 4, 5, 6, 7");
                FilterStartDateNotIn(repository, "2012-02-29", "1, 2, 3, 4, 6, 7");
                FilterStartDateNotIn(repository, "2012-03-01", "1, 2, 3, 4, 5, 7");
                FilterStartDateNotIn(repository, "2012-12-31", "1, 2, 3, 4, 5, 6, 7");

                // Valid alternative formats:
                FilterStartDateNotIn(repository, "2012-3", "1, 2, 3, 4, 5, 7");
                FilterStartDateNotIn(repository, "2012-03-1", "1, 2, 3, 4, 5, 7");
                FilterStartDateNotIn(repository, "2012-3-01", "1, 2, 3, 4, 5, 7");
                FilterStartDateNotIn(repository, "2012-3-1", "1, 2, 3, 4, 5, 7");

                // Error handling:
                TestUtility.ShouldFail(() => FilterStartDateNotIn(repository, "123", ""), "invalid format");
                TestUtility.ShouldFail(() => FilterStartDateNotIn(repository, "2011-02-29", ""), "un-representable DateTime");
            }
        }

        [TestMethod]
        public void FilterNullValues()
        {
            using (var container = new RhetosTestContainer())
            {
                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestGenericFilter.Child",
                        "DELETE FROM TestGenericFilter.Simple",

                        // Null and empty string:
                        "INSERT INTO TestGenericFilter.Simple (ID, Code, Name) SELECT '" + parentId + "', -1, 'a'",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT -2, ''",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT -3, null",

                        // Null datetime:
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 1, '2013-02-01'",
                        "INSERT INTO TestGenericFilter.Simple (Code, Start) SELECT 2, null",

                        // Null reference:
                        "INSERT INTO TestGenericFilter.Child (Name, ParentID) SELECT 'c1', '" + parentId + "'",
                        "INSERT INTO TestGenericFilter.Child (Name, ParentID) SELECT 'c2', null",

                        // Null int:
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 0, 'n1'",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT null, 'n2'",
                    });

                var repository = container.Resolve<Common.DomRepository>();
                var simple = repository.TestGenericFilter.Simple.Query();
                var child = repository.TestGenericFilter.Child.Query();

                // Null and empty string:

                IQueryable<TestGenericFilter.Simple> filter1(string property, string operation, object value) =>
                    GenericFilterHelperFilter(simple, new[] {
                        new FilterCriteria { Property = property, Operation = operation, Value = value },
                        new FilterCriteria { Property = "Code", Operation = "less", Value = 0 }});

                Assert.AreEqual("-1", TestUtility.DumpSorted(filter1("Name", "equal", "a"), item => item.Code));
                Assert.AreEqual("-2", TestUtility.DumpSorted(filter1("Name", "equal", ""), item => item.Code));
                Assert.AreEqual("-3", TestUtility.DumpSorted(filter1("Name", "equal", null), item => item.Code));
                Assert.AreEqual("-1", TestUtility.DumpSorted(filter1("Name", "notequal", ""), item => item.Code));
                Assert.AreEqual("-1, -2", TestUtility.DumpSorted(filter1("Name", "notequal", null), item => item.Code));
                Assert.AreEqual("-1, -2", TestUtility.DumpSorted(filter1("Name", "less", "b"), item => item.Code));

                // Null datetime:

                IQueryable<TestGenericFilter.Simple> filter2(string property, string operation, object value) =>
                    GenericFilterHelperFilter(simple, new[] {
                        new FilterCriteria { Property = property, Operation = operation, Value = value },
                        new FilterCriteria { Property = "Code", Operation = "greater", Value = 0 }});

                Assert.AreEqual("1", TestUtility.DumpSorted(filter2("Start", "equal", new DateTime(2013, 2, 1)), item => item.Code));
                Assert.AreEqual("2", TestUtility.DumpSorted(filter2("Start", "equal", null), item => item.Code));

                // Null reference:

                IQueryable<TestGenericFilter.Child> filterChild(string property, string operation, object value) =>
                    GenericFilterHelperFilter(child, new[] {
                        new FilterCriteria { Property = property, Operation = operation, Value = value }});

                Assert.AreEqual("c1", TestUtility.DumpSorted(filterChild("Parent.ID", "equal", parentId), item => item.Name));
                Assert.AreEqual("c2", TestUtility.DumpSorted(filterChild("Parent.ID", "equal", null), item => item.Name));

                // Null int:

                IQueryable<TestGenericFilter.Simple> filterSimple(string property, string operation, object value) =>
                    GenericFilterHelperFilter(simple, new[] {
                        new FilterCriteria { Property = property, Operation = operation, Value = value }});

                Assert.AreEqual("n1", TestUtility.DumpSorted(filterSimple("Code", "equal", 0), item => item.Name));
                Assert.AreEqual("n2", TestUtility.DumpSorted(filterSimple("Code", "equal", null), item => item.Name));
            }
        }

        [TestMethod]
        public void InvalidPropertyNameError()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var childQuery = repository.TestGenericFilter.Child.Query();
                FilterCriteria filter;

                filter = new FilterCriteria { Property = "Parentt", Operation = "equal", Value = null };
                TestUtility.ShouldFail<ClientException>(() => GenericFilterHelperFilter(childQuery, new[] { filter }).ToList(),
                    "generic filter", "property 'Parentt'", "TestGenericFilter", "Child'");
            }
        }

        [TestMethod]
        public void GenericFilterOnLoadedArray()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                foreach (bool preferQuery in new[] { true, false })
                {
                    var filtered = repository.TestComputed.Simple.Read(
                        new[]
                        {
                            new FilterCriteria { Filter = "TestComputed.SpecialLoad", Value = new TestComputed.SpecialLoad { SpecialName = "abc" } },
                            new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "a" }
                        },
                        preferQuery);
                    Assert.AreEqual("abc", TestUtility.DumpSorted(filtered, item => item.Name));
                }
            }
        }

        [TestMethod]
        public void GenericQueryableInSubquery()
        {
            string expectedSql = @"SELECT 
    [Extent1].[ID] AS [ID]
    FROM [TestGenericFilter].[Child] AS [Extent1]
    WHERE  EXISTS (SELECT 
        1 AS [C1]
        FROM [TestGenericFilter].[Simple] AS [Extent2]
        WHERE (N'A' = [Extent2].[Name]) AND ([Extent2].[ID] = [Extent1].[ParentID])
    )";
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var subquery = repository.TestGenericFilter.Simple.Query(item => item.Name == "A").Select(item => item.ID);

                var querySimple = repository.TestGenericFilter.Child.Query(item => subquery.Contains(item.ParentID.Value)).Select(item => item.ID);
                Assert.AreEqual(expectedSql, querySimple.ToString(), "This is just a control query.");

                var genericFilter = new FilterCriteria { Property = "ParentID", Operation = "In", Value = subquery };
                var queryGenericFilter = repository.TestGenericFilter.Child.Query(new[] { genericFilter }).Select(item => item.ID);
                Assert.AreEqual(expectedSql, queryGenericFilter.ToString(), "Generic filter's Value parameter should be implemented as a LINQ subquery as generated a single SQL query.");
            }
        }

        [TestMethod]
        public void OptimizeEqualsGuidTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var id = Guid.NewGuid();

                var sqlQuery1 = repository.TestGenericFilter.Child.Query(new FilterCriteria("ID", "equals", id)).ToString();
                TestUtility.AssertNotContains(sqlQuery1, id.ToString());

                var sqlQuery2 = repository.TestGenericFilter.Child.Query(new FilterCriteria("ParentID", "equals", id)).ToString();
                TestUtility.AssertNotContains(sqlQuery2, id.ToString());

                var nullableId = new Nullable<Guid>(Guid.NewGuid());
                var sqlQuery3 = repository.TestGenericFilter.Child.Query(new FilterCriteria("ID", "equals", nullableId)).ToString();
                TestUtility.AssertNotContains(sqlQuery3, nullableId.Value.ToString());

                var sqlQuery4 = repository.TestGenericFilter.Child.Query(new FilterCriteria("ParentID", "equals", nullableId)).ToString();
                TestUtility.AssertNotContains(sqlQuery4, nullableId.Value.ToString());
            }
        }

        [TestMethod]
        public void OptimizeInGuid()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = context.Repository;

                var s1 = new TestGenericFilter.Simple { Name = "s1" };
                var s2 = new TestGenericFilter.Simple { Name = "s2" };
                var s3 = new TestGenericFilter.Simple { Name = "s3" };
                var s4 = new TestGenericFilter.Simple { Name = "s4" };
                repository.TestGenericFilter.Simple.Insert(s1, s2, s3, s4);

                var c1 = new TestGenericFilter.Child { Name = "c1", ParentID = s1.ID };
                var c2 = new TestGenericFilter.Child { Name = "c2", ParentID = s2.ID };
                var c3 = new TestGenericFilter.Child { Name = "c3", ParentID = s3.ID };
                var c4 = new TestGenericFilter.Child { Name = "c4", ParentID = s4.ID };
                repository.TestGenericFilter.Child.Insert(c1, c2, c3, c4);

                var filter = new[]
                {
                    new FilterCriteria("ID", "in", new List<Guid> { c1.ID, c2.ID, c3.ID, c4.ID }),
                    new FilterCriteria("ID", "notin", new List<Guid> { c1.ID }),
                    new FilterCriteria("ParentID", "notin", new List<Guid> { s2.ID }),
                };

                var q = repository.TestGenericFilter.Child.Query(filter);
                var sql = q.ToString();
                Console.WriteLine(sql);
                Assert.AreEqual("c3, c4", TestUtility.DumpSorted(q, c => c.Name));

                TestUtility.AssertNotContains(q.ToString(), c3.ID.ToString(), "Optimized ID 'in'.");
                TestUtility.AssertNotContains(q.ToString(), c1.ID.ToString(), "Optimized ID 'notin'.");
                TestUtility.AssertNotContains(q.ToString(), s2.ID.ToString(), "Optimized ParentID 'notin'.");
            }
        }
    }
}
