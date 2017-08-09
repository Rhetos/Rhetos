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
using Autofac.Features.Indexed;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using Rhetos.XmlSerialization;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.TestCommon;
using Rhetos.Logging;

namespace CommonConcepts.Test
{
#pragma warning disable CS0618 // Type or member is obsolete (QueryDataSourceCommandInfo)

    [TestClass]
    public class QueryDataSourceCommandTest
    {
        private static void InitializeData(RhetosTestContainer container)
        {
            container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    "DELETE FROM TestQueryDataStructureCommand.E;",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'a';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'b';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'c';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'd';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'e';"
                });
        }

        private static string ReportCommandResult(RhetosTestContainer container, QueryDataSourceCommandInfo info, bool sort = false)
        {
            var commands = container.Resolve<IIndex<Type, IEnumerable<ICommandImplementation>>>();
            var queryDataSourceCommand = (QueryDataSourceCommand)commands[typeof(QueryDataSourceCommandInfo)].Single();

            var result = (QueryDataSourceCommandResult)queryDataSourceCommand.Execute(info).Data.Value;
            var items = ((IEnumerable<TestQueryDataStructureCommand.E>)result.Records).Select(item => item.Name);
            if (sort)
                items = items.OrderBy(x => x);
            var report = string.Join(", ", items) + " /" + result.TotalRecords;
            Console.WriteLine(report);
            return report;
        }

        [TestMethod]
        public void Entity()
        {
            using (var container = new RhetosTestContainer())
            {
                InitializeData(container);

                var info = new QueryDataSourceCommandInfo { DataSource = "TestQueryDataStructureCommand.E" };
                Assert.AreEqual("a, b, c, d, e /5", ReportCommandResult(container, info, true));
            }
        }

        [TestMethod]
        public void Ordering()
        {
            using (var container = new RhetosTestContainer())
            {
                InitializeData(container);

                var info = new QueryDataSourceCommandInfo
                               {
                                   DataSource = "TestQueryDataStructureCommand.E",
                                   OrderByProperty = "Name",
                                   OrderDescending = true
                               };
                Assert.AreEqual("e, d, c, b, a /5", ReportCommandResult(container, info));
            }
        }

        [TestMethod]
        public void Paging()
        {
            using (var container = new RhetosTestContainer())
            {
                InitializeData(container);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    RecordsPerPage = 3,
                    PageNumber = 2,
                    OrderByProperty = "Name",
                    OrderDescending = true
                };
                Assert.AreEqual("b, a /5", ReportCommandResult(container, info));
            }
        }

        [TestMethod]
        public void PagingWithoutOrder()
        {
            using (var container = new RhetosTestContainer())
            {
                InitializeData(container);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    RecordsPerPage = 3,
                    PageNumber = 2
                };

                TestUtility.ShouldFail(() => ReportCommandResult(container, info), "Sort order must be set if paging is used");
            }
        }

        [TestMethod]
        public void GenericFilter()
        {
            using (var container = new RhetosTestContainer())
            {
                InitializeData(container);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    GenericFilter = new [] { new FilterCriteria { Property = "Name", Operation = "NotEqual", Value = "c" } }
                };
                Assert.AreEqual("a, b, d, e /4", ReportCommandResult(container, info, true));
            }
        }

        [TestMethod]
        public void GenericFilterWithPaging()
        {
            using (var container = new RhetosTestContainer())
            {
                InitializeData(container);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    GenericFilter = new [] { new FilterCriteria { Property = "Name", Operation = "NotEqual", Value = "c" } },
                    RecordsPerPage = 2,
                    PageNumber = 2,
                    OrderByProperty = "Name"
                };
                Assert.AreEqual("d, e /4", ReportCommandResult(container, info));
            }
        }

        //====================================================================

        private static string ReportCommandResult2(RhetosTestContainer container, QueryDataSourceCommandInfo info, bool sort = false)
        {
            var commands = container.Resolve<IIndex<Type, IEnumerable<ICommandImplementation>>>();
            var queryDataSourceCommand = (QueryDataSourceCommand)commands[typeof(QueryDataSourceCommandInfo)].Single();

            var result = (QueryDataSourceCommandResult)queryDataSourceCommand.Execute(info).Data.Value;
            var items = ((IEnumerable<TestQueryDataStructureCommand.Source>)result.Records).Select(item => item.Name);
            if (sort)
                items = items.OrderBy(x => x);
            var report = string.Join(", ", items) + " /" + result.TotalRecords;
            Console.WriteLine(report);
            return report;
        }


        [TestMethod]
        public void Filter()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestQueryDataStructureCommand.Source;" });
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestQueryDataStructureCommand.Source (Name) SELECT N'" + name + "';"));

                var info = new QueryDataSourceCommandInfo { DataSource = "TestQueryDataStructureCommand.Source" };
                Assert.AreEqual("a1, b1, b2, c1 /4", ReportCommandResult2(container, info, true));

                info.Filter = new TestQueryDataStructureCommand.FilterByPrefix {  Prefix = "b"};
                Assert.AreEqual("b1, b2 /2", ReportCommandResult2(container, info, true));

                info.OrderByProperty = "Name";
                info.OrderDescending = true;
                Assert.AreEqual("b2, b1 /2", ReportCommandResult2(container, info));

                info.PageNumber = 1;
                info.RecordsPerPage = 1;
                Assert.AreEqual("b2 /2", ReportCommandResult2(container, info));

                info.GenericFilter = new[] { new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } };
                Assert.AreEqual("b1 /1", ReportCommandResult2(container, info));
            }
        }

        [TestMethod]
        public void NullGenericFilter()
        {
            using (var container = new RhetosTestContainer())
            {
                var genericRepos = container.Resolve<GenericRepositories>().GetGenericRepository("Common.Claim");

                var readCommand = new ReadCommandInfo
                {
                    DataSource = "Common.Claim",
                    Top = 3,
                    OrderByProperties = new[] { new OrderByProperty { Property = "ClaimResource" } },
                    ReadRecords = true,
                    ReadTotalCount = true
                };

                var serverCommandsUtility = container.Resolve<ServerCommandsUtility>();

                var readResult = serverCommandsUtility.ExecuteReadCommand(readCommand, genericRepos);
                Console.WriteLine("Records.Length: " + readResult.Records.Length);
                Console.WriteLine("TotalCount: " + readResult.TotalCount);
                Assert.IsTrue(readResult.Records.Length < readResult.TotalCount);
            }
        }
   }

    //====================================================================

    class SimpleDataTypeProvider : IDataTypeProvider
    {
        public IBasicData<T> CreateBasicData<T>(T value)
        {
            return new XmlBasicData<T> { Data = value };
        }

        public IDataArray CreateDataArray<T>(Type type, T[] data) where T : class
        {
            throw new NotImplementedException();
        }

        public IDataArray CreateDomainDataArray(string domainType)
        {
            throw new NotImplementedException();
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
