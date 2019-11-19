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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using Rhetos;

namespace CommonConcepts.Test
{
    [TestClass]
    public class AllowSaveValidationsTest
    {
        [TestMethod]
        public void ValidData()
        {
            using (var container = new RhetosTestContainer())
            {
                var simple = container.Resolve<Common.DomRepository>().TestAllowSave.Simple;
                var s1 = new TestAllowSave.Simple { Code = 1, Name = "a", CodeAS = 2, NameAS = "b" };
                simple.Insert(s1);
                Assert.AreEqual("1a2b", simple.Query(new[] { s1.ID }).Select(s => s.Code + s.Name + s.CodeAS + s.NameAS).Single());
            }
        }

        [TestMethod]
        public void InvalidInteger()
        {
            using (var container = new RhetosTestContainer())
            {
                var simple = container.Resolve<Common.DomRepository>().TestAllowSave.Simple;
                var s1 = new TestAllowSave.Simple { Code = null, Name = "a", CodeAS = 2, NameAS = "b" };
                TestUtility.ShouldFail<UserException>(
                    () => simple.Insert(s1),
                    "It is not allowed to enter TestAllowSave.Simple because the required property Code is not set.",
                    "SystemMessage: DataStructure:TestAllowSave.Simple,ID:", ",Property:Code");
            }
        }

        [TestMethod]
        public void InvalidString()
        {
            using (var container = new RhetosTestContainer())
            {
                var simple = container.Resolve<Common.DomRepository>().TestAllowSave.Simple;
                var s1 = new TestAllowSave.Simple { Code = 1, Name = null, CodeAS = 2, NameAS = "b" };
                TestUtility.ShouldFail<UserException>(
                    () => simple.Insert(s1),
                    "It is not allowed to enter TestAllowSave.Simple because the required property Name is not set.",
                    "SystemMessage: DataStructure:TestAllowSave.Simple,ID:", ",Property:Name");
            }
            using (var container = new RhetosTestContainer())
            {
                var simple = container.Resolve<Common.DomRepository>().TestAllowSave.Simple;
                var s1 = new TestAllowSave.Simple { Code = 1, Name = "", CodeAS = 2, NameAS = "b" };
                TestUtility.ShouldFail<UserException>(
                    () => simple.Insert(s1),
                    "It is not allowed to enter TestAllowSave.Simple because the required property Name is not set.",
                    "SystemMessage: DataStructure:TestAllowSave.Simple,ID:", ",Property:Name");
            }
        }

        [TestMethod]
        public void AllowSave()
        {
            foreach (bool useDatabaseNullSemantics in new[] { false, true })
                using (var container = new RhetosTestContainer())
                {
                    container.SetUseDatabaseNullSemantics(useDatabaseNullSemantics);
                    var simple = container.Resolve<Common.DomRepository>().TestAllowSave.Simple;
                    var s1 = new TestAllowSave.Simple { Code = 1, Name = "a", CodeAS = null, NameAS = null };
                    var s2 = new TestAllowSave.Simple { Code = 1, Name = "aaaaa", CodeAS = 2, NameAS = "b" };

                    simple.Insert(s1, s2);
                    Assert.AreEqual("1a, 1aaaaa2b", TestUtility.DumpSorted(simple.Load(new[] { s1.ID, s2.ID }), s => s.Code + s.Name + s.CodeAS + s.NameAS));
                    Assert.AreEqual(0, simple.Validate(new[] { s1.ID, s2.ID }, onSave: true).Count(), "There should be no invalid items with errors that are checked on save.");

                    // Errors that are NOT checked on save:
                    var errors = simple.Validate(new[] { s1.ID, s2.ID }, onSave: false);
                    Assert.AreEqual(
                        TestUtility.DumpSorted(new[] {
                            "The required property CodeAS is not set./CodeAS/" + s1.ID,
                            "The required property NameAS is not set./NameAS/" + s1.ID,
                            "[Test] Longer than 3.//" + s2.ID,
                            "[Test] Longer than 4./Name/" + s2.ID }),
                        TestUtility.DumpSorted(errors,
                            error => string.Format(error.Message, error.MessageParameters) + "/" + error.Property + "/" + error.ID));
                }
        }

        [TestMethod]
        public void AllowSaveEmptyString()
        {
            using (var container = new RhetosTestContainer())
            {
                var simple = container.Resolve<Common.DomRepository>().TestAllowSave.Simple;
                var s1 = new TestAllowSave.Simple { Code = 1, Name = "a", CodeAS = 2, NameAS = "" };

                simple.Insert(s1);
                Assert.AreEqual("1a2", simple.Query(new[] { s1.ID }).Select(s => s.Code + s.Name + s.CodeAS + s.NameAS).Single());
                Assert.AreEqual(0, simple.Validate(new[] { s1.ID }, onSave: true).Count(), "There should be no invalid items with errors that are checked on save.");

                // Errors that are NOT checked on save:
                var errors = simple.Validate(new[] { s1.ID }, onSave: false);
                Assert.AreEqual(
                    "The required property NameAS is not set./NameAS",
                    TestUtility.DumpSorted(errors, error => string.Format(error.Message, error.MessageParameters) + "/" + error.Property));
                Assert.IsTrue(errors.All(e => e.ID == s1.ID));
            }
        }

        [TestMethod]
        public void InvalidDataReporting()
        {
            using (var container = new RhetosTestContainer())
            {
                var simpleSystem = container.Resolve<Common.DomRepository>().TestAllowSave.SimpleSystem;
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                Console.WriteLine(id1);
                Console.WriteLine(id2);
                sqlExecuter.ExecuteSql("INSERT INTO TestAllowSave.SimpleSystem (ID) SELECT " + SqlUtility.QuoteGuid(id1));
                sqlExecuter.ExecuteSql("INSERT INTO TestAllowSave.SimpleSystem (ID, Code, Name, CodeAS, NameAS) SELECT " + SqlUtility.QuoteGuid(id2) + ", 1, '', 2, ''");

                {
                    var errorsOnSave = simpleSystem.Validate(new[] { id1, id2 }, onSave: true);
                    Assert.AreEqual(
                        TestUtility.DumpSorted(new[] {
                            "System required property ShortString TestAllowSave.SimpleSystem.Name is not set./Name/" + id1,
                            "System required property Integer TestAllowSave.SimpleSystem.Code is not set./Code/" + id1 }),
                        TestUtility.DumpSorted(errorsOnSave,
                            error => string.Format(error.Message, error.MessageParameters) + "/" + error.Property + "/" + error.ID));
                }
                {
                    var errorsAllowSave = simpleSystem.Validate(new[] { id1, id2 }, onSave: false);
                    Assert.AreEqual(
                        TestUtility.DumpSorted(new[] {
                            "The required property CodeAS is not set./CodeAS/" + id1,
                            "The required property NameAS is not set./NameAS/" + id1,
                            "The required property NameAS is not set./NameAS/" + id2 }),
                        TestUtility.DumpSorted(errorsAllowSave,
                            error => string.Format(error.Message, error.MessageParameters) + "/" + error.Property + "/" + error.ID));
                }
            }
        }
    }
}
