﻿/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace CommonConcepts.Test
{
    [TestClass]
    public class GenericFilterTest
    {
        // TODO: Better unit tests.

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

        [TestMethod]
        public void FilterByProperties()
        {
            IQueryable<TestPermissionBrowse> browse = TestPermissionBrowse.Query();

            Console.WriteLine(browse.Count());
            var filterCriterias = new []
                {
                    new FilterCriteria { Property = "Principal", Value = "Domain Users", Operation = "Equal" },
                    new FilterCriteria { Property = "ClaimResource", Value = "Common.Log", Operation = "Equal" }
                };

            browse = browse.Where(GenericFilterWithPagingUtility.ToExpression<TestPermissionBrowse>(filterCriterias));
            Assert.AreEqual(0, browse.Count());
        }

        [TestMethod]
        public void FilterByReferenceProperties()
        {
            IQueryable<TestPermission> permissions = TestPermission.Query();

            Console.WriteLine(permissions.Count());
            var filterCriterias = new []
                {
                    new FilterCriteria { Property = "Claim.ClaimRight", Value = "Read", Operation = "Equal" }
                };

            permissions = permissions.Where(GenericFilterWithPagingUtility.ToExpression<TestPermission>(filterCriterias));
            Assert.AreEqual(0, permissions.Count());
        }

        [TestMethod]
        public void FilterByEmptyConditions()
        {
            IQueryable<TestPermission> permissions = TestPermission.Query();

            Console.WriteLine(permissions.Count());
            List<FilterCriteria> filterCriterias = new List<FilterCriteria>();

            permissions = GenericFilterWithPagingUtility.Filter(permissions, filterCriterias);
            Assert.AreEqual(4, permissions.Count());
        }

        //==================================================================

        private static void FilterName(Common.DomRepository repository, string operation, string value, string expectedCodes)
        {
            Console.WriteLine("TEST NAME: " + operation + " " + value);
            var source = repository.TestGenericFilter.Simple.Query();
            var result = GenericFilterWithPagingUtility.Filter(source, new[] { new FilterCriteria { Property = "Name", Operation = operation, Value = value } });
            Assert.AreEqual(expectedCodes, TestUtility.DumpSorted(result, item => item.Code.ToString()));
        }

        private static readonly string[] ComperisonOperations = new[] { "equal", "notequal", "less", "lessequal", "greater", "greaterequal" };

        private static void FilterCode(Common.DomRepository repository, string operation, string value, string expectedCodes)
        {
            Console.WriteLine("TEST CODE: " + operation + " " + value);
            var source = repository.TestGenericFilter.Simple.Query();
            var result = GenericFilterWithPagingUtility.Filter(source, new[] { new FilterCriteria { Property = "Code", Operation = operation, Value =
                ComperisonOperations.Contains(operation) ? (object) int.Parse(value) : value } });
            Assert.AreEqual(expectedCodes, TestUtility.DumpSorted(result, item => item.Code.ToString()));
        }

        [TestMethod]
        public void FilterStringOperations()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestGenericFilter.Simple;",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 1, 'abc1';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 2, 'abc2';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 3, 'def3';",
                    });

                var repository = new Common.DomRepository(executionContext);
                FilterName(repository, "equal", "abc2", "2");
                FilterName(repository, "notequal", "abc2", "1, 3");
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
                FilterName(repository, "contains", "b", "1, 2");
                FilterName(repository, "contains", "c2", "2");
                FilterName(repository, "contains", "abc2", "2");
                FilterName(repository, "contains", "d", "3");
                FilterName(repository, "contains", "", "1, 2, 3");
            }
        }

        [TestMethod]
        public void FilterIntOperations()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestGenericFilter.Simple;",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 1, 'abc1';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 2, 'abc2';",
                        "INSERT INTO TestGenericFilter.Simple (Code, Name) SELECT 3, 'def3';",
                    });

                var repository = new Common.DomRepository(executionContext);
                FilterCode(repository, "equal", "2", "2");
                FilterCode(repository, "notequal", "2", "1, 3");
                FilterCode(repository, "less", "2", "1");
                FilterCode(repository, "lessequal", "2", "1, 2");
                FilterCode(repository, "greater", "2", "3");
                FilterCode(repository, "greaterequal", "2", "2, 3");
                FilterCode(repository, "startswith", "2", "2");
                FilterCode(repository, "startswith", "", "1, 2, 3");
                FilterCode(repository, "startswith", "7", "");
                FilterCode(repository, "contains", "2", "2");
                FilterCode(repository, "contains", "", "1, 2, 3");
                FilterCode(repository, "contains", "7", "");
            }
        }

        private static void FilterStart(Common.DomRepository repository, string value, string expectedCodes)
        {
            Console.WriteLine("TEST DateIn: " + value);
            var source = repository.TestGenericFilter.Simple.Query();
            var result = GenericFilterWithPagingUtility.Filter(source, new[] { new FilterCriteria { Property = "Start", Operation = "DateIn", Value = value } });
            Assert.AreEqual(expectedCodes, TestUtility.DumpSorted(result, item => item.Code.ToString()));
        }

        [TestMethod]
        public void FilterDateIn()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
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

                var repository = new Common.DomRepository(executionContext);

                FilterStart(repository, "2010", "");
                FilterStart(repository, "2011", "1");
                FilterStart(repository, "2012", "2, 3, 4, 5, 6");
                FilterStart(repository, "2013", "7");
                FilterStart(repository, "2014", "");

                FilterStart(repository, "2011-02", "");
                FilterStart(repository, "2012-01", "2");
                FilterStart(repository, "2012-02", "3, 4, 5");
                FilterStart(repository, "2012-03", "6");

                FilterStart(repository, "2012-01-31", "2");
                FilterStart(repository, "2012-02-01", "");
                FilterStart(repository, "2012-02-02", "3, 4");
                FilterStart(repository, "2012-02-03", "");
                FilterStart(repository, "2012-02-29", "5");
                FilterStart(repository, "2012-03-01", "6");
                FilterStart(repository, "2012-12-31", "");

                // Valid alternative formats:
                FilterStart(repository, "2012-3", "6");
                FilterStart(repository, "2012-03-1", "6");
                FilterStart(repository, "2012-3-01", "6");
                FilterStart(repository, "2012-3-1", "6");

                // Error handling:
                TestUtility.ShouldFail(() => FilterStart(repository, "123", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "12345", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-123", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-1-", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-12-", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-11-11-", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-11-11-11", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-1-1-1", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-234-12", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "1234-12-234", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "12345-1", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "12345-11", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "12345-1-1", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "12345-11-11", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "11-11-11", ""), "invalid format", "format");
                TestUtility.ShouldFail(() => FilterStart(repository, "11-11-1112", ""), "invalid format", "format");

                TestUtility.ShouldFail(() => FilterStart(repository, "2011-02-29", ""), "invalid date");
                TestUtility.ShouldFail(() => FilterStart(repository, "2011-13-01", ""), "invalid month");
            }
        }
    }
}
