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
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class FullTextSearchTest
    {
        /// <summary>
        /// Data preparation is separated from tests because we need to wait for asynchronous FTS index population on SQL Server.
        /// </summary>
        private static void PrepareData()
        {
            if (!DataPrepared)
                lock (DataPreparedLock)
                {
                    if (!DataPrepared)
                    {
                        // Insert the test data:

                        using (var container = new RhetosTestContainer(true))
                        {
                            var simpleTestData = new[]
                            {
                                new TestFullTextSearch.Simple { Name = "ab", Code = 12 },
                                new TestFullTextSearch.Simple { Name = "abc", Code = 3 },
                                new TestFullTextSearch.Simple { Name = "cd ab", Code = 4 },
                                new TestFullTextSearch.Simple { Name = "123", Code = 56 },
                                new TestFullTextSearch.Simple { Name = "xy", Code = -123 },
                            };

                            var alternativeTestData = new[]
                            {
                                new TestFullTextSearch.AlternativeEntity { AlternativeKey = 1, Text1 = "ab", Text2 = "12" },
                                new TestFullTextSearch.AlternativeEntity { AlternativeKey = 2, Text1 = "abc", Text2 = "3" },
                                new TestFullTextSearch.AlternativeEntity { AlternativeKey = 3, Text1 = "cd ab", Text2 = "4" },
                                new TestFullTextSearch.AlternativeEntity { AlternativeKey = 4, Text1 = "123", Text2 = "56" },
                                new TestFullTextSearch.AlternativeEntity { AlternativeKey = 5, Text1 = "xy", Text2 = "-123" },
                            };

                            var context = container.Resolve<Common.ExecutionContext>();

                            context.GenericRepository<TestFullTextSearch.Simple>().InsertOrUpdateOrDelete(
                                simpleTestData,
                                sameRecord: GenericComparer<TestFullTextSearch.Simple>(item => new { item.Name, item.Code }),
                                sameValue: (a, b) => true,
                                filterLoad: new FilterAll(),
                                assign: null);

                            context.GenericRepository<TestFullTextSearch.AlternativeEntity>().InsertOrUpdateOrDelete(
                                alternativeTestData,
                                sameRecord: GenericComparer<TestFullTextSearch.AlternativeEntity>(item => new { item.AlternativeKey }),
                                sameValue: GenericEquality<TestFullTextSearch.AlternativeEntity>(item => new { item.Text1, item.Text2 }),
                                filterLoad: new FilterAll(),
                                assign: (dest, src) => {
                                    dest.AlternativeKey = src.AlternativeKey;
                                    dest.Text1 = src.Text1;
                                    dest.Text2 = src.Text2;
                                });

                            context.Repository.TestFullTextSearch.SimpleFTS.Recompute();
                        }

                        // Wait for SQL Server to populate the full-text search index:

                        using (var container = new RhetosTestContainer(true))
                        {
                            var stopwatch = Stopwatch.StartNew();
                            while (true)
                            {
                                int? ftsStatus1 = null;
                                int? ftsStatus2 = null;
                                var getFtsStatus = "SELECT OBJECTPROPERTYEX(OBJECT_ID('TestFullTextSearch.SimpleFTS'), 'TableFulltextPopulateStatus'), "
                                                        + "OBJECTPROPERTYEX(OBJECT_ID('TestFullTextSearch.AlternativeEntity'), 'TableFulltextPopulateStatus')";
                                container.Resolve<ISqlExecuter>().ExecuteReader(getFtsStatus, reader =>
                                {
                                    ftsStatus1 = reader.GetInt32(0);
                                    ftsStatus2 = reader.GetInt32(1);
                                });

                                if (ftsStatus1 != 0 || ftsStatus2 != 0)
                                {
                                    const int timeoutSeconds = 20;
                                    if (stopwatch.Elapsed.TotalSeconds > timeoutSeconds)
                                        Assert.Inconclusive($"Full-text search index is not populated within {timeoutSeconds} seconds.");

                                    Console.WriteLine($"Waiting for full-text search index to refresh.");
                                    System.Threading.Thread.Sleep(200);
                                }
                                else
                                {
                                    Console.WriteLine($"Full-text search index populated in {stopwatch.Elapsed}.");
                                    break;
                                }
                            }
                        }

                        DataPrepared = true;
                    }
                }
        }

        private static IComparer<T> GenericComparer<T>(Func<T, object> keySelector)
        {
            // Not perfect, but good enough for this test set.
            return Comparer<T>.Create((a, b) => keySelector(a).ToString().CompareTo(keySelector(b).ToString()));
        }

        private static Func<T, T, bool> GenericEquality<T>(Func<T, object> keySelector)
        {
            // Not perfect, but good enough for this test set.
            return (a, b) => keySelector(a).ToString().Equals(keySelector(b).ToString());
        }

        private static object DataPreparedLock = new object();
        private static volatile bool DataPrepared = false;

        [TestMethod]
        public void SearchOnIndexTable()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var tests = new Dictionary<string, string>
                {
                    { "\"ab*\"", "ab, abc, cd ab" },
                    { "\"12*\"", "123, ab, xy" },
                    { "a'b", "" },
                    { "a'#'b", "" },
                };

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in tests)
                {
                    Console.WriteLine("Searching '" + test.Key + "'");

                    var filtered = repository.TestFullTextSearch.SimpleFTS.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.SimpleFTS", "*"))
                        .Select(item => item.Base.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");

                    filtered = repository.TestFullTextSearch.SimpleFTS.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.SimpleFTS", "Text"))
                        .Select(item => item.Base.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchOnIndexTable_StringLiteralPattern()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();

                var filtered = repository.TestFullTextSearch.SimpleFTS.Query()
                    .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, "\"ab*\"", "TestFullTextSearch.SimpleFTS", "*"))
                    .Select(item => item.Base.Name).ToList();
                Assert.AreEqual("ab, abc, cd ab", TestUtility.DumpSorted(filtered));
            }
        }

        [TestMethod]
        public void SearchOnBaseEntity()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var tests = new Dictionary<string, string>
                {
                    { "\"ab*\"", "ab, abc, cd ab" },
                    { "\"12*\"", "123, ab, xy" },
                };

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in tests)
                {
                    Console.WriteLine("Searching '" + test.Key + "'");

                    var filtered = repository.TestFullTextSearch.Simple.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.SimpleFTS", "*"))
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchOnBrowse()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var tests = new Dictionary<string, string>
                {
                    { "\"ab*\"", "ab, abc, cd ab" },
                    { "\"12*\"", "123, ab, xy" },
                };

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in tests)
                {
                    Console.WriteLine("Searching '" + test.Key + "'");

                    var filtered = repository.TestFullTextSearch.SimpleBrowse.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.SimpleFTS", "*"))
                        .Select(item => item.Name).ToList();

                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchOnComplexQuery()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var tests = new Dictionary<string, string>
                {
                    { "\"ab*\"", "12-ab, 3-abc, 4-cd ab" },
                    { "\"12*\"", "-123-xy, 12-ab, 56-123" },
                };

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in tests)
                {
                    Console.WriteLine("Searching '" + test.Key + "'");

                    var filter = repository.TestFullTextSearch.SimpleBrowse.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.SimpleFTS", "*"))
                        // Testing combination of filters on different tables:
                        .Where(item => item.Base.Extension_SimpleInfo.Description.Length > 0)
                        .Select(item => item.Base.Extension_SimpleInfo.Description);

                    Console.WriteLine(filter.ToString());
                    var filtered = filter.ToList();

                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchOnBaseEntityWithSortAndPaging()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();

                {
                    string pattern = "\"ab*\"";
                    string result = "cd ab, abc, ab";

                    Console.WriteLine("Searching '" + pattern + "'");
                    var filtered = repository.TestFullTextSearch.Simple.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, pattern, "TestFullTextSearch.SimpleFTS", "*"))
                        .OrderByDescending(item => item.Name)
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(result, TestUtility.Dump(filtered), "Searching '" + pattern + "'.");
                }

                {
                    string pattern = "\"ab*\"";
                    string result = "abc";

                    Console.WriteLine("Searching '" + pattern + "'");
                    var filtered = repository.TestFullTextSearch.Simple.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, pattern, "TestFullTextSearch.SimpleFTS", "*"))
                        .OrderByDescending(item => item.Name)
                        .Skip(1)
                        .Take(1)
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(result, TestUtility.Dump(filtered), "Searching '" + pattern + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchWithRankTop()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var tests = new Dictionary<string, string>
                {
                    { "\"ab*\"", "ab, abc, cd ab" },
                    { "\"12*\"", "123, ab, xy" },
                };

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in tests)
                {
                    Console.WriteLine("Searching '" + test.Key + "'");

                    // rankTop > count:

                    int rankTop = 10;
                    var filteredQuery = repository.TestFullTextSearch.SimpleBrowse.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key,
                            "TestFullTextSearch.SimpleFTS", "*", rankTop));
                    Console.WriteLine(filteredQuery.ToString());
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filteredQuery, item => item.Name), $"Searching top {rankTop} '{test.Key}'.");

                    // rankTop < count:

                    rankTop = 2;
                    Assert.AreEqual(rankTop, filteredQuery.ToList().Count(), $"Searching top {rankTop} '{test.Key}'.");

                    // rankTop as a literal:

                    filteredQuery = repository.TestFullTextSearch.SimpleBrowse.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key,
                            "TestFullTextSearch.SimpleFTS", "*", 2));
                    Assert.AreEqual(rankTop, filteredQuery.ToList().Count(), $"Searching '{test.Key}' with rankTop int literal.");
                }
            }
        }

        [TestMethod]
        public void NullArgument()
        {
            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();
                var ex = TestUtility.ShouldFail(
                    () => repository.TestFullTextSearch.SimpleFTS.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, null, "TestFullTextSearch.SimpleFTS", "*"))
                        .Select(item => item.Base.Name).ToList());
                TestUtility.AssertContains(ex.ToString(), "Search pattern must not be NULL.");
            }
        }

        [TestMethod]
        public void TableParameter()
        {
            // SQL Server does not support the table parameter to be a variable.

            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();
                string table = "TestFullTextSearch.SimpleFTS";
                var ex = TestUtility.ShouldFail(
                    () => repository.TestFullTextSearch.SimpleFTS.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, "a", table, "*"))
                        .Select(item => item.Base.Name).ToList());
                TestUtility.AssertContains(ex.ToString(), new[] { "Please use a string literal", "tableName" });
            }
        }

        [TestMethod]
        public void ColumnsParameter()
        {
            // SQL Server does not support the columns parameter to be a variable.

            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();
                string columns = "*";
                var ex = TestUtility.ShouldFail(
                    () => repository.TestFullTextSearch.SimpleFTS.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, "a", "TestFullTextSearch.SimpleFTS", columns))
                        .Select(item => item.Base.Name).ToList());
                TestUtility.AssertContains(ex.ToString(), new[] { "Please use a string literal", "searchColumns" });
            }
        }

        [TestMethod]
        public void RankTopParameter()
        {
            // SQL Server does not support the rankTop parameter to be an expression.

            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();
                int topValue = 10;
                var ex = TestUtility.ShouldFail(
                    () => repository.TestFullTextSearch.SimpleFTS.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, "a", "TestFullTextSearch.SimpleFTS", "*", topValue + 1))
                        .Select(item => item.Base.Name).ToList());
                TestUtility.AssertContains(ex.ToString(), new[] { "Please use a simple integer variable", "rankTop" });
            }
        }

        [TestMethod]
        public void SearchAlternativeIntegerKey()
        {
            PrepareData();

            using (var container = new RhetosTestContainer(false))
            {
                var tests = new Dictionary<string, string>
                {
                    { "\"ab*\"", "ab, abc, cd ab" },
                    { "\"12*\"", "123, ab, xy" },
                };

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in tests)
                {
                    Console.WriteLine("Searching '" + test.Key + "'");

                    {
                        var filtered = repository.TestFullTextSearch.AlternativeEntity.Query()
                            .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.AlternativeKey.Value, test.Key, "TestFullTextSearch.AlternativeEntity", "*"));
                        Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered, item => item.Text1), "Searching '" + test.Key + "'.");
                    }
                    {
                        var filtered = repository.TestFullTextSearch.AlternativeEntity.Query()
                            .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.AlternativeKey.Value, test.Key, "TestFullTextSearch.AlternativeEntity", "(Text1, Text2)"));
                        Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered, item => item.Text1), "Searching Text1, Text2 '" + test.Key + "'.");
                    }
                    {
                        var filtered = repository.TestFullTextSearch.AlternativeEntity.Query()
                            .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.AlternativeKey.Value, test.Key, "TestFullTextSearch.AlternativeEntity", "*", 10));
                        Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered, item => item.Text1), "Searching top 10 '" + test.Key + "'.");
                    }
                    {
                        var filtered = repository.TestFullTextSearch.AlternativeEntity.Query()
                            .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.AlternativeKey.Value, test.Key, "TestFullTextSearch.AlternativeEntity", "(Text1, Text2)", 10));
                        Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered, item => item.Text1), "Searching top 10 Text1, Text2 '" + test.Key + "'.");
                    }
                    {
                        var filtered = repository.TestFullTextSearch.AlternativeEntity.Query()
                            .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.AlternativeKey.Value, test.Key, "TestFullTextSearch.AlternativeEntity", "*", 2));
                        Assert.AreEqual(2, filtered.Count(), "Searching top 2 (literal) * '" + test.Key + "'.");
                    }
                    {
                        int top = 2;
                        var filtered = repository.TestFullTextSearch.AlternativeEntity.Query()
                            .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.AlternativeKey.Value, test.Key, "TestFullTextSearch.AlternativeEntity", "*", top));
                        Assert.AreEqual(2, filtered.Count(), "Searching top 2 (var) *'" + test.Key + "'.");
                    }
                    {
                        var filtered = repository.TestFullTextSearch.AlternativeEntity.Query()
                            .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.AlternativeKey.Value, test.Key, "TestFullTextSearch.AlternativeEntity", "(Text1, Text2)", 2));
                        Assert.AreEqual(2, filtered.Count(), "Searching top 2 (literal) Text1, Text2 '" + test.Key + "'.");
                    }
                }
            }
        }
    }
}
