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
                repository.TestFullTextSearch.Simple.Save(testData, null, repository.TestFullTextSearch.Simple.All());
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
                        System.Threading.Thread.Sleep(500);
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
        [Ignore] // TODO: Implement ORM mapping for to FullTextSearch function.
        public void SearchOnIndexTable()
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

                    string columns = "*";
                    var filtered = repository.TestFullTextSearch.Simple_Search.Query()
                        .Where(item => item.FullTextSearch(test.Key, "TestFullTextSearch.Simple_Search", columns))
                        .Select(item => item.Base.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");

                    columns = "Text";
                    filtered = repository.TestFullTextSearch.Simple_Search.Query()
                        .Where(item => item.FullTextSearch(test.Key, "TestFullTextSearch.Simple_Search", columns))
                        .Select(item => item.Base.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        [Ignore] // TODO: Implement ORM mapping for to FullTextSearch function.
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
                        .Where(item => item.FullTextSearch(test.Key, "TestFullTextSearch.Simple_Search", "*"))
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        [Ignore] // Not yet supported by NHibernate.
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
                        .Where(item => item.FullTextSearch(test.Key, "TestFullTextSearch.Simple_Search", "*"))
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered), "Searching '" + test.Key + "'.");
                }
            }
        }

        [TestMethod]
        [Ignore] // TODO: Implement ORM mapping for to FullTextSearch function.
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
                        .Where(item => item.FullTextSearch(pattern, "TestFullTextSearch.Simple_Search", "*"))
                        .OrderByDescending(item => item.Name)
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(result, TestUtility.Dump(filtered), "Searching '" + pattern + "'.");
                }

                {
                    string pattern = "\"ab*\"";
                    string result = "abc";

                    Console.WriteLine("Searching '" + pattern + "'");
                    var filtered = repository.TestFullTextSearch.Simple.Query()
                        .Where(item => item.FullTextSearch(pattern, "TestFullTextSearch.Simple_Search", "*"))
                        .OrderByDescending(item => item.Name)
                        .Skip(1)
                        .Take(1)
                        .Select(item => item.Name).ToList();
                    Assert.AreEqual(result, TestUtility.Dump(filtered), "Searching '" + pattern + "'.");
                }
            }
        }
    }
}
