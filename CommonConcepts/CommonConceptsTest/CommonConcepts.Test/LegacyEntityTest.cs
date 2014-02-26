﻿/*
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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class LegacyEntityTest
    {
        private static readonly Guid GuidA = Guid.NewGuid();
        private static readonly Guid GuidB = Guid.NewGuid();
        private static void InitializeData(Common.ExecutionContext executionContext)
        {
            executionContext.SqlExecuter.ExecuteSql(new[]
                {
                    "DELETE FROM Test13.Old2;",
                    "DELETE FROM Test13.Old1;",
                    "INSERT INTO Test13.Old1 (ID, IDOld1, Name) SELECT '" + GuidA + "', 11, 'a';",
                    "INSERT INTO Test13.Old1 (ID, IDOld1, Name) SELECT '" + GuidB + "', 12, 'b';",
                    "INSERT INTO Test13.Old2 (ID, IDOld2, Name, Old1ID, Same) SELECT NEWID(), 21, 'ax', 11, 'sx'",
                    "INSERT INTO Test13.Old2 (ID, IDOld2, Name, Old1ID, Same) SELECT NEWID(), 22, 'ay', 11, 'sy'"
                });
        }

        static string ReportLegacy1(Common.ExecutionContext executionContext, Common.DomRepository domRepository)
        {
            executionContext.NHibernateSession.Flush();
            executionContext.NHibernateSession.Clear();

            var loaded = domRepository.Test13.Legacy1.Query().Select(l1 => l1.Name);
            return string.Join(", ", loaded.OrderBy(x => x));
        }

        static string ReportLegacy2(Common.ExecutionContext executionContext, Common.DomRepository domRepository)
        {
            executionContext.NHibernateSession.Flush();
            executionContext.NHibernateSession.Clear();

            var loaded = domRepository.Test13.Legacy2.Query().Select(l2 => l2.Leg1.Name + " " + l2.NameNew + " " + l2.Same);
            return string.Join(", ", loaded.OrderBy(x => x));
        }

        [TestMethod]
        public void Query()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                InitializeData(executionContext);

                Assert.AreEqual("a, b", ReportLegacy1(executionContext, repository));

                Assert.AreEqual("a ax sx, a ay sy", ReportLegacy2(executionContext, repository));
            }
        }

        [TestMethod]
        public void WritableWithUpdateableView()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                InitializeData(executionContext);
                Assert.AreEqual("a, b", ReportLegacy1(executionContext, repository), "initial");

                repository.Test13.Legacy1.Insert(new[] { new Test13.Legacy1 { Name = "c" } });
                Assert.AreEqual("a, b, c", ReportLegacy1(executionContext, repository), "insert");

                var updated = repository.Test13.Legacy1.Query().Where(item => item.Name == "a").Single();
                executionContext.NHibernateSession.Evict(updated);
                updated.Name = "ax";
                repository.Test13.Legacy1.Update(new[] { updated });
                Assert.AreEqual("ax, b, c", ReportLegacy1(executionContext, repository), "update");

                var deleted = repository.Test13.Legacy1.Query().Where(item => item.Name == "b").Single();
                repository.Test13.Legacy1.Delete(new[] { deleted });
                Assert.AreEqual("ax, c", ReportLegacy1(executionContext, repository), "delete");
            }
        }

        [TestMethod]
        public void WritableWithInsteadOfTrigger()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                InitializeData(executionContext);
                Assert.AreEqual("a ax sx, a ay sy", ReportLegacy2(executionContext, repository), "initial");

                repository.Test13.Legacy2.Insert(new[] { new Test13.Legacy2 { NameNew = "bnew", Leg1 = new Test13.Legacy1 { ID = GuidB }, Same = "snew" } });
                Assert.AreEqual("a ax sx, a ay sy, b bnew snew", ReportLegacy2(executionContext, repository), "insert");

                var updated = repository.Test13.Legacy2.Query().Where(item => item.NameNew == "ax").Single();
                executionContext.NHibernateSession.Evict(updated);
                updated.NameNew += "2";
                updated.Leg1 = new Test13.Legacy1 { ID = GuidB };
                updated.Same += "2";
                repository.Test13.Legacy2.Update(new[] { updated });
                Assert.AreEqual("a ay sy, b ax2 sx2, b bnew snew", ReportLegacy2(executionContext, repository), "update");

                repository.Test13.Legacy2.Delete(repository.Test13.Legacy2.Query().Where(item => item.NameNew == "ay"));
                Assert.AreEqual("b ax2 sx2, b bnew snew", ReportLegacy2(executionContext, repository), "insert");
            }
        }

        [TestMethod]
        public void ReadOnlyProperty()
        {
            try
            {
                using (var executionContext = new CommonTestExecutionContext())
                {
                    var repository = new Common.DomRepository(executionContext);
                    executionContext.SqlExecuter.ExecuteSql(new[]
                {
                    "DELETE FROM Test13.Old3;",
                    "ALTER TABLE Test13.Old3 DROP COLUMN Num;",
                    "ALTER TABLE Test13.Old3 ADD Num INTEGER IDENTITY(123, 1);",
                    "INSERT INTO Test13.Old3 (ID, Text) SELECT NEWID(), 'abc'"
                });

                    var leg = repository.Test13.Legacy3.Query().Single();
                    Assert.AreEqual(123, leg.NumNew);
                    leg.TextNew = leg.TextNew + "x";
                    leg.NumNew = leg.NumNew + 100;

                    try
                    {
                        repository.Test13.Legacy3.Update(new[] { leg });
                        executionContext.NHibernateSession.Flush();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    executionContext.NHibernateSession.Clear();
                    leg = repository.Test13.Legacy3.Query().Single();
                    Assert.AreEqual(123, leg.NumNew);

                    Console.WriteLine(leg.TextNew);
                }
            }
            finally
            {
                using (var executionContext = new CommonTestExecutionContext())
                    executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM Test13.Old3;",
                        "ALTER TABLE Test13.Old3 DROP COLUMN Num;",
                        "ALTER TABLE Test13.Old3 ADD Num INTEGER;"
                    });
            }
        }

        [TestMethod]
        public void Filter()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM Test13.Old3;",
                        "INSERT INTO Test13.Old3 (Num, Text) SELECT 10, 'a'",
                        "INSERT INTO Test13.Old3 (Num, Text) SELECT 20, 'a'",
                        "INSERT INTO Test13.Old3 (Num, Text) SELECT 30, 'a'",
                        "INSERT INTO Test13.Old3 (Num, Text) SELECT 110, 'a2'",
                        "INSERT INTO Test13.Old3 (Num, Text) SELECT 120, 'a3'",
                        "INSERT INTO Test13.Old3 (Num, Text) SELECT 130, 'a4'"
                    });

                var filtered = repository.Test13.Legacy3.Filter(new Test13.PatternFilter { Pattern = "2" });
                Assert.AreEqual("110, 20", TestUtility.DumpSorted(filtered, item => item.NumNew.ToString()));
            }
        }

        [TestMethod]
        public void MultipleKeyColumns()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var c1id = Guid.NewGuid();
                var c2id = Guid.NewGuid();
                var p1id = Guid.NewGuid();
                var p2id = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[]
                {
                    "DELETE FROM Test13.OldMultiChild",
                    "DELETE FROM Test13.OldMultiParent",
                    "INSERT INTO Test13.OldMultiParent (ID, Key1, Key2, Name) SELECT '"+p1id+"', 123, 'abc', 'Parent123abc'",
                    "INSERT INTO Test13.OldMultiParent (ID, Key1, Key2, Name) SELECT '"+p2id+"', 456, 'def', 'Parent456def'",
                    "INSERT INTO Test13.OldMultiChild (ID, ParentKey1, ParentKey2, Name) SELECT '"+c1id+"', 123, 'abc', 'Child123abc'",
                    "INSERT INTO Test13.OldMultiChild (ID, ParentKey1, ParentKey2, Name) SELECT '"+c2id+"', 456, 'def', 'Child456def'",
                });

                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual(
                    "Child123abc-Parent123abc, Child456def-Parent456def",
                    TestUtility.DumpSorted(repository.Test13.LegacyMultiChild.Query(),
                        child => child.Name + "-" + child.Parent.Name));

                var c1 = repository.Test13.LegacyMultiChild.Filter(new[] { c1id }).Single();
                c1.ParentID = p2id;
                repository.Test13.LegacyMultiChild.Update(new[] { c1 });

                executionContext.NHibernateSession.Clear();
                Assert.AreEqual(
                    "Child123abc-Parent456def, Child456def-Parent456def",
                    TestUtility.DumpSorted(repository.Test13.LegacyMultiChild.Query(),
                        child => child.Name + "-" + child.Parent.Name));
            }
        }
    }
}
