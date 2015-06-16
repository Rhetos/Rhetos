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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class BlomRepositoryConcepts
    {
        [TestMethod]
        public void OnSaveUpdateAndVerify()
        {
            using (var container = new RhetosTestContainer())
            {
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

                baseRepos.Insert(baseItems);
                testerRepos.Insert(testerItems);

                Assert.AreEqual("b0, b1 locked", TestUtility.DumpSorted(baseRepos.Load(), item => item.Name));
                Assert.AreEqual("b0/t0-11, b1 locked/t1-22", TestUtility.DumpSorted(testerRepos.Query(), item => item.Base.Name + "/" + item.Name + "-" + item.Code));

                testerItems[0].Name += " modified"; // Modifying Name property should update the base entity's name ("updated").
                testerItems[1].Code = 222;
                testerRepos.Update(testerItems);

                Assert.AreEqual("b0 updated, b1 locked", TestUtility.DumpSorted(baseRepos.Load(), item => item.Name));
                Assert.AreEqual("b0 updated/t0 modified-11, b1 locked/t1-222", TestUtility.DumpSorted(testerRepos.Query(), item => item.Base.Name + "/" + item.Name + "-" + item.Code));

                testerItems[0].Code = 111;
                testerItems[1].Name += " modified"; // Modifying Name property when base is locked should fail.
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => testerRepos.Update(testerItems),
                    "It is not allowed to modify locked item's name 't1' => 't1 modified'.");
            }
        }
    }
}
