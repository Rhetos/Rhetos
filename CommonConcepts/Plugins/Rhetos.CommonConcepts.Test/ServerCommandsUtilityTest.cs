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
using Rhetos.CommonConcepts.Test.Mocks;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Processing.DefaultCommands;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.CommonConcepts.Test
{
    static class LegacyExtension
    {
        public static ReadCommandResult ExecuteReadCommand(
            this GenericRepository<IEntity> genericRepository,
            ReadCommandInfo readCommandInfo)
        {
            readCommandInfo.DataSource = typeof(ServerCommandsUtilityTest.SimpleEntity).FullName;
            var serverCommandsUtility = new ServerCommandsUtility(new ConsoleLogProvider(), new ApplyFiltersOnClientRead());
            var commandResult = serverCommandsUtility.ExecuteReadCommand(readCommandInfo, genericRepository);
            return commandResult;
        }
    }

    [TestClass]
    public class ServerCommandsUtilityTest
    {
        interface ISimpleEntity : IEntity
        {
            string Name { get; set; }
        }

        public class SimpleEntity : ISimpleEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public string Data { get; set; }

            public override string ToString()
            {
                return Name != null ? Name : "<null>";
            }
        }

        class SimpleEntityList : List<SimpleEntity>
        {
            int idCounter = 0;
            public void Add(string name) { Add(new SimpleEntity { Name = name, ID = new Guid(idCounter++, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) }); }
        }

        GenericRepository<IEntity> NewRepos(IRepository repository)
        {
            return new TestGenericRepository<IEntity, SimpleEntity>(repository);
        }

        class ImplicitReadCommandRepository : IRepository
        {
            public int QueryCount = 0;
            public int DropQueryCount() { int t = QueryCount; QueryCount = 0; return t; }

            public IQueryable<SimpleEntity> Query()
            {
                return new[] { 1 }
                    .SelectMany(x =>
                    {
                        QueryCount++;
                        return new SimpleEntityList { "a1", "b1", "b2", "b3", "b4", "b5" };
                    }).AsQueryable();
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> source, string pattern)
            {
                return source.Where(item => item.Name.Contains(pattern));
            }

            public static readonly KeyValuePair<string, Type>[] ReadParameterTypes = new KeyValuePair<string, Type>[]
            {};
        }

        class ExplicitReadCommandRepository : IRepository
        {
            public ReadCommandResult ReadCommand(ReadCommandInfo commandInfo)
            {
                return new ReadCommandResult
                {
                    Records = new SimpleEntityList { "a1", "b1", "b2" }.ToArray(),
                    TotalCount = 10
                };
            }
        }

        static string Dump(ReadCommandResult commandResult)
        {
            return TestUtility.Dump(commandResult.Records) + " / " + commandResult.TotalCount.Value;
        }

        [TestMethod]
        public void ReadCommand()
        {
            var entityRepos = new ImplicitReadCommandRepository();
            var genericRepos = NewRepos(entityRepos);

            var command = new ReadCommandInfo
            {
                Filters = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } },
                OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                Top = 3,
                Skip = 3,
                ReadRecords = true,
                ReadTotalCount = true,
            };
            Assert.AreEqual("a1, b1, b2 / 10", Dump(NewRepos(new ExplicitReadCommandRepository()).ExecuteReadCommand(command)));
            Assert.AreEqual(0, entityRepos.DropQueryCount());
            Assert.AreEqual("b4, b5 / 5", Dump(genericRepos.ExecuteReadCommand(command)));
            Assert.AreEqual(2, entityRepos.DropQueryCount()); // Paging should result with two queries: selecting items and count.

            command = new ReadCommandInfo { ReadRecords = true, ReadTotalCount = true };
            Assert.AreEqual("a1, b1, b2, b3, b4, b5 / 6", Dump(genericRepos.ExecuteReadCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new ReadCommandInfo { Filters = new[] { new FilterCriteria("1") }, ReadRecords = true, ReadTotalCount = true };
            Assert.AreEqual("a1, b1 / 2", Dump(genericRepos.ExecuteReadCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new ReadCommandInfo { Filters = new[] { new FilterCriteria("1"), new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } }, ReadRecords = true, ReadTotalCount = true };
            Assert.AreEqual("b1 / 1", Dump(genericRepos.ExecuteReadCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new ReadCommandInfo { Filters = new[] { new FilterCriteria("1"), new FilterCriteria { Filter = "System.String", Value = "b" } }, ReadRecords = true, ReadTotalCount = true };
            Assert.AreEqual("b1 / 1", Dump(genericRepos.ExecuteReadCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new ReadCommandInfo { Filters = new[] { new FilterCriteria("b") }, Top = 2, Skip = 2, OrderByProperties = new[] { new OrderByProperty { Property = "Name" } }, ReadRecords = true, ReadTotalCount = true };
            Assert.AreEqual("b3, b4 / 5", Dump(genericRepos.ExecuteReadCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Enumerable filter will cause GenericRepository to materialize of the query, so it will be executed only once even though the paging is used.
        }

        [TestMethod]
        public void ReadCommand_OutsideInterface()
        {
            var entityRepos = new ImplicitReadCommandRepository();
            var genericRepos = NewRepos(entityRepos);

            var command = new ReadCommandInfo
            {
                Filters = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } },
                OrderByProperties = new[] { new OrderByProperty { Property = "Data" } },
                Top = 3,
                Skip = 3,
                ReadRecords = true,
                ReadTotalCount = true,
            };
            Assert.AreEqual("b4, b5 / 5", Dump(genericRepos.ExecuteReadCommand(command)));

            command = new ReadCommandInfo
            {
                Filters = new[] { new FilterCriteria { Property = "Data", Operation = "Equal", Value = "xxx" } },
                OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                Top = 3,
                Skip = 3,
                ReadRecords = true,
                ReadTotalCount = true,
            };
            Assert.AreEqual(" / 0", Dump(genericRepos.ExecuteReadCommand(command)));
        }

        [TestMethod]
        public void ReadCommand_Null()
        {
            var entityRepos = new ImplicitReadCommandRepository();
            var genericRepos = NewRepos(entityRepos);

            var command = new ReadCommandInfo
            {
                Filters = new[] { new FilterCriteria { Property = "Data", Operation = "Equal", Value = null } },
                OrderByProperties = new[] { new OrderByProperty { Property = "Name" } },
                Top = 3,
                Skip = 3,
                ReadRecords = true,
                ReadTotalCount = true,
            };
            Assert.AreEqual("b3, b4, b5 / 6", Dump(genericRepos.ExecuteReadCommand(command)));
        }
    }
}
