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
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class FilterTest
    {
        private static ReadCommandResult ExecuteCommand(ReadCommandInfo commandInfo, IUnitOfWorkScope scope)
        {
            var commands = scope.Resolve<IPluginsContainer<ICommandImplementation>>();
            var readCommand = (ReadCommand)commands.GetImplementations(typeof(ReadCommandInfo)).Single();
            return readCommand.Execute(commandInfo);
        }

        [TestMethod]
        public void FilterAll()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                Assert.AreEqual("1a, 2b", TestUtility.DumpSorted(repository.Test10.Source.Query(), item => item.i + item.s));
            }
        }

        [TestMethod]
        public void FilterByIdentities()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var source = repository.Test10.Source.Load().OrderBy(item => item.i).ToArray();
                Assert.AreEqual("2b", TestUtility.DumpSorted(repository.Test10.Source.Load(new [] { source[1].ID }), item => item.i + item.s));
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
            int oldFilterIds;
            Guid commitCheckId = Guid.NewGuid();

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var filterIdRepos = scope.Resolve<GenericRepository<Common.FilterId>>();
                oldFilterIds = filterIdRepos.Query().Count();

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
                scope.Resolve<ISqlExecuter>().ExecuteSql(commands);
                var repository = scope.Resolve<Common.DomRepository>();

                var loaded = repository.Test10.Simple.Load(new[] { guids[0] });
                Assert.AreEqual("0", TestUtility.DumpSorted(loaded, item => item.i.ToString()));

                try
                {
                    var loadedByIds = repository.Test10.Simple.Load(guids);
                    Assert.AreEqual(n, loadedByIds.Length);

                    var queriedByIds = scope.Resolve<GenericRepository<Test10.Simple>>().Query(guids);
                    Assert.AreEqual(n, queriedByIds.Count());
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

                context.EntityFrameworkContext.Database.ExecuteSqlCommand("DELETE FROM Test10.Simple");
                repository.Test10.Simple.Insert(new[] { new Test10.Simple { ID = commitCheckId } });

                scope.CommitAndClose();
            }

            using (var scope = TestScope.Create())
            {
                var testRepos = scope.Resolve<GenericRepository<Test10.Simple>>();
                if (!testRepos.Query(new[] { commitCheckId }).Any())
                    Assert.Fail("Transaction did not commit. Cannot test for remaining temporary data.");

                var filterIdRepos = scope.Resolve<GenericRepository<Common.FilterId>>();
                Assert.AreEqual(0, filterIdRepos.Query().Count() - oldFilterIds, "Temporary data used for filtering should be cleaned.");
            }
        }

        //=======================================================================

        [TestMethod]
        public void FilterBy()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = scope.Resolve<Common.DomRepository>();

                var loaded = repository.TestFilter.Source.Filter(new TestFilter.FilterByPrefix { Prefix = "b" });
                Assert.AreEqual("b1, b2", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        //=======================================================================

        [TestMethod]
        public void ComposableFilterBy_CompositionOfTwoFilters()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = scope.Resolve<Common.DomRepository>();

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
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = scope.Resolve<Common.DomRepository>();

                var loaded = repository.TestFilter.Source.Filter(new TestFilter.ComposableFilterByPrefix { Prefix = "b" });
                Assert.AreEqual("b1, b2", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        //=======================================================================

        [TestMethod]
        public void ItemFilter()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = scope.Resolve<Common.DomRepository>();

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
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.Source;" });
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestFilter.Source (Name) SELECT N'" + name + "';"));

                var repository = scope.Resolve<Common.DomRepository>();

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
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.Source" });
                var repository = scope.Resolve<Common.DomRepository>();

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
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.Source" });
                var repository = scope.Resolve<Common.DomRepository>();

                var s1 = new TestFilter.Source { Name = "A s1" };
                var s2 = new TestFilter.Source { Name = "B s2" };
                repository.TestFilter.Source.Insert(new[] { s1, s2 });
                var d1 = new TestFilter.SourceDetail { ParentID = s1.ID, Name2 = "C d1" };
                var d2 = new TestFilter.SourceDetail { ParentID = s2.ID, Name2 = "D d2" };
                repository.TestFilter.SourceDetail.Insert(new[] { d1, d2 });

                var filteredDetailByMaster = repository.TestFilter.SourceDetail.Filter(new TestFilter.FilterByPrefix { Prefix = "A" });
                Assert.AreEqual("C d1", TestUtility.DumpSorted(filteredDetailByMaster, item => item.Name2));

                var filteredMasterByDetail = repository.TestFilter.Source.Filter(new TestFilter.FilterDetail { Prefix = "C" });
                Assert.AreEqual("A s1", TestUtility.DumpSorted(filteredMasterByDetail, item => item.Name));
            }
        }

        [TestMethod]
        public void NullGenericFilterTest()
        {
            using (var scope = TestScope.Create())
            {
                var genericRepos = scope.Resolve<GenericRepositories>().GetGenericRepository("Common.Claim");

                var readCommand = new ReadCommandInfo
                {
                    DataSource = "Common.Claim",
                    Top = 3,
                    OrderByProperties = new[] { new OrderByProperty { Property = "ClaimResource" } },
                    ReadRecords = true,
                    ReadTotalCount = true
                };

                var serverCommandsUtility = scope.Resolve<ServerCommandsUtility>();

                var readResult = serverCommandsUtility.ExecuteReadCommand(readCommand, genericRepos);
                Console.WriteLine("Records.Length: " + readResult.Records.Length);
                Console.WriteLine("TotalCount: " + readResult.TotalCount);
                Assert.IsTrue(readResult.Records.Length < readResult.TotalCount);
            }
        }

        [TestMethod]
        public void SimpleComposableFilterCaseInsensitive()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.CombinedFilters" });
                var repository = scope.Resolve<Common.DomRepository>();

                var s1 = new TestFilter.CombinedFilters { Name = "Abeceda" };
                var s2 = new TestFilter.CombinedFilters { Name = "abeceda" };
                repository.TestFilter.CombinedFilters.Insert(new[] { s1, s2 });

                var filteredByContainsJustComposable = ExecuteCommand(new ReadCommandInfo
                {
                    DataSource = "TestFilter.CombinedFilters",
                    Filters = new[] { new FilterCriteria(new TestFilter.ComposableFilterByContains { Pattern = "Abec" }) },
                    ReadRecords = true,
                    ReadTotalCount = true
                }, scope);

                var filteredByContainsWithGenericFilter = ExecuteCommand(new ReadCommandInfo
                {
                    DataSource = "TestFilter.CombinedFilters",
                    Filters = new[]
                    {
                        new FilterCriteria(new TestFilter.ComposableFilterByContains { Pattern = "Abec" }),
                        new FilterCriteria("Name", "Contains", "Abec")
                    },
                    ReadRecords = true,
                    ReadTotalCount = true
                }, scope);
                // filter doubled should return same results as just one Composable filter
                Assert.AreEqual(filteredByContainsJustComposable.Records.Length, filteredByContainsWithGenericFilter.Records.Length);
                Assert.AreEqual(2, filteredByContainsWithGenericFilter.Records.Length);
            }
        }

        [TestMethod]
        public void SimpleComposableFilterGenericFilterReferenced()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.CombinedFilters", "DELETE FROM TestFilter.Simple" });
                var repository = scope.Resolve<Common.DomRepository>();
                var refEnt = new TestFilter.Simple { Name = "test" };
                repository.TestFilter.Simple.Insert(new[] { refEnt });
                var s1 = new TestFilter.CombinedFilters { Name = "Abeceda", SimpleID = refEnt.ID };
                var s2 = new TestFilter.CombinedFilters { Name = "abeceda" };
                repository.TestFilter.CombinedFilters.Insert(new[] { s1, s2 });

                // Containing "ece" and referenced object name contains "es"
                var filteredByContainsWithGenericFilter = ExecuteCommand(new ReadCommandInfo
                {
                    DataSource = "TestFilter.CombinedFilters",
                    Filters = new[]
                    {
                        new FilterCriteria(new TestFilter.ComposableFilterByContains { Pattern = "ece" }),
                        new FilterCriteria("Simple.Name", "Contains", "es")
                    },
                    ReadRecords = true,
                    ReadTotalCount = true
                }, scope);
                Assert.AreEqual(1, filteredByContainsWithGenericFilter.Records.Length);
            }
        }

        private static string ReportFilteredBrowse(IUnitOfWorkScope scope, ReadCommandInfo readCommandInfo)
        {
            readCommandInfo.DataSource = "TestFilter.ComposableFilterBrowse";

            return TestUtility.DumpSorted(
                ExecuteCommand(readCommandInfo, scope).Records,
                item =>
                {
                    var x = (TestFilter.ComposableFilterBrowse)item;
                    return x.Name + " " + (x.DebugInfoSimpleName ?? "null");
                });
        }

        ReadCommandInfo ReadCommandWithFilters(params object[] filters)
        {
            return new ReadCommandInfo
            {
                Filters = filters.Select(f => f is FilterCriteria ? f : new FilterCriteria(f))
                    .Cast<FilterCriteria>().ToArray(),
                ReadRecords = true,
                ReadTotalCount = true
            };
        }

        [TestMethod]
        public void ComposableFilterBrowse()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.CombinedFilters", "DELETE FROM TestFilter.Simple" });
                var repository = scope.Resolve<Common.DomRepository>();

                var parentA = new TestFilter.Simple { Name = "PA" };
                var parentB = new TestFilter.Simple { Name = "PB" };
                repository.TestFilter.Simple.Insert(new[] { parentA, parentB });

                var childA = new TestFilter.CombinedFilters { Name = "CA", SimpleID = parentA.ID };
                var childB = new TestFilter.CombinedFilters { Name = "CB", SimpleID = parentB.ID };
                var childNull = new TestFilter.CombinedFilters { Name = "CN", SimpleID = null };
                repository.TestFilter.CombinedFilters.Insert(new[] { childA, childB, childNull });

                Assert.AreEqual("CA PA", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new TestFilter.SimpleNameA())));

                Assert.AreEqual("CA PA", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new FilterCriteria { Property = "Simple.Name", Operation = "Contains", Value = "a" })));

                Assert.AreEqual("CA PA", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new TestFilter.SimpleNameA(),
                    new FilterCriteria { Property = "Simple.Name", Operation = "Contains", Value = "a" })));

                Assert.AreEqual("CN null", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new FilterCriteria { Property = "Name", Operation = "Contains", Value = "n" })));

                Assert.AreEqual("CN null", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new TestFilter.NameN())));

                Assert.AreEqual("", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new TestFilter.NameN(),
                    new FilterCriteria { Property = "Simple.Name", Operation = "Contains", Value = "p" })));

                Assert.AreEqual("", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new TestFilter.NameN(),
                    new FilterCriteria { Property = "Simple.Name", Operation = "Contains", Value = "p" })));

                Assert.AreEqual("CA PA", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new TestFilter.ComposableFilterBrowseLoader { Pattern = "a" })));
            }
        }

        [TestMethod]
        public void ArrayFilterBrowse()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestFilter.CombinedFilters", "DELETE FROM TestFilter.Simple" });
                var repository = scope.Resolve<Common.DomRepository>();

                var parentA = new TestFilter.Simple { Name = "PA" };
                var parentB = new TestFilter.Simple { Name = "PB" };
                repository.TestFilter.Simple.Insert(new[] { parentA, parentB });

                var childA = new TestFilter.CombinedFilters { Name = "CA", SimpleID = parentA.ID };
                var childB = new TestFilter.CombinedFilters { Name = "CB", SimpleID = parentB.ID };
                var childNull = new TestFilter.CombinedFilters { Name = "CN", SimpleID = null };
                repository.TestFilter.CombinedFilters.Insert(new[] { childA, childB, childNull });

                Assert.AreEqual("CA PA", ReportFilteredBrowse(scope, ReadCommandWithFilters(
                    new TestFilter.ComposableFilterBrowseLoader { Pattern = "a" },
                    new FilterCriteria { Property = "Simple.Name", Operation = "Contains", Value = "p" })));

                // This test just documents the current behavior, this is not an intended feature.
                // NullReferenceException because "Simple.Name" FilterCriteria is executed in C# instead of the database.
                TestUtility.ShouldFail<NullReferenceException>(() =>
                    ReportFilteredBrowse(scope, ReadCommandWithFilters(
                        new TestFilter.ComposableFilterBrowseLoader { Pattern = "c" },
                        new FilterCriteria { Property = "Simple.Name", Operation = "Contains", Value = "p" })));
            }
        }

        [TestMethod]
        public void ComposableFilterWithExecutionContext()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var currentUserName = scope.Resolve<IUserInfo>().UserName;
                Assert.IsTrue(!string.IsNullOrWhiteSpace(currentUserName));

                Assert.AreEqual(currentUserName,
                    repository.TestFilter.FixedData.Filter(new TestFilter.ComposableFilterWithContext()).Single().Name);
            }
        }

        [TestMethod]
        public void ExternalFilterType()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                repository.TestFilter.ExternalFilter.Delete(repository.TestFilter.ExternalFilter.Query());
                repository.TestFilter.ExternalFilter.Insert(
                    new[] { "str", "snull", "date", "dnull", "ddef" }
                    .Select(name => new TestFilter.ExternalFilter { Name = name }));

                var tests = new List<Tuple<Type, object, string>>
                {
                    Tuple.Create<Type, object, string>(null, "abc", "str"),
                    Tuple.Create<Type, object, string>(typeof(string), null, "snull"),
                    Tuple.Create<Type, object, string>(null, DateTime.Now, "date"),
                    Tuple.Create<Type, object, string>(typeof(DateTime), null, "ddef"), // A value type instance cannot be null.
                    Tuple.Create<Type, object, string>(typeof(DateTime), default(DateTime), "ddef"),
                };

                var gr = scope.Resolve<GenericRepository<TestFilter.ExternalFilter>>();

                foreach (var test in tests)
                {
                    Type filterType = test.Item1 ?? test.Item2.GetType();

                    string testReport = filterType.FullName + ": " + test.Item2;
                    Console.WriteLine(testReport);

                    {
                        var reposReadResult = gr.Load(test.Item2, filterType);
                        Assert.AreEqual(
                            test.Item3,
                            TestUtility.DumpSorted(reposReadResult, item => item.Name),
                            "ReadRepos: " + testReport);
                    }

                    {
                        var readCommand = new ReadCommandInfo
                        {
                            DataSource = "TestFilter.ExternalFilter",
                            Filters = new FilterCriteria[] { new FilterCriteria {
                                Filter = filterType.FullName,
                                Value = test.Item2 } },
                            ReadRecords = true
                        };
                        
                        var commandResult = (TestFilter.ExternalFilter[])ExecuteCommand(readCommand, scope).Records;
                        Assert.AreEqual(
                            test.Item3,
                            TestUtility.DumpSorted(commandResult, item => item.Name),
                            "ReadCommand: " + testReport);
                    }
                }
            }
        }

        [TestMethod]
        public void AutoFilter_NoEffectOnServerObjectModel()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestFilter.AutoFilter1.Delete(repository.TestFilter.AutoFilter1.Query());
                repository.TestFilter.AutoFilter2.Delete(repository.TestFilter.AutoFilter2.Query());

                repository.TestFilter.AutoFilter1.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter1 { Name = name }));

                repository.TestFilter.AutoFilter2.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter2 { Name = name }));

                Assert.AreEqual("a1, a2, b1, b2", TestUtility.DumpSorted(repository.TestFilter.AutoFilter1.Query(), item => item.Name));
                Assert.AreEqual("a1, a2, b1, b2", TestUtility.DumpSorted(repository.TestFilter.AutoFilter2.Query(), item => item.Name));
                Assert.AreEqual("a1, a2, b1, b2", TestUtility.DumpSorted(repository.TestFilter.AutoFilter2Browse.Query(), item => item.Name2));

                var gr = scope.Resolve<GenericRepositories>();

                Assert.AreEqual("a1, a2, b1, b2", TestUtility.DumpSorted(gr.GetGenericRepository<TestFilter.AutoFilter1>().Load(), item => item.Name));
                Assert.AreEqual("a1, a2, b1, b2", TestUtility.DumpSorted(gr.GetGenericRepository<TestFilter.AutoFilter2>().Load(), item => item.Name));
                Assert.AreEqual("a1, a2, b1, b2", TestUtility.DumpSorted(gr.GetGenericRepository<TestFilter.AutoFilter2Browse>().Load(), item => item.Name2));
            }
        }

        [TestMethod]
        public void AutoFilter_Simple()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestFilter.AutoFilter1.Delete(repository.TestFilter.AutoFilter1.Query());
                repository.TestFilter.AutoFilter2.Delete(repository.TestFilter.AutoFilter2.Query());

                repository.TestFilter.AutoFilter1.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter1 { Name = name }));

                repository.TestFilter.AutoFilter2.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter2 { Name = name }));

                TestClientRead<TestFilter.AutoFilter1>(scope, "a1, a2", item => item.Name);
                TestClientRead<TestFilter.AutoFilter2>(scope, "a1, a2, b1, b2", item => item.Name);
                TestClientRead<TestFilter.AutoFilter2Browse>(scope, "b1x, b2x", item => item.Name2);
            }
        }

        private static void TestClientRead<T>(IUnitOfWorkScope scope, string expected, Func<T, object> reporter, ReadCommandInfo readCommand = null)
            where T : class, IEntity
        {
            readCommand = readCommand ?? new ReadCommandInfo();
            readCommand.DataSource = typeof(T).FullName;
            readCommand.ReadRecords = true;

            var loaded = ExecuteCommand(readCommand, scope).Records;
            var report = loaded.Select(item => reporter((T)item).ToString());
            if (readCommand.OrderByProperties == null)
                report = report.OrderBy(x => x);
            Assert.AreEqual(expected, string.Join(", ", report));
        }

        [TestMethod]
        public void AutoFilter_Complex()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestFilter.AutoFilter1.Delete(repository.TestFilter.AutoFilter1.Query());
                repository.TestFilter.AutoFilter2.Delete(repository.TestFilter.AutoFilter2.Query());

                repository.TestFilter.AutoFilter1.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter1 { Name = name }));

                repository.TestFilter.AutoFilter2.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter2 { Name = name }));

                var gr = scope.Resolve<GenericRepositories>();

                var readCommand = new ReadCommandInfo
                {
                    Filters = new FilterCriteria[] { new FilterCriteria("Name", "contains", "2") },
                    OrderByProperties = new OrderByProperty[] { new OrderByProperty { Property = "Name", Descending = true } },
                    ReadTotalCount = true,
                    Top = 1
                };
                TestClientRead<TestFilter.AutoFilter1>(scope, "a2", item => item.Name, readCommand);

                readCommand = new ReadCommandInfo
                {
                    Filters = new FilterCriteria[] { new FilterCriteria("Name2", "contains", "2") },
                    OrderByProperties = new OrderByProperty[] { new OrderByProperty { Property = "Name2", Descending = true } },
                    ReadTotalCount = true,
                    Top = 1
                };
                TestClientRead<TestFilter.AutoFilter2Browse>(scope, "b2x", item => item.Name2, readCommand);
            }
        }

        [TestMethod]
        public void AutoFilter_NoRedundantAutoFilter()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestFilter.AutoFilter1.Delete(repository.TestFilter.AutoFilter1.Query());
                repository.TestFilter.AutoFilter2.Delete(repository.TestFilter.AutoFilter2.Query());

                repository.TestFilter.AutoFilter1.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter1 { Name = name }));

                repository.TestFilter.AutoFilter2.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter2 { Name = name }));

                var gr = scope.Resolve<GenericRepositories>();

                // Number of 'x' characters in the Name property shows how many times the filter was applied.
                // Auto filter should not be applied if the filter was already manually applied.

                var readCommand = new ReadCommandInfo
                {
                    Filters = new FilterCriteria[] {
                        new FilterCriteria("Name2", "contains", "2"),
                        new FilterCriteria(typeof(string)) },
                    OrderByProperties = new OrderByProperty[] { new OrderByProperty { Property = "Name2", Descending = true } },
                    ReadTotalCount = true,
                    Top = 1
                };
                TestClientRead<TestFilter.AutoFilter2Browse>(scope, "b2x", item => item.Name2, readCommand);

                // Same filter manually applied multiple times.

                readCommand = new ReadCommandInfo
                {
                    Filters = new FilterCriteria[] {
                        new FilterCriteria("Name2", "contains", "2"),
                        new FilterCriteria(typeof(string)),
                        new FilterCriteria("abc") },
                    OrderByProperties = new OrderByProperty[] { new OrderByProperty { Property = "Name2", Descending = true } },
                    ReadTotalCount = true,
                    Top = 1
                };
                TestClientRead<TestFilter.AutoFilter2Browse>(scope, "b2xx", item => item.Name2, readCommand);
            }
        }

        [TestMethod]
        public void AutoFilter_Where()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestFilter.AutoFilter3.Delete(repository.TestFilter.AutoFilter3.Query());

                repository.TestFilter.AutoFilter3.Insert(
                    new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestFilter.AutoFilter3 { Name = name }));

                var readAll = new ReadCommandInfo
                {
                    ReadRecords = true,
                    OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                };
                var read10 = new ReadCommandInfo
                {
                    ReadRecords = true,
                    OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                    Top = 10,
                };
                var readFiltered = new ReadCommandInfo
                {
                    ReadRecords = true,
                    OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                    Filters = new[] { new FilterCriteria { Filter = "TestFilter.WithA" } },
                };
                var readFiltered10 = new ReadCommandInfo
                {
                    ReadRecords = true,
                    OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                    Top = 10,
                    Filters = new[] { new FilterCriteria { Filter = "TestFilter.WithA" } },
                };

                TestClientRead<TestFilter.AutoFilter3>(scope, "a1, a2, b1, b2", item => item.Name, readAll);
                TestClientRead<TestFilter.AutoFilter3>(scope, "a1, a2", item => item.Name, read10);
                TestClientRead<TestFilter.AutoFilter3>(scope, "a1, a2", item => item.Name, readFiltered);
                TestClientRead<TestFilter.AutoFilter3>(scope, "a1, a2", item => item.Name, readFiltered10);

                Assert.AreEqual("", TestUtility.Dump(readAll.Filters.Select(f => f.Filter)));
                Assert.AreEqual("TestFilter.WithA", TestUtility.Dump(read10.Filters.Select(f => f.Filter)));
                Assert.AreEqual("TestFilter.WithA", TestUtility.Dump(readFiltered.Filters.Select(f => f.Filter)));
                Assert.AreEqual("TestFilter.WithA", TestUtility.Dump(readFiltered10.Filters.Select(f => f.Filter))); // To make sure the filter is not duplicated.
            }
        }

        [TestMethod]
        public void ItemFilterReferenced()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var source = repository.TestFilter.Source;
                var detail = repository.TestFilter.SourceDetail;

                var testSources = new[] { "a1", "a2", "b1", "b2" }.Select(name => new TestFilter.Source { Name = name }).ToList();
                source.Save(testSources, null, source.Load());

                var testDetails = testSources.Select(s => new TestFilter.SourceDetail { ParentID = s.ID, Name2 = "d" }).ToList();
                detail.Save(testDetails, null, detail.Load());

                Assert.AreEqual("a1d, b1d", TestUtility.DumpSorted(detail.Filter(detail.Query(), new TestFilter.Composable()).Select(d => d.Parent.Name + d.Name2)));
            }
        }

        //=====================================================================================================

        [TestMethod]
        public void QueryFilterParemeterDataStructure()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var parents = new[]
                {
                    new TestFilter.Simple { Name = "a0" },
                    new TestFilter.Simple { Name = "b1" },
                };
                repository.TestFilter.Simple.Insert(parents);

                var items = new[]
                {
                    new TestFilter.SimpleItem { SimpleID = parents[0].ID, Name = "si0" },
                    new TestFilter.SimpleItem { SimpleID = parents[0].ID, Name = "si1" },
                    new TestFilter.SimpleItem { SimpleID = parents[1].ID, Name = "si2" },
                    new TestFilter.SimpleItem { SimpleID = parents[1].ID, Name = "si3" },
                };
                repository.TestFilter.SimpleItem.Insert(items);

                var filter = new TestFilter.FilterByPrefix { Prefix = "a" };

                var ids = items.Select(item => item.ID).ToList();
                var query = repository.TestFilter.SimpleItem.Query();

                var directFilterImplementation = repository.TestFilter.SimpleItem.Filter(query, filter).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(directFilterImplementation, item => item.Name));
                 
                var genericWithExplicitFilterType = repository.TestFilter.SimpleItem.Load(filter, filter.GetType()).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(genericWithExplicitFilterType, item => item.Name));

                var genericWithImplicitFilterType = repository.TestFilter.SimpleItem.Load(filter).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(genericWithImplicitFilterType, item => item.Name));

                var legacyFilterMethod = repository.TestFilter.SimpleItem.Filter(filter).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(legacyFilterMethod, item => item.Name));
            }
        }

        [TestMethod]
        public void QueryFilterCustomPareter()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var items = new[]
                {
                    new TestFilter.SimpleItem { Name = "si0" },
                    new TestFilter.SimpleItem { Name = "si1" },
                    new TestFilter.SimpleItem { Name = "si2" },
                    new TestFilter.SimpleItem { Name = "si3" },
                };
                repository.TestFilter.SimpleItem.Insert(items);

                var filter = new HashSet<string>(new[] { "si0", "si1" });

                var ids = items.Select(item => item.ID).ToList();
                var query = repository.TestFilter.SimpleItem.Query();

                var directFilterImplementation = repository.TestFilter.SimpleItem.Filter(query, filter).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(directFilterImplementation, item => item.Name));

                var genericWithExplicitFilterType = repository.TestFilter.SimpleItem.Load(filter, filter.GetType()).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(genericWithExplicitFilterType, item => item.Name));

                var genericWithImplicitFilterType = repository.TestFilter.SimpleItem.Load(filter).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(genericWithImplicitFilterType, item => item.Name));

                var legacyFilterMethod = repository.TestFilter.SimpleItem.Filter(filter).Where(item => ids.Contains(item.ID));
                Assert.AreEqual("si0, si1", TestUtility.DumpSorted(legacyFilterMethod, item => item.Name));
            }
        }

        [TestMethod]
        public void DataStructureReadParameters_Basic()
        {
            using (var scope = TestScope.Create())
            {
                var readParameters = scope.Resolve<IDataStructureReadParameters>().GetReadParameters(
                    "TestFilter.MultipleReadTypes",
                    extendedSet: false);

                var expected = @"
CommonFilter2: TestFilter.CommonFilter2
IEnumerable<MultipleReadTypes>: System.Collections.Generic.IEnumerable`1[TestFilter.MultipleReadTypes]
List<TestFilter.MultipleReadTypes>: System.Collections.Generic.List`1[TestFilter.MultipleReadTypes]
QueryFilter1: TestFilter.QueryFilter1
string[]: System.String[]
System.Collections.Generic.IEnumerable<System.Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]
System.Guid: System.Guid
TestFilter.CommonFilter2: TestFilter.CommonFilter2
TestFilter.FilterBy1: TestFilter.FilterBy1
TestFilter.ItemFilter1: TestFilter.ItemFilter1
TestFilter.Query1: TestFilter.Query1
";
                Assert.AreEqual(
                    string.Join("\r\n", expected.Split("\r\n").Where(line => !string.IsNullOrWhiteSpace(line)).OrderBy(x => x)),
                    string.Join("\r\n", readParameters.Select(rp => rp.ToString()).OrderBy(x => x)));
            }
        }

        [TestMethod]
        public void DataStructureReadParameters_Extended()
        {
            using (var scope = TestScope.Create())
            {
                var readParameters = scope.Resolve<IDataStructureReadParameters>().GetReadParameters(
                    "TestFilter.MultipleReadTypes",
                    extendedSet: true);

                var expected = @"
CommonFilter2: TestFilter.CommonFilter2

IEnumerable<MultipleReadTypes>: System.Collections.Generic.IEnumerable`1[TestFilter.MultipleReadTypes]
MultipleReadTypes[]: TestFilter.MultipleReadTypes[]

List<TestFilter.MultipleReadTypes>: System.Collections.Generic.List`1[TestFilter.MultipleReadTypes]
List<MultipleReadTypes>: System.Collections.Generic.List`1[TestFilter.MultipleReadTypes]

QueryFilter1: TestFilter.QueryFilter1

string[]: System.String[]

System.Collections.Generic.IEnumerable<System.Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]
IEnumerable<Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]
System.Guid[]: System.Guid[]
Guid[]: System.Guid[]

System.Guid: System.Guid
Guid: System.Guid

TestFilter.CommonFilter2: TestFilter.CommonFilter2
CommonFilter2: TestFilter.CommonFilter2

TestFilter.FilterBy1: TestFilter.FilterBy1
FilterBy1: TestFilter.FilterBy1

TestFilter.ItemFilter1: TestFilter.ItemFilter1
ItemFilter1: TestFilter.ItemFilter1

TestFilter.Query1: TestFilter.Query1
Query1: TestFilter.Query1
";
                Assert.AreEqual(
                    string.Join("\r\n", expected.Split("\r\n").Distinct().Where(line => !string.IsNullOrWhiteSpace(line)).OrderBy(x => x)),
                    string.Join("\r\n", readParameters.Select(rp => rp.ToString()).OrderBy(x => x)));
            }
        }
    }
}
