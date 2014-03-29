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
using CommonConcepts.Test.Utilities;

namespace CommonConcepts.Test
{
    [TestClass]
    public class QueryDataSourceCommandTest
    {
        private static void InitializeData(Common.ExecutionContext executionContext)
        {
            executionContext.SqlExecuter.ExecuteSql(new[]
                {
                    "DELETE FROM TestQueryDataStructureCommand.E;",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'a';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'b';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'c';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'd';",
                    "INSERT INTO TestQueryDataStructureCommand.E(Name) SELECT 'e';"
                });
        }

        private static string ReportCommandResult(Common.ExecutionContext executionContext, ICommandInfo info, bool sort = false)
        {
            var repositories = Create.GenericRepositories(executionContext);

            ICommandImplementation command = new QueryDataSourceCommand(new SimpleDataTypeProvider(), repositories);
            var result = (QueryDataSourceCommandResult)command.Execute(info).Data.Value;
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                InitializeData(executionContext);

                var info = new QueryDataSourceCommandInfo { DataSource = "TestQueryDataStructureCommand.E" };
                Assert.AreEqual("a, b, c, d, e /5", ReportCommandResult(executionContext, info, true));
            }
        }

        [TestMethod]
        public void Ordering()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                InitializeData(executionContext);

                var info = new QueryDataSourceCommandInfo
                               {
                                   DataSource = "TestQueryDataStructureCommand.E",
                                   OrderByProperty = "Name",
                                   OrderDescending = true
                               };
                Assert.AreEqual("e, d, c, b, a /5", ReportCommandResult(executionContext, info));
            }
        }

        [TestMethod]
        public void Paging()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                InitializeData(executionContext);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    RecordsPerPage = 3,
                    PageNumber = 2,
                    OrderByProperty = "Name",
                    OrderDescending = true
                };
                Assert.AreEqual("b, a /5", ReportCommandResult(executionContext, info));
            }
        }

        [TestMethod]
        public void PagingWithoutOrder()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                InitializeData(executionContext);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    RecordsPerPage = 3,
                    PageNumber = 2
                };

                string exceptionMessage = "";
                try
                {
                    ReportCommandResult(executionContext, info);
                }
                catch (Exception ex)
                {
                    exceptionMessage = ex.Message;
                    Console.WriteLine(exceptionMessage);
                }
                Assert.IsTrue(exceptionMessage.Contains("OrderByProperty"));
            }
        }

        [TestMethod]
        public void GenericFilter()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                InitializeData(executionContext);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    GenericFilter = new [] { new FilterCriteria { Property = "Name", Operation = "NotEqual", Value = "c" } }
                };
                Assert.AreEqual("a, b, d, e /4", ReportCommandResult(executionContext, info, true));
            }
        }

        [TestMethod]
        public void GenericFilterWithPaging()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                InitializeData(executionContext);

                var info = new QueryDataSourceCommandInfo
                {
                    DataSource = "TestQueryDataStructureCommand.E",
                    GenericFilter = new [] { new FilterCriteria { Property = "Name", Operation = "NotEqual", Value = "c" } },
                    RecordsPerPage = 2,
                    PageNumber = 2,
                    OrderByProperty = "Name"
                };
                Assert.AreEqual("d, e /4", ReportCommandResult(executionContext, info));
            }
        }

        //====================================================================

        private static string ReportCommandResult2(Common.ExecutionContext executionContext, ICommandInfo info, bool sort = false)
        {
            ICommandImplementation command = new QueryDataSourceCommand(new SimpleDataTypeProvider(), Create.GenericRepositories(executionContext));
            var result = (QueryDataSourceCommandResult)command.Execute(info).Data.Value;
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestQueryDataStructureCommand.Source;" });
                executionContext.SqlExecuter.ExecuteSql(new[] { "a1", "b1", "b2", "c1" }
                    .Select(name => "INSERT INTO TestQueryDataStructureCommand.Source (Name) SELECT N'" + name + "';"));

                var info = new QueryDataSourceCommandInfo { DataSource = "TestQueryDataStructureCommand.Source" };
                Assert.AreEqual("a1, b1, b2, c1 /4", ReportCommandResult2(executionContext, info, true));

                info.Filter = new TestQueryDataStructureCommand.FilterByPrefix {  Prefix = "b"};
                Assert.AreEqual("b1, b2 /2", ReportCommandResult2(executionContext, info, true));

                info.OrderByProperty = "Name";
                info.OrderDescending = true;
                Assert.AreEqual("b2, b1 /2", ReportCommandResult2(executionContext, info));

                info.PageNumber = 1;
                info.RecordsPerPage = 1;
                Assert.AreEqual("b2 /2", ReportCommandResult2(executionContext, info));

                info.GenericFilter = new[] { new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } };
                Assert.AreEqual("b1 /1", ReportCommandResult2(executionContext, info));
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
}
