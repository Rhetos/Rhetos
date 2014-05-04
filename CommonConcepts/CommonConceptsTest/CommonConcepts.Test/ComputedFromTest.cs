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
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;

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
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistAll" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump));

                repository.TestComputedFrom.PersistAll.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("aa 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump), "recompute all with SaveFilter to sync only Code 11 ");

                repository.TestComputedFrom.PersistAll.RecomputeFromSource();
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("aa 11, bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump), "recompute all");

                repository.TestComputedFrom.PersistAll.Delete(repository.TestComputedFrom.PersistAll.All());
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

                repository.TestComputedFrom.PersistAll.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.All().Where(item => item.Code == 22).Single().ID });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.All(), Dump), "recompute by ID (code 22)");
            }
        }

        [TestMethod]
        public void PersistPartial()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistPartial" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump));

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Name == "aa"));
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("aa", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump), "recompute all with SaveFilter to sync only Name aa ");

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource();
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("aa, bb", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump), "recompute all");

                repository.TestComputedFrom.PersistPartial.Delete(repository.TestComputedFrom.PersistPartial.All());
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.All().Where(item => item.Name == "bb").Single().ID });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("bb", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.All(), Dump), "recompute by ID (name bb)");
            }
        }

        [TestMethod]
        public void PersistCustom()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistCustom" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump));

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("aa 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump), "recompute all with SaveFilter to sync only Code 11 ");

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource();
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("aa 11, bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump), "recompute all");

                repository.TestComputedFrom.PersistCustom.Delete(repository.TestComputedFrom.PersistCustom.All());
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.All().Where(item => item.Code == 22).Single().ID });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.All(), Dump), "recompute by ID (code 22)");
            }
        }

        [TestMethod]
        public void PersistComplex()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistComplex" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.All(), Dump));

                repository.TestComputedFrom.PersistComplex.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("aa  11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.All(), Dump), "recompute Code 11 from Source");

                repository.TestComputedFrom.PersistComplex.RecomputeFromSource2();
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
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
            using (var container = new RhetosTestContainer())
            {
                var deleteTables = new[] { "MultiSync", "Base1", "Base2" };
                container.Resolve<ISqlExecuter>().ExecuteSql(deleteTables.Select(t => "DELETE FROM TestComputedFrom." + t));
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.All(), Dump));

                var b = new TestComputedFrom.Base1 { ID = Guid.NewGuid(), Name1 = "b1" };
                repository.TestComputedFrom.Base1.Insert(new[] { b });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("b1 b1a b1b ", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.All(), Dump));

                var ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()));
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()));

                ms.Start = new DateTime(2001, 2, 3);
                ms.LastModifiedName1bx = new DateTime(2001, 2, 3);
                repository.TestComputedFrom.MultiSync.Update(new[] { ms });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);

                b.Info = "xxx";
                repository.TestComputedFrom.Base1.Update(new[] { b });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);

                b.Name1 = "b1new";
                repository.TestComputedFrom.Base1.Update(new[] { b });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("b1new b1newa b1newb ", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.All(), Dump));
                ms = repository.TestComputedFrom.MultiSync.All().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()));
            }
        }

        //==========================================================================================

        [TestMethod]
        public void KeyProperties()
        {
            using (var container = new RhetosTestContainer())
            {
                var deleteTables = new[] { "SyncByKeySource", "SyncByKeyTarget" };
                container.Resolve<ISqlExecuter>().ExecuteSql(deleteTables.Select(t => "DELETE FROM TestComputedFrom." + t));
                var repository = container.Resolve<Common.DomRepository>();

                var s1a = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "a", Data = "1a", ID = Guid.NewGuid() };
                var s1b = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "b", Data = "1b", ID = Guid.NewGuid() };
                var s1c = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "c", Data = "1c", ID = Guid.NewGuid() };
                var s1d = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "d", Data = "1d", ID = Guid.NewGuid() };
                var s2a = new TestComputedFrom.SyncByKeySource { Name1 = 2, Name2 = "a", Data = "2a", ID = Guid.NewGuid() };
                repository.TestComputedFrom.SyncByKeySource.Insert(new[] { s1a, s1b, s1c, s1d, s2a });

                Assert.AreEqual("1a1a, 1b1b, 1c1c, 1d1d, 2a2a", TestUtility.DumpSorted(
                    repository.TestComputedFrom.SyncByKeyTarget.All(),
                    item => item.Name1 + item.Name2 + item.Data + item.Control));

                {
                    var targetItems = repository.TestComputedFrom.SyncByKeyTarget.All();
                    foreach (var item in targetItems)
                        item.Control = "x";
                    repository.TestComputedFrom.SyncByKeyTarget.Update(targetItems);
                }

                Assert.AreEqual("1a1ax, 1b1bx, 1c1cx, 1d1dx, 2a2ax", TestUtility.DumpSorted(
                    repository.TestComputedFrom.SyncByKeyTarget.All(),
                    item => item.Name1 + item.Name2 + item.Data + item.Control));

                var s2b = new TestComputedFrom.SyncByKeySource { Name1 = 2, Name2 = "b", Data = "2b", ID = Guid.NewGuid() };
                s1b.Name2 = "B"; // same key - expected for computed value to be updated
                s1c.Name2 = "cc"; // modified key - expected for computed value to delete 1c, insert 1cc (no control 'x')
                s2a.Data = "2aa";
                repository.TestComputedFrom.SyncByKeySource.Save(new[] { s2b }, new[] { s1b, s1c, s2a }, new[] { s1d });

                // expected: insert 2b, 1cc; delete 1c, 1d; update 1B, 2a.
                Assert.AreEqual("1a1ax, 1B1bx, 1cc1c, 2a2aax, 2b2b", TestUtility.DumpSorted(
                    repository.TestComputedFrom.SyncByKeyTarget.All(),
                    item => item.Name1 + item.Name2 + item.Data + item.Control));
            }
        }

        [TestMethod]
        public void KeyProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                var deleteTables = new[] { "SyncByKeySource", "SyncByKeyTarget2" };
                container.Resolve<ISqlExecuter>().ExecuteSql(deleteTables.Select(t => "DELETE FROM TestComputedFrom." + t));
                var repository = container.Resolve<Common.DomRepository>();

                var s1a = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "a", Data = "1a", ID = Guid.NewGuid() };
                var s1b = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "b", Data = "1b", ID = Guid.NewGuid() };
                var s1c = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "c", Data = "1c", ID = Guid.NewGuid() };
                var s1d = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "d", Data = "1d", ID = Guid.NewGuid() };
                var s2a = new TestComputedFrom.SyncByKeySource { Name1 = 2, Name2 = "a", Data = "2a", ID = Guid.NewGuid() };
                repository.TestComputedFrom.SyncByKeySource.Insert(new[] { s1a, s1b, s1c, s1d, s2a });

                Assert.AreEqual("1a1a, 1b1b, 1c1c, 1d1d, 2a2a", TestUtility.DumpSorted(
                    repository.TestComputedFrom.SyncByKeyTarget2.All(),
                    item => item.Name1x + item.Name2x + item.Datax + item.Control));

                {
                    var targetItems = repository.TestComputedFrom.SyncByKeyTarget2.All();
                    foreach (var item in targetItems)
                        item.Control = "x";
                    repository.TestComputedFrom.SyncByKeyTarget2.Update(targetItems);
                }

                Assert.AreEqual("1a1ax, 1b1bx, 1c1cx, 1d1dx, 2a2ax", TestUtility.DumpSorted(
                    repository.TestComputedFrom.SyncByKeyTarget2.All(),
                    item => item.Name1x + item.Name2x + item.Datax + item.Control));

                var s2b = new TestComputedFrom.SyncByKeySource { Name1 = 2, Name2 = "b", Data = "2b", ID = Guid.NewGuid() };
                s1b.Name2 = "B"; // same key - expected for computed value to be updated
                s1c.Name2 = "cc"; // modified key - expected for computed value to delete 1c, insert 1cc (no control 'x')
                s2a.Data = "2aa";
                repository.TestComputedFrom.SyncByKeySource.Save(new[] { s2b }, new[] { s1b, s1c, s2a }, new[] { s1d });

                // expected: insert 2b, 1cc; delete 1c, 1d; update 1B, 2a.
                Assert.AreEqual("1a1ax, 1B1bx, 1cc1c, 2a2aax, 2b2b", TestUtility.DumpSorted(
                    repository.TestComputedFrom.SyncByKeyTarget2.All(),
                    item => item.Name1x + item.Name2x + item.Datax + item.Control));
            }
        }
    }
}
