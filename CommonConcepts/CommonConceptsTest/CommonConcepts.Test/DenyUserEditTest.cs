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
using Rhetos.TestCommon;
using Rhetos.Utilities;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DenyUserEditTest
    {
        private string DumpSimple(Common.DomRepository repository)
        {
            return TestUtility.DumpSorted(
                repository.TestDenyUserEdit.Simple.All(),
                item => (item.Editable ?? "null")
                    + " " + (item.NonEditable ?? "null")
                    + " " + (item.NonEditableReference != null ? item.NonEditableReference.Name : "null"));
        }

        [TestMethod]
        public void EditableProperty()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = new Common.DomRepository(executionContext);

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
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = new Common.DomRepository(executionContext);

                var simple = new TestDenyUserEdit.Simple { Editable = "a", NonEditable = "x" };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(new[] { simple }, null, null, true),
                    "insert noneditable",
                    "Simple", "NonEditable", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditablePropertyUpdate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var simpleID = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent",
                        "INSERT INTO TestDenyUserEdit.Simple (ID, Editable, NonEditable) VALUES (" + SqlUtility.QuoteGuid(simpleID) + ", 'a', 'x')"
                    });

                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual("a x null", DumpSimple(repository));

                var simple = new TestDenyUserEdit.Simple { ID = simpleID, Editable = "b", NonEditable = "x" };
                repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true);
                Assert.AreEqual("b x null", DumpSimple(repository));

                simple.NonEditable = "y";
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true),
                    "update noneditable",
                    "Simple", "NonEditable", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditablePropertyUpdateToNull()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var simpleID = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent",
                        "INSERT INTO TestDenyUserEdit.Simple (ID, Editable, NonEditable) VALUES (" + SqlUtility.QuoteGuid(simpleID) + ", 'a', 'x')"
                    });

                var repository = new Common.DomRepository(executionContext);

                var simple = new TestDenyUserEdit.Simple { ID = simpleID, Editable = "a", NonEditable = null };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true),
                    "update noneditable",
                    "Simple", "NonEditable", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditablePropertyUpdateFromNull()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var simpleID = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent",
                        "INSERT INTO TestDenyUserEdit.Simple (ID, Editable, NonEditable) VALUES (" + SqlUtility.QuoteGuid(simpleID) + ", 'a', NULL)"
                    });

                var repository = new Common.DomRepository(executionContext);

                var simple = new TestDenyUserEdit.Simple { ID = simpleID, Editable = "a", NonEditable = "x" };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(null, new[] { simple }, null, true),
                    "update noneditable",
                    "Simple", "NonEditable", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditableRefereceInsert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = new Common.DomRepository(executionContext);

                var parent = new TestDenyUserEdit.Parent { Name = "p" };
                repository.TestDenyUserEdit.Parent.Save(new[] { parent }, null, null, true);

                var simple = new TestDenyUserEdit.Simple { Editable = "a", NonEditableReference = parent };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(new[] { simple }, null, null, true),
                    "insert noneditable reference",
                    "Simple", "NonEditableReference", "not allowed");
            }
        }

        [TestMethod]
        public void NonEditableRefereceInsertGuid()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestDenyUserEdit.Simple",
                        "DELETE FROM TestDenyUserEdit.Parent"
                    });
                var repository = new Common.DomRepository(executionContext);

                var parent = new TestDenyUserEdit.Parent { Name = "p" };
                repository.TestDenyUserEdit.Parent.Save(new[] { parent }, null, null, true);

                var simple = new TestDenyUserEdit.Simple { Editable = "a", NonEditableReferenceID = parent.ID };
                TestUtility.ShouldFail(() => repository.TestDenyUserEdit.Simple.Save(new[] { simple }, null, null, true),
                    "insert noneditable reference",
                    "Simple", "NonEditableReference", "not allowed");
            }
        }

        [TestMethod]
        public void HardcodedEntity_Insert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                TestUtility.ShouldFail(
                    () => repository.TestDenyUserEdit.Hardcoded.Save(new[] { new TestDenyUserEdit.Hardcoded { Name = "abc" } }, null, null, true),
                    "", "It is not allowed to directly modify TestDenyUserEdit.Hardcoded.");
            }
        }

        [TestMethod]
        public void HardcodedEntity_Delete()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestDenyUserEdit.Hardcoded",
                    "INSERT INTO TestDenyUserEdit.Hardcoded (Name) VALUES ('abc')" });
                var repository = new Common.DomRepository(executionContext);

                var item = repository.TestDenyUserEdit.Hardcoded.All().Single();
                Assert.AreEqual("abc", item.Name);
                TestUtility.ShouldFail(
                    () => repository.TestDenyUserEdit.Hardcoded.Save(null, null, new[] { item }, true),
                    "", "It is not allowed to directly modify TestDenyUserEdit.Hardcoded.");
            }
        }

        [TestMethod]
        public void HardcodedEntity_Update()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestDenyUserEdit.Hardcoded",
                    "INSERT INTO TestDenyUserEdit.Hardcoded (Name) VALUES ('abc')" });
                var repository = new Common.DomRepository(executionContext);

                var item = repository.TestDenyUserEdit.Hardcoded.All().Single();
                Assert.AreEqual("abc", item.Name);
                item.Name += "x";
                TestUtility.ShouldFail(
                    () => repository.TestDenyUserEdit.Hardcoded.Save(null, new[] { item }, null, true),
                    "", "It is not allowed to directly modify TestDenyUserEdit.Hardcoded.");
            }
        }
    }
}
