/*
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
using Rhetos.Processing.DefaultCommands;

namespace CommonConcepts.Test
{
    [TestClass]
    public class FilterTest
    {
        private static string ReportSource<T>(Common.DomRepository repository, T filter)
        {
            var filterRepository = (IFilterRepository<T, Test10.Source>) repository.Test10.Source;
            var loaded = filterRepository.Filter(filter).Select(item => item.i + item.s);
            var report = string.Join(", ", loaded.OrderBy(s => s));
            Console.WriteLine("Report: " + report);
            return report;
        }

        [TestMethod]
        public void FilterAll()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                Assert.AreEqual("1a, 2b", ReportSource<FilterAll>(repository, null));
            }
        }

        [TestMethod]
        public void FilterByIdentities()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var source = repository.Test10.Source.All().OrderBy(item => item.i).ToArray();
                Assert.AreEqual("2b", ReportSource(repository, new [] {source[1].ID}));
            }
        }

        [TestMethod]
        public void FilterEntityByIdentifiers1()
        {
            FilterEntityByIdentifiers(1);
        }

        [TestMethod]
        public void FilterEntityByIdentifiers100()
        {
            FilterEntityByIdentifiers(100);
        }

        [TestMethod]
        public void FilterEntityByIdentifiers10000()
        {
            FilterEntityByIdentifiers(10000);
        }

        private static void FilterEntityByIdentifiers(int n)
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var guids = Enumerable.Range(0, n).Select(x => Guid.NewGuid()).ToList();

                List<string> commands = new List<string>();
                commands.Add("DELETE FROM Test10.Simple;");
                for (int j = 0; j < (n+999) / 1000; j++)
                {
                var sql = new StringBuilder();
                    for (int i = 0; i < 1000 && j*1000+i < n; i++)
                        sql.AppendFormat("INSERT INTO Test10.Simple (ID, i) SELECT '{0}', {1};\r\n", guids[j*1000 + i], j*1000 + i);
                    commands.Add(sql.ToString());
                }
                executionContext.SqlExecuter.ExecuteSql(commands);
                var repository = new Common.DomRepository(executionContext);

                var loaded = repository.Test10.Simple.Filter(new[] { guids[0] });
                Assert.AreEqual("0", TestUtility.DumpSorted(loaded, item => item.i.ToString()));

                try
                {
                    var all = repository.Test10.Simple.Filter(guids);
                    Assert.AreEqual(n, all.Count());
                }
                catch (Exception ex)
                {
                    var limitedLengthReport = new StringBuilder();
                    do
                    {
                        string message = ex.GetType().Name + ": " + ex.Message;
                        if (message.Length > 1000)
                            message = message.Substring(0, 1000);
                        limitedLengthReport.AppendLine(message);

                        ex = ex.InnerException;
                    } while (ex != null);

                    throw new Exception(limitedLengthReport.ToString());
                }
            }
        }

        //=======================================================================

        [TestMethod]
        public void FilterBy()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                executionContext.SqlExecuter.ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = new Common.DomRepository(executionContext);

                IFilterRepository<TestFilter.FilterByPrefix, TestFilter.Source> filterRepository = repository.TestFilter.Source;
                var loaded = filterRepository.Filter(new TestFilter.FilterByPrefix { Prefix = "b" });
                Assert.AreEqual("b1, b2", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        //=======================================================================

        [TestMethod]
        public void ComposableFilterBy_CompositionOfTwoFilters()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                executionContext.SqlExecuter.ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = new Common.DomRepository(executionContext);

                var q = repository.TestFilter.Source.Query();
                q = repository.TestFilter.Source.Filter(q, new TestFilter.ComposableFilterByPrefix { Prefix = "b" });
                q = repository.TestFilter.Source.Filter(q, new TestFilter.ComposableFilterByContains { Pattern = "2" });
                var loaded = q.ToArray();

                Assert.AreEqual("b2", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        [TestMethod]
        public void ComposableFilterBy_StandardFilterInterface()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                executionContext.SqlExecuter.ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = new Common.DomRepository(executionContext);

                IFilterRepository<TestFilter.ComposableFilterByPrefix, TestFilter.Source> filterRepository = repository.TestFilter.Source;
                var loaded = filterRepository.Filter(new TestFilter.ComposableFilterByPrefix { Prefix = "b" });
                Assert.AreEqual("b1, b2", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        //=======================================================================

        [TestMethod]
        public void ItemFilter()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                executionContext.SqlExecuter.ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = new Common.DomRepository(executionContext);

                var q = repository.TestFilter.Source.Query();
                q = repository.TestFilter.Source.Filter(q, new TestFilter.ItemStartsWithB());
                q = repository.TestFilter.Source.Filter(q, new TestFilter.ItemContains2());
                var loaded = q.ToArray();

                Assert.AreEqual("b2", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        [TestMethod]
        public void ItemFilter_ExplicitModuleName()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                executionContext.SqlExecuter.ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = new Common.DomRepository(executionContext);

                var q = repository.TestFilter.Source.Query();
                q = repository.TestFilter.Source.Filter(q, new TestFilter2.ItemStartsWithC());
                var loaded = q.ToArray();

                Assert.AreEqual("c1", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        //=======================================================================

        [TestMethod]
        public void FilterByBase()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.Source" });
                var repository = new Common.DomRepository(executionContext);

                var s1 = new TestFilter.Source { ID = Guid.NewGuid(), Name = "A s1" };
                var s2 = new TestFilter.Source { ID = Guid.NewGuid(), Name = "B s2" };
                var e1 = new TestFilter.SourceExtension { ID = s1.ID, Name2 = "C e1" };
                var e2 = new TestFilter.SourceExtension { ID = s2.ID, Name2 = "D e2" };
                repository.TestFilter.Source.Insert(new[] { s1, s2 });
                repository.TestFilter.SourceExtension.Insert(new[] { e1, e2 });

                var filteredExtensionByBase = repository.TestFilter.SourceExtension.Filter(new TestFilter.FilterByPrefix { Prefix = "A" });
                Assert.AreEqual("C e1", TestUtility.DumpSorted(filteredExtensionByBase, item => item.Name2));
            }
        }
        [TestMethod]
        public void FilterByReferencedAndLinkedItems()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.Source" });
                var repository = new Common.DomRepository(executionContext);

                var s1 = new TestFilter.Source { Name = "A s1" };
                var s2 = new TestFilter.Source { Name = "B s2" };
                var d1 = new TestFilter.SourceDetail { Parent = s1, Name2 = "C d1" };
                var d2 = new TestFilter.SourceDetail { Parent = s2, Name2 = "D d2" };
                repository.TestFilter.Source.Insert(new[] { s1, s2 });
                repository.TestFilter.SourceDetail.Insert(new[] { d1, d2 });

                var filteredDetailByMaster = repository.TestFilter.SourceDetail.Filter(new TestFilter.FilterByPrefix { Prefix = "A" });
                Assert.AreEqual("C d1", TestUtility.DumpSorted(filteredDetailByMaster, item => item.Name2));

                var filteredMasterByDetail = repository.TestFilter.Source.Filter(new TestFilter.FilterDetail { Prefix = "C" });
                Assert.AreEqual("A s1", TestUtility.DumpSorted(filteredMasterByDetail, item => item.Name));
            }
        }

        [TestMethod]
        public void SimpleComposableFilterCaseInsensitive()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.CombinedFilters" });
                var repository = new Common.DomRepository(executionContext);

                var s1 = new TestFilter.CombinedFilters { Name = "Abeceda" };
                var s2 = new TestFilter.CombinedFilters { Name = "abeceda" };
                repository.TestFilter.CombinedFilters.Insert(new[] { s1, s2 });

                var filteredByContainsJustComposable = (repository.TestFilter.CombinedFilters as IQueryDataSourceCommandImplementation).QueryData(new QueryDataSourceCommandInfo
                {
                    DataSource = "TestFilter.CombinedFilters",
                    Filter = new TestFilter.ComposableFilterByContains { Pattern = "Abec" }
                });

                var filteredByContainsWithGenericFilter = (repository.TestFilter.CombinedFilters as IQueryDataSourceCommandImplementation).QueryData(new QueryDataSourceCommandInfo
                {
                    DataSource = "TestFilter.CombinedFilters",
                    Filter = new TestFilter.ComposableFilterByContains { Pattern = "Abec" },
                    GenericFilter = new FilterCriteria[] {new FilterCriteria {Property = "Name", Operation = "Contains", Value="Abec"}}
                });
                // filter doubled should return same results as just one Composable filter
                Assert.AreEqual(filteredByContainsJustComposable.Records.Length, filteredByContainsWithGenericFilter.Records.Length);
            }
        }

        [TestMethod]
        public void SimpleComposableFilterGenericFilterReferenced()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestFilter.CombinedFilters", "DELETE FROM TestFilter.Simple" });
                var repository = new Common.DomRepository(executionContext);
                var refEnt = new TestFilter.Simple {Name = "test"};
                repository.TestFilter.Simple.Insert(new[] { refEnt });
                var s1 = new TestFilter.CombinedFilters { Name = "Abeceda", Simple = refEnt };
                var s2 = new TestFilter.CombinedFilters { Name = "abeceda" };
                repository.TestFilter.CombinedFilters.Insert(new[] { s1, s2 });

                // Containing "ece" and referenced object name contains "es"
                var filteredByContainsWithGenericFilter = (repository.TestFilter.CombinedFilters as IQueryDataSourceCommandImplementation).QueryData(new QueryDataSourceCommandInfo
                {
                    DataSource = "TestFilter.CombinedFilters",
                    Filter = new TestFilter.ComposableFilterByContains { Pattern = "ece" },
                    GenericFilter = new FilterCriteria[] { new FilterCriteria { Property = "Simple.Name", Operation = "Contains", Value = "es" } }
                });
                Assert.AreEqual(1, filteredByContainsWithGenericFilter.Records.Length);
            }
        }
    }
}
