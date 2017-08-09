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
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DenyUserEditTest
    {
        private string DumpSimple(Common.DomRepository repository)
        {
            return TestUtility.DumpSorted(
                repository.TestDenyUserEdit.Simple.Query(),
                item => (item.Editable ?? "null")
                    + " " + (item.NonEditable ?? "null")
                    + " " + (item.NonEditableReference != null ? item.NonEditableReference.Name : "null"));
        }

        [TestMethod]
        public void EditableProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var simple = new TestDenyUserEdit.Simple { Editable = "a" };
                repository.TestDenyUserEdit.Simple.Save(new[] { simple }, null, null, true);
                Assert.AreEqual("a null null", DumpSimple(repository));

                simple.Editable = "b";
                repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true);
                Assert.AreEqual("b null null", DumpSimple(repository));
            }
        }

        [TestMethod]
        public void NonEditablePropertyInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var simple = new TestDenyUserEdit.Simple { Editable = "a", NonEditable = "x" };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(new[] { simple }, null, null, true),
                    "Simple", "NonEditable", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditablePropertyUpdate()
        {
            using (var container = new RhetosTestContainer())
            {
                var simpleID = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent",
                        "INSERT INTO TestDenyUserEdit.Simple (ID, Editable, NonEditable) VALUES (" + SqlUtility.QuoteGuid(simpleID) + ", 'a', 'x')"
                    });

                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("a x null", DumpSimple(repository));

                var simple = new TestDenyUserEdit.Simple { ID = simpleID, Editable = "b", NonEditable = "x" };
                repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true);
                Assert.AreEqual("b x null", DumpSimple(repository));

                simple.NonEditable = "y";
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true),
                    "Simple", "NonEditable", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditablePropertyUpdateToNullIgnore()
        {
            using (var container = new RhetosTestContainer())
            {
                var simpleID = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent",
                        "INSERT INTO TestDenyUserEdit.Simple (ID, Editable, NonEditable) VALUES (" + SqlUtility.QuoteGuid(simpleID) + ", 'a', 'x')"
                    });

                var repository = container.Resolve<Common.DomRepository>();

                var simple = new TestDenyUserEdit.Simple { ID = simpleID, Editable = "a", NonEditable = null };
                //Client may ignore existence of the DenyUserEdit properties (the value will be null on save).
                repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true);
                Assert.AreEqual("x", repository.TestDenyUserEdit.Simple.Query().Where(item => item.ID == simpleID).Select(item => item.NonEditable).Single(), "Old value should remain unchanged after client sends null.");

                var simple2 = new TestDenyUserEdit.Simple { ID = simpleID, Editable = "a", NonEditable = null };
                // Explicit server update to null value should work.
                repository.TestDenyUserEdit.Simple.Save(null, new[] { simple2 }, null, false);
                Assert.IsNull(repository.TestDenyUserEdit.Simple.Query().Where(item => item.ID == simpleID).Select(item => item.NonEditable).Single(), "Explicit server update to null value should work.");
            }
        }

        [TestMethod]
        public void NonEditablePropertyUpdateFromNull()
        {
            using (var container = new RhetosTestContainer())
            {
                var simpleID = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent",
                        "INSERT INTO TestDenyUserEdit.Simple (ID, Editable, NonEditable) VALUES (" + SqlUtility.QuoteGuid(simpleID) + ", 'a', NULL)"
                    });

                var repository = container.Resolve<Common.DomRepository>();

                var simple = new TestDenyUserEdit.Simple { ID = simpleID, Editable = "a", NonEditable = "x" };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true),
                    "Simple", "NonEditable", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditableRefereceInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var parent = new TestDenyUserEdit.Parent { Name = "p" };
                repository.TestDenyUserEdit.Parent.Save(new[] { parent }, null, null, true);

                var simple = new TestDenyUserEdit.Simple { Editable = "a", NonEditableReferenceID = parent.ID };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(new[] { simple }, null, null, true),
                    "Simple", "NonEditableReference", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditableRefereceInsertGuid()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var parent = new TestDenyUserEdit.Parent { Name = "p" };
                repository.TestDenyUserEdit.Parent.Save(new[] { parent }, null, null, true);

                var simple = new TestDenyUserEdit.Simple { Editable = "a", NonEditableReferenceID = parent.ID };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(new[] { simple }, null, null, true),
                    "Simple", "NonEditableReference", "not allowed");
            }
        }

        [TestMethod]
        public void HardcodedEntity_Insert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                TestUtility.ShouldFail(
                    () => repository.TestDenyUserEdit.Hardcoded.Save(new[] { new TestDenyUserEdit.Hardcoded { Name = "abc" } }, null, null, true),
                    "It is not allowed to directly modify TestDenyUserEdit.Hardcoded.");
            }
        }

        [TestMethod]
        public void HardcodedEntity_Delete()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestDenyUserEdit.Hardcoded",
                    "INSERT INTO TestDenyUserEdit.Hardcoded (Name) VALUES ('abc')" });
                var repository = container.Resolve<Common.DomRepository>();

                var item = repository.TestDenyUserEdit.Hardcoded.Load().Single();
                Assert.AreEqual("abc", item.Name);
                TestUtility.ShouldFail(
                    () => repository.TestDenyUserEdit.Hardcoded.Save(null, null, new[] { item }, true),
                    "It is not allowed to directly modify TestDenyUserEdit.Hardcoded.");
            }
        }

        [TestMethod]
        public void HardcodedEntity_Update()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestDenyUserEdit.Hardcoded",
                    "INSERT INTO TestDenyUserEdit.Hardcoded (Name) VALUES ('abc')" });
                var repository = container.Resolve<Common.DomRepository>();

                var item = repository.TestDenyUserEdit.Hardcoded.Load().Single();
                Assert.AreEqual("abc", item.Name);
                item.Name += "x";
                TestUtility.ShouldFail(
                    () => repository.TestDenyUserEdit.Hardcoded.Save(null, new[] { item }, null, true),
                    "It is not allowed to directly modify TestDenyUserEdit.Hardcoded.");
            }
        }

        [TestMethod]
        public void AutoInitialized()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var sqlExecuter = container.Resolve<ISqlExecuter>();

                DateTime start = SqlUtility.GetDatabaseTime(sqlExecuter);

                var item = new TestDenyUserEdit.AutoInitialized
                {
                    ID = Guid.NewGuid()
                };

                repository.TestDenyUserEdit.AutoInitialized.Save(new[] { item }, null, null, checkUserPermissions: true);

                DateTime finish = SqlUtility.GetDatabaseTime(sqlExecuter);

                item = repository.TestDenyUserEdit.AutoInitialized.Load(new[] { item.ID }).Single();

                Console.WriteLine("item.Start: " + item.Start);
                Assert.IsTrue(item.Start >= start && item.Start <= finish);
            }
        }

    }
}
