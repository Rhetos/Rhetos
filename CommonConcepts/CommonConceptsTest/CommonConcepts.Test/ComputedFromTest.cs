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
    public class ComputedFromTest
    {
        private static string Dump(TestComputedFrom.PersistAll item) { return item.Name + " " + item.Code; }
        private static string Dump(TestComputedFrom.PersistPartial item) { return item.Name; }
        private static string Dump(TestComputedFrom.PersistCustom item) { return item.NamePersist + " " + item.Code; }
        private static string Dump(TestComputedFrom.PersistComplex item) { return item.Name + " " + item.Name2 + " " + item.Code; }
        private static string Dump(TestComputedFrom.MultiSync item) { return item.Base.Name1 + " " + item.Name1a + " " + item.Name1bx + " " + item.Name2a; }


        [TestMethod]
        public void PersistAll()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistAll" });
                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump));

                repository.TestComputedFrom.PersistAll.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("aa 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump), "recompute all with SaveFilter to sync only Code 11 ");

                repository.TestComputedFrom.PersistAll.RecomputeFromSource();
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("aa 11, bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump), "recompute all");

                repository.TestComputedFrom.PersistAll.Delete(repository.TestComputedFrom.PersistAll.All());
                executionContext.NHibernateSession.Clear();

                repository.TestComputedFrom.PersistAll.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.All().Where(item => item.Code == 22).Single().ID });
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump), "recompute by ID (code 22)");
            }
        }

        [TestMethod]
        public void PersistPartial()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistPartial" });
                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump));

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Name == "aa"));
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("aa", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump), "recompute all with SaveFilter to sync only Name aa ");

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource();
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("aa, bb", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump), "recompute all");

                repository.TestComputedFrom.PersistPartial.Delete(repository.TestComputedFrom.PersistPartial.All());
                executionContext.NHibernateSession.Clear();

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.All().Where(item => item.Name == "bb").Single().ID });
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("bb", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump), "recompute by ID (name bb)");
            }
        }

        [TestMethod]
        public void PersistCustom()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistCustom" });
                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump));

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("aa 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump), "recompute all with SaveFilter to sync only Code 11 ");

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource();
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("aa 11, bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump), "recompute all");

                repository.TestComputedFrom.PersistCustom.Delete(repository.TestComputedFrom.PersistCustom.All());
                executionContext.NHibernateSession.Clear();

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.All().Where(item => item.Code == 22).Single().ID });
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump), "recompute by ID (code 22)");
            }
        }

        [TestMethod]
        public void PersistComplex()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistComplex" });
                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.All(), Dump));

                repository.TestComputedFrom.PersistComplex.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("aa  11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.All(), Dump), "recompute Code 11 from Source");

                repository.TestComputedFrom.PersistComplex.RecomputeFromSource2();
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual(" dd , aa cc 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.All(), Dump), "recompute from Source2");
            }
        }

        private static string Dump(DateTime? dateTime)
        {
            return dateTime.HasValue ? dateTime.Value.ToString("s") : "null";
        }

        private static void AssertIsRecently(DateTime? time, DateTime now, bool positiveLogic = true)
        {
            string msg = "Time " + Dump(time.Value) + " should " + (positiveLogic ? "" : "NOT ") + "be recent to " + Dump(now) + ".";
            Console.WriteLine(msg);
            Assert.AreEqual(positiveLogic, time.Value >= now.AddSeconds(-10) && time.Value <= now.AddSeconds(1), msg);
        }

        [TestMethod]
        public void MultiSync_InsertBase()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var deleteTables = new[] { "MultiSync", "Base1", "Base2" };
                executionContext.SqlExecuter.ExecuteSql(deleteTables.Select(t => "DELETE FROM TestComputedFrom." + t));
                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.All(), Dump));

                var b = new TestComputedFrom.Base1 { ID = Guid.NewGuid(), Name1 = "b1" };
                repository.TestComputedFrom.Base1.Insert(new[] { b });
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("b1 b1a b1b ", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.All(), Dump));

                var ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter));
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter));

                ms.Start = new DateTime(2001, 2, 3);
                ms.LastModifiedName1bx = new DateTime(2001, 2, 3);
                repository.TestComputedFrom.MultiSync.Update(new[] { ms });
                executionContext.NHibernateSession.Clear();
                ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter), false);

                b.Info = "xxx";
                repository.TestComputedFrom.Base1.Update(new[] { b });
                executionContext.NHibernateSession.Clear();
                ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter), false);

                b.Name1 = "b1new";
                repository.TestComputedFrom.Base1.Update(new[] { b });
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("b1new b1newa b1newb ", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.All(), Dump));
                ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(executionContext.SqlExecuter));
            }
        }
    }
}
