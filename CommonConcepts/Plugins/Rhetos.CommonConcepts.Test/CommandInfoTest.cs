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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using Rhetos.Security;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;

namespace Rhetos.CommonConcepts.Test
{
    internal class SimpleParameter
    {
        public int Code { get; set; }
        public string Name { get; set; }
    }

    internal class SimpleEntity : IEntity
    {
        public Guid ID { get; set; }
        public int Code { get; set; }
        public string Name { get; set; }
    }

    [TestClass]
    public class CommandInfoTest
    {
        [TestMethod]
        public void TestToString()
        {
            var simpleParameter = new SimpleParameter { Code = 123, Name = "test" };
            var simpleEntity1 = new SimpleEntity { ID = new Guid("bb7eb875-8ec2-4a1e-86d6-103f5da490eb"), Code = 11, Name = "e1" };
            var simpleEntity2 = new SimpleEntity { ID = new Guid("ad8885bc-934e-4716-8524-d971bf541e62"), Code = 22, Name = "e2" };
            var simpleEntity3 = new SimpleEntity { ID = new Guid("7f161f06-360c-4f61-b700-ebcac4e1afb5"), Code = 33, Name = "e3" };

            Assert.AreEqual("Rhetos.Processing.DefaultCommands.DummyCommandInfo", new DummyCommandInfo { }.ToString());

            Assert.AreEqual("DownloadReportCommandInfo", new DownloadReportCommandInfo { }.ToString());
            Assert.AreEqual("DownloadReportCommandInfo Rhetos.CommonConcepts.Test.SimpleParameter", new DownloadReportCommandInfo { Report = simpleParameter }.ToString());
            Assert.AreEqual("DownloadReportCommandInfo Rhetos.CommonConcepts.Test.SimpleParameter as pdf", new DownloadReportCommandInfo { Report = simpleParameter, ConvertFormat = "pdf" }.ToString());

            Assert.AreEqual("ExecuteActionCommandInfo", new ExecuteActionCommandInfo { }.ToString());
            Assert.AreEqual("ExecuteActionCommandInfo Rhetos.CommonConcepts.Test.SimpleParameter", new ExecuteActionCommandInfo { Action = simpleParameter }.ToString());

            Assert.AreEqual("Rhetos.Processing.DefaultCommands.LoadDslModelCommandInfo", new LoadDslModelCommandInfo { }.ToString());
            Assert.AreEqual("Rhetos.Processing.DefaultCommands.PingCommandInfo", new PingCommandInfo { }.ToString());

            Assert.AreEqual("ReadCommandInfo ", new ReadCommandInfo { }.ToString());
            Assert.AreEqual("ReadCommandInfo Mod.Ent", new ReadCommandInfo { DataSource = "Mod.Ent" }.ToString());
            Assert.AreEqual("ReadCommandInfo Mod.Ent records count, order by Code -Name, skip 1, top 2, filters: Rhetos.CommonConcepts.Test.SimpleParameter \"...\", Rhetos.CommonConcepts.Test.SimpleParameter, Name StartsWith \"test\", System.Guid[] \"1 items: c56d3af5-db59-4b15-bd27-f800c36dc685\", System.Guid[] \"2 items: c56d3af5-db59-4b15-bd27-f800c36dc685 ...\"", new ReadCommandInfo
            {
                DataSource = "Mod.Ent",
                ReadRecords = true,
                ReadTotalCount = true,
                OrderByProperties = new[] {
                        new OrderByProperty { Property = "Code", Descending = false },
                        new OrderByProperty { Property = "Name", Descending = true } },
                Skip = 1,
                Top = 2,
                Filters = new[] {
                        new FilterCriteria(simpleParameter),
                        new FilterCriteria(typeof(SimpleParameter)),
                        new FilterCriteria("Name", "StartsWith", "test"),
                        new FilterCriteria(new[] { new Guid("c56d3af5-db59-4b15-bd27-f800c36dc685") }),
                        new FilterCriteria(new[] { new Guid("c56d3af5-db59-4b15-bd27-f800c36dc685"), new Guid("a378621c-b784-4005-a304-1c92e2f07d95") }) },
            }.ToString());
            Assert.AreEqual("ReadCommandInfo , order by   test -, filters: ,  , test, test ,  test,   \"test\", test test, test  \"test\",  test \"test\"", new ReadCommandInfo
            {
                Filters = new[] {
                        null,
                        new FilterCriteria { },
                        new FilterCriteria { Filter = "test" },
                        new FilterCriteria { Property = "test" },
                        new FilterCriteria { Operation = "test" },
                        new FilterCriteria { Value = "test" },
                        new FilterCriteria { Property = "test", Operation = "test" },
                        new FilterCriteria { Property = "test", Value = "test" },
                        new FilterCriteria { Operation = "test", Value = "test" } },
                OrderByProperties = new[] {
                        null,
                        new OrderByProperty { },
                        new OrderByProperty { Property = "test" },
                        new OrderByProperty { Descending = true },
                        }
            }.ToString());
            Assert.AreEqual("ReadCommandInfo , filters: Rhetos.CommonConcepts.Test.SimpleEntity \"ID bb7eb875-8ec2-4a1e-86d6-103f5da490eb\"", new ReadCommandInfo
            {
                Filters = new[] { new FilterCriteria(new SimpleEntity { ID = new Guid("bb7eb875-8ec2-4a1e-86d6-103f5da490eb"), Code = 11, Name = "e1" }) }
            }.ToString());

            Assert.AreEqual("SaveEntityCommandInfo ", new SaveEntityCommandInfo { }.ToString());
            Assert.AreEqual("SaveEntityCommandInfo Mod.Ent, insert 3", new SaveEntityCommandInfo
            {
                Entity = "Mod.Ent",
                DataToInsert = new[] { simpleEntity1, simpleEntity2, simpleEntity3 }
            }.ToString());
            Assert.AreEqual("SaveEntityCommandInfo Mod.Ent, insert 1, update 1, delete 1", new SaveEntityCommandInfo
            {
                Entity = "Mod.Ent",
                DataToInsert = new[] { simpleEntity1 },
                DataToUpdate = new[] { simpleEntity2 },
                DataToDelete = new[] { simpleEntity3 }
            }.ToString());

            Assert.AreEqual("QueryDataSourceCommandInfo ", new QueryDataSourceCommandInfo { }.ToString());
            Assert.AreEqual("QueryDataSourceCommandInfo Mod.Ent", new QueryDataSourceCommandInfo { DataSource = "Mod.Ent" }.ToString());
        }
    }
}
