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

using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DomRepositoryConcepts
    {
        [TestMethod]
        public void OnSaveUpdateAndValidate()
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log, EventType.Info);
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestDataStructure.SaveTesterBase" });

                var context = container.Resolve<Common.ExecutionContext>();

                var baseRepos = context.Repository.TestDataStructure.SaveTesterBase;
                var testerRepos = context.Repository.TestDataStructure.SaveTester;

                var baseItems = new[] {
                    new TestDataStructure.SaveTesterBase { ID = Guid.NewGuid(), Name = "b0" },
                    new TestDataStructure.SaveTesterBase { ID = Guid.NewGuid(), Name = "b1 locked" } };

                var testerItems = new[] {
                    new TestDataStructure.SaveTester { ID = baseItems[0].ID, Name = "t0", Code = 11 },
                    new TestDataStructure.SaveTester { ID = baseItems[1].ID, Name = "t1", Code = 22 } };

                log.Clear();
                baseRepos.Insert(baseItems);
                testerRepos.Insert(testerItems);
                Assert.AreEqual("", TestUtility.Dump(log));

                Assert.AreEqual("b0, b1 locked", TestUtility.DumpSorted(baseRepos.Load(), item => item.Name));
                Assert.AreEqual("b0/t0-11, b1 locked/t1-22", TestUtility.DumpSorted(testerRepos.Query(), item => item.Base.Name + "/" + item.Name + "-" + item.Code));

                testerItems[0].Name += " modified"; // Modifying Name property should update the base entity's name ("updated").
                testerItems[1].Code = 222;
                log.Clear();
                testerRepos.Update(testerItems);
                Assert.AreEqual("[Info] test: t0 => t0 modified", TestUtility.Dump(log));

                Assert.AreEqual("b0 updated, b1 locked", TestUtility.DumpSorted(baseRepos.Load(), item => item.Name));
                Assert.AreEqual("b0 updated/t0 modified-11, b1 locked/t1-222", TestUtility.DumpSorted(testerRepos.Query(), item => item.Base.Name + "/" + item.Name + "-" + item.Code));

                testerItems[0].Code = 111;
                testerItems[1].Name += " modified"; // Modifying Name property when base is locked should fail.
                log.Clear();
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => testerRepos.Update(testerItems),
                    "It is not allowed to modify locked item's name 't1' => 't1 modified'.");
                Assert.AreEqual("", TestUtility.Dump(log));
            }
        }

        [TestMethod]
        public void SaveMethodInitialization()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestDataStructure.SaveTesterBase" });

                var context = container.Resolve<Common.ExecutionContext>();

                var baseRepos = context.Repository.TestDataStructure.SaveTesterBase;
                var testerRepos = context.Repository.TestDataStructure.SaveTester;

                var baseItem = new TestDataStructure.SaveTesterBase { ID = Guid.NewGuid(), Name = "b0" };
                var testerItem = new TestDataStructure.SaveTester { ID = baseItem.ID, Name = "default", Code = 11 };

                baseRepos.Insert(baseItem);
                testerRepos.Insert(testerItem);

                Assert.AreEqual("b0/initialized-11", TestUtility.DumpSorted(testerRepos.Query(), item => item.Base.Name + "/" + item.Name + "-" + item.Code));
            }
        }

        [TestMethod]
        public void AutomaticallyDeleteExtensionWithBusinessLogic()
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log);
                var repository = container.Resolve<Common.DomRepository>();

                var baseItem = new TestDataStructure.SaveTesterBase { Name = "b1" };
                repository.TestDataStructure.SaveTesterBase.Insert(baseItem);

                var extensionItem = new TestDataStructure.SaveTester { ID = baseItem.ID, Name = "e1" };
                repository.TestDataStructure.SaveTester.Insert(extensionItem);

                Assert.AreEqual(1, repository.TestDataStructure.SaveTester.Query(new[] { extensionItem.ID }).Count()); // Just checking the test is not false positive.

                log.Clear();
                repository.TestDataStructure.SaveTesterBase.Delete(baseItem);

                Console.WriteLine("LOG BEGIN\r\n" + string.Join("\r\n", log) + "\r\nLOG END");

                Assert.AreEqual(0, repository.TestDataStructure.SaveTester.Query(new[] { extensionItem.ID }).Count()); // Just checking the test is not false positive.

                Assert.IsTrue(log.Any(entry => entry.Contains("SaveTester.Deletions: e1.")), "Deleting the base entity should result with deletion of the extension through the object model (not just deleted in database by cascade delete).");
            }
        }
    }
}
