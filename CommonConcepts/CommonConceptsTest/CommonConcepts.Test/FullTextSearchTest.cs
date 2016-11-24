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
        private static Lazy<bool> dataPrepared = new Lazy<bool>(() => { PrepareData(); return true; }, true);

        private static void PrepareData()
        {
            using (var container = new RhetosTestContainer(true))
            {
                var testData = new[]
                {
                    new TestFullTextSearch.Simple { Name = "ab", Code = 12 },
                    new TestFullTextSearch.Simple { Name = "abc", Code = 3 },
                    new TestFullTextSearch.Simple { Name = "cd ab", Code = 4 },
                    new TestFullTextSearch.Simple { Name = "123", Code = 56 },
                    new TestFullTextSearch.Simple { Name = "xy", Code = -123 },
                };

                var repository = container.Resolve<Common.DomRepository>();
                repository.TestFullTextSearch.Simple.Save(testData, null, repository.TestFullTextSearch.Simple.Load());
            }

            using (var container = new RhetosTestContainer(true))
            {
                var stopwatch = Stopwatch.StartNew();
                while (true)
                {
                    int? ftsStatus = null;
                    var getFtsStatus = "SELECT OBJECTPROPERTYEX(OBJECT_ID('TestFullTextSearch.Simple_Search'), 'TableFulltextPopulateStatus')";
                    container.Resolve<ISqlExecuter>().ExecuteReader(getFtsStatus, reader => ftsStatus = reader.GetInt32(0));

                    if (ftsStatus != 0)
                    {
                        const int timeout = 20;
                        if (stopwatch.Elapsed.TotalSeconds > timeout)
                            Assert.Inconclusive("Full-text search index is not populated within " + timeout + " seconds.");

                        Console.WriteLine("Waiting for full-text search index to refresh.");
                        System.Threading.Thread.Sleep(200);
                    }
                    else
                    {
                        Console.WriteLine("Full-text search index populated " + stopwatch.Elapsed);
                        break;
                    }
                }
            }
        }

        [TestMethod]
        public void SearchOnIndexTable()
        {
            Assert.IsTrue(dataPrepared.Value);

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

                    var filtered = repository.TestFullTextSearch.Simple_Search.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.Simple_Search", "*"))
                        .Select(item => item.Base.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");

                    filtered = repository.TestFullTextSearch.Simple_Search.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.Simple_Search", "Text"))
                        .Select(item => item.Base.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchOnIndexTable_StringLiteralPattern()
        {
            Assert.IsTrue(dataPrepared.Value);

            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();

                var filtered = repository.TestFullTextSearch.Simple_Search.Query()
                    .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, "\"ab*\"", "TestFullTextSearch.Simple_Search", "*"))
                    .Select(item => item.Base.Name).ToList();
                Assert.AreEqual("ab, abc, cd ab", TestUtility.DumpSorted(filtered));
            }
        }

        [TestMethod]
        public void SearchOnBaseEntity()
        {
            Assert.IsTrue(dataPrepared.Value);

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
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.Simple_Search", "*"))
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchOnBrowse()
        {
            Assert.IsTrue(dataPrepared.Value);

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
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.Simple_Search", "*"))
                        .Select(item => item.Name).ToList();

                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        public void SearchOnComplexQuery()
        {
            Assert.IsTrue(dataPrepared.Value);

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
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, test.Key, "TestFullTextSearch.Simple_Search", "*"))
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
            Assert.IsTrue(dataPrepared.Value);

            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();

                {
                    string pattern = "\"ab*\"";
                    string result = "cd ab, abc, ab";

                    Console.WriteLine("Searching '" + pattern + "'");
                    var filtered = repository.TestFullTextSearch.Simple.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, pattern, "TestFullTextSearch.Simple_Search", "*"))
                        .OrderByDescending(item => item.Name)
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(result, TestUtility.Dump(filtered), "Searching '" + pattern + "'.");
                }

                {
                    string pattern = "\"ab*\"";
                    string result = "abc";

                    Console.WriteLine("Searching '" + pattern + "'");
                    var filtered = repository.TestFullTextSearch.Simple.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, pattern, "TestFullTextSearch.Simple_Search", "*"))
                        .OrderByDescending(item => item.Name)
                        .Skip(1)
                        .Take(1)
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(result, TestUtility.Dump(filtered), "Searching '" + pattern + "'.");
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
                    () => repository.TestFullTextSearch.Simple_Search.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, null, "TestFullTextSearch.Simple_Search", "*"))
                        .Select(item => item.Base.Name).ToList());
                TestUtility.AssertContains(ex.ToString(), "Search pattern must not be NULL.");
            }
        }

        [TestMethod]
        public void TableParameter()
        {
            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();
                string table = "TestFullTextSearch.Simple_Search";
                var ex = TestUtility.ShouldFail(
                    () => repository.TestFullTextSearch.Simple_Search.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, "a", table, "*"))
                        .Select(item => item.Base.Name).ToList());
                TestUtility.AssertContains(ex.ToString(), new[] { "Please use a string literal", "tableName" });
            }
        }

        [TestMethod]
        public void ColumnsParameter()
        {
            using (var container = new RhetosTestContainer(false))
            {
                var repository = container.Resolve<Common.DomRepository>();
                string columns = "*";
                var ex = TestUtility.ShouldFail(
                    () => repository.TestFullTextSearch.Simple_Search.Query()
                        .Where(item => DatabaseExtensionFunctions.FullTextSearch(item.ID, "a", "TestFullTextSearch.Simple_Search", columns))
                        .Select(item => item.Base.Name).ToList());
                TestUtility.AssertContains(ex.ToString(), new[] { "Please use a string literal", "searchColumns" });
            }
        }
    }
}
