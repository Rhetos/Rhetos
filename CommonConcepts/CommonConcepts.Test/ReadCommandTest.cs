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

namespace CommonConcepts.Test
{
    [TestClass]
    public class ReadCommandTest
    {
        private static void InitializeData(IUnitOfWorkScope scope)
        {
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    "DELETE FROM TestQueryDataStructureCommand.E;",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'a';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'b';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'c';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'd';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'e';"
                });
        }

        private static string ReportCommandResult(IUnitOfWorkScope scope, ReadCommandInfo info, bool sort = false)
        {
            var commands = scope.Resolve<IPluginsContainer<ICommandImplementation>>();
            var readCommand = (ReadCommand)commands.GetImplementations(typeof(ReadCommandInfo)).Single();

            var result = readCommand.Execute(info);
            var items = ((IEnumerable<TestQueryDataStructureCommand.E>)result.Records).Select(item => item.Name);
            if (sort)
                items = items.OrderBy(x => x);
            var report = string.Join(", ", items) + " /" + result.TotalCount.Value;
            Console.WriteLine(report);
            return report;
        }

        [TestMethod]
        public void Entity()
        {
            using (var scope = TestScope.Create())
            {
                InitializeData(scope);

                var info = new ReadCommandInfo { DataSource = "TestQueryDataStructureCommand.E", ReadRecords = true, ReadTotalCount = true };
                Assert.AreEqual("a, b, c, d, e /5", ReportCommandResult(scope, info, true));
            }
        }

        [TestMethod]
        public void Ordering()
        {
            using (var scope = TestScope.Create())
            {
                InitializeData(scope);

                var info = new ReadCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    OrderByProperties = new[] { new OrderByProperty { Property = "Name", Descending = true } },
                    ReadRecords = true,
                    ReadTotalCount = true
                };
                Assert.AreEqual("e, d, c, b, a /5", ReportCommandResult(scope, info));
            }
        }

        [TestMethod]
        public void Paging()
        {
            using (var scope = TestScope.Create())
            {
                InitializeData(scope);

                var info = new ReadCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    Top = 3,
                    Skip = 3,
                    OrderByProperties = new[] { new OrderByProperty { Property = "Name", Descending = true } },
                    ReadRecords = true,
                    ReadTotalCount = true
                };
                Assert.AreEqual("b, a /5", ReportCommandResult(scope, info));
            }
        }

        [TestMethod]
        public void PagingWithoutOrder()
        {
            using (var scope = TestScope.Create())
            {
                InitializeData(scope);

                var info = new ReadCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    Top = 3,
                    Skip = 3,
                    ReadRecords = true,
                    ReadTotalCount = true
                };

                TestUtility.ShouldFail(() => ReportCommandResult(scope, info), "Sort order must be set if paging is used");
            }
        }

        [TestMethod]
        public void Filters()
        {
            using (var scope = TestScope.Create())
            {
                InitializeData(scope);

                var info = new ReadCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    Filters = new[] { new FilterCriteria { Property = "Name", Operation = "NotEqual", Value = "c" } },
                    ReadRecords = true,
                    ReadTotalCount = true
                };
                Assert.AreEqual("a, b, d, e /4", ReportCommandResult(scope, info, true));
            }
        }

        [TestMethod]
        public void GenericFilterWithPaging()
        {
            using (var scope = TestScope.Create())
            {
                InitializeData(scope);

                var info = new ReadCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    Filters = new[] { new FilterCriteria { Property = "Name", Operation = "NotEqual", Value = "c" } },
                    Top = 2,
                    Skip = 2,
                    OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                    ReadRecords = true,
                    ReadTotalCount = true
                };
                Assert.AreEqual("d, e /4", ReportCommandResult(scope, info));
            }
        }

        //====================================================================

        private static string ReportCommandResult2(IUnitOfWorkScope scope, ReadCommandInfo info, bool sort = false)
        {
            var commands = scope.Resolve<IPluginsContainer<ICommandImplementation>>();
            var readCommand = (ReadCommand)commands.GetImplementations(typeof(ReadCommandInfo)).Single();

            var result = readCommand.Execute(info);
            var items = ((IEnumerable<TestQueryDataStructureCommand.Source>)result.Records).Select(item => item.Name);
            if (sort)
                items = items.OrderBy(x => x);
            var report = string.Join(", ", items) + " /" + result.TotalCount.Value;
            Console.WriteLine(report);
            return report;
        }


        [TestMethod]
        public void Filter()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestQueryDataStructureCommand.Source;" });
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestQueryDataStructureCommand.Source (Name) SELECT N'" + name + "';"));

                var info = new ReadCommandInfo { DataSource = "TestQueryDataStructureCommand.Source", ReadRecords = true, ReadTotalCount = true };
                Assert.AreEqual("a1, b1, b2, c1 /4", ReportCommandResult2(scope, info, true));

                info.Filters = new[] { new FilterCriteria(new TestQueryDataStructureCommand.FilterByPrefix { Prefix = "b" }) };
                Assert.AreEqual("b1, b2 /2", ReportCommandResult2(scope, info, true));

                info.OrderByProperties = new[] { new OrderByProperty { Property = "Name", Descending = true } };
                Assert.AreEqual("b2, b1 /2", ReportCommandResult2(scope, info));

                info.Top = 1;
                Assert.AreEqual("b2 /2", ReportCommandResult2(scope, info));

                info.Filters = info.Filters.Concat(new[] { new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } }).ToArray();
                Assert.AreEqual("b1 /1", ReportCommandResult2(scope, info));
            }
        }

        [TestMethod]
        public void NullGenericFilter()
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
                    ReadTotalCount = true,
                };

                var serverCommandsUtility = scope.Resolve<ServerCommandsUtility>();

                var readResult = serverCommandsUtility.ExecuteReadCommand(readCommand, genericRepos);
                Console.WriteLine("Records.Length: " + readResult.Records.Length);
                Console.WriteLine("TotalCount: " + readResult.TotalCount);
                Assert.IsTrue(readResult.Records.Length < readResult.TotalCount);
            }
        }
    }
}
