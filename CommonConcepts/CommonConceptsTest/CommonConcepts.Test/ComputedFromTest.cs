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
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using System.Text.RegularExpressions;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ComputedFromTest
    {
        private static string Dump(TestComputedFrom.PersistAll item) { return item.Name + " " + item.Code; }
        private static string Dump(TestComputedFrom.PersistPartial item) { return item.Name; }
        private static string Dump(TestComputedFrom.PersistCustom item) { return item.NamePersist + " " + item.Code; }
        private static string Dump(TestComputedFrom.PersistComplex item) { return item.Name + " " + item.Name2 + " " + item.Code; }
        private static string Dump(TestComputedFrom.PersistOverlap item) { return item.Name + " " + item.Code; }
        private static string Dump(Common.Queryable.TestComputedFrom_MultiSync item) { return item.Base.Name1 + " " + item.Name1a + " " + item.Name1bx + " " + item.Name2a; }


        [TestMethod]
        public void PersistAll()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistAll" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.Load(), Dump));

                repository.TestComputedFrom.PersistAll.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                Assert.AreEqual("aa 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.Load(), Dump), "recompute all with SaveFilter to sync only Code 11 ");

                repository.TestComputedFrom.PersistAll.RecomputeFromSource();
                Assert.AreEqual("aa 11, bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.Load(), Dump), "recompute all");

                repository.TestComputedFrom.PersistAll.Delete(repository.TestComputedFrom.PersistAll.Load());

                repository.TestComputedFrom.PersistAll.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.Load().Where(item => item.Code == 22).Single().ID });
                Assert.AreEqual("bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistAll.Load(), Dump), "recompute by ID (code 22)");
            }
        }

        [TestMethod]
        public void PersistPartial()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistPartial" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.Load(), Dump));

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Name == "aa"));
                Assert.AreEqual("aa", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.Load(), Dump), "recompute all with SaveFilter to sync only Name aa ");

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource();
                Assert.AreEqual("aa, bb", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.Load(), Dump), "recompute all");

                repository.TestComputedFrom.PersistPartial.Delete(repository.TestComputedFrom.PersistPartial.Load());

                repository.TestComputedFrom.PersistPartial.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.Load().Where(item => item.Name == "bb").Single().ID });
                Assert.AreEqual("bb", TestUtility.DumpSorted(repository.TestComputedFrom.PersistPartial.Load(), Dump), "recompute by ID (name bb)");
            }
        }

        [TestMethod]
        public void PersistCustom()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistCustom" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.Load(), Dump));

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource(new FilterAll(), items => items.Where(item => item.Code == 11));
                Assert.AreEqual("aa 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.Load(), Dump), "recompute all with SaveFilter to sync only Code 11 ");

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource();
                Assert.AreEqual("aa 11, bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.Load(), Dump), "recompute all");

                repository.TestComputedFrom.PersistCustom.Delete(repository.TestComputedFrom.PersistCustom.Load());

                repository.TestComputedFrom.PersistCustom.RecomputeFromSource(new[] { repository.TestComputedFrom.Source.Load().Where(item => item.Code == 22).Single().ID });
                Assert.AreEqual("bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.PersistCustom.Load(), Dump), "recompute by ID (code 22)");
            }
        }

        [TestMethod]
        public void PersistComplex()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistComplex" });
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.Load(), Dump));

                repository.TestComputedFrom.PersistComplex.RecomputeFromSource(new Rhetos.Dom.DefaultConcepts.FilterAll(), items => items.Where(item => item.Code == 11));
                Assert.AreEqual("aa  11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.Load(), Dump), "recompute Code 11 from Source");

                repository.TestComputedFrom.PersistComplex.RecomputeFromSource2();
                Assert.AreEqual(" dd , aa cc 11", TestUtility.DumpSorted(repository.TestComputedFrom.PersistComplex.Load(), Dump), "recompute from Source2");
            }
        }

        [TestMethod]
        public void PersistOverlap()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestComputedFrom.PersistOverlap" });
                var testComputedFrom = container.Resolve<Common.DomRepository>().TestComputedFrom;

                Assert.AreEqual("", TestUtility.DumpSorted(testComputedFrom.PersistOverlap.Load(), Dump));

                testComputedFrom.PersistOverlap.RecomputeFromSource();
                Assert.AreEqual("aa 11, bb 22", TestUtility.DumpSorted(testComputedFrom.PersistOverlap.Load(), Dump));

                testComputedFrom.PersistOverlap.RecomputeFromSource2();
                Assert.AreEqual("cc 11, dd 22", TestUtility.DumpSorted(testComputedFrom.PersistOverlap.Load(), Dump));

                testComputedFrom.PersistOverlap.RecomputeFromSource(new[] { new Guid("16BB8451-BC22-4B4E-888E-9B5DD2355A61") });
                Assert.AreEqual("aa 11, dd 22", TestUtility.DumpSorted(testComputedFrom.PersistOverlap.Load(), Dump));
            }
        }

        private static void AssertIsRecently(DateTime? time, DateTime now, bool positiveLogic = true)
        {
            string msg = "Time " + time.Dump() + " should " + (positiveLogic ? "" : "NOT ") + "be recent to " + now.Dump() + ".";
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

                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.Query(), Dump));

                var b = new TestComputedFrom.Base1 { ID = Guid.NewGuid(), Name1 = "b1" };
                repository.TestComputedFrom.Base1.Insert(new[] { b });
                Assert.AreEqual("b1 b1a b1b ", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.Query().ToList(), Dump));

                var ms = repository.TestComputedFrom.MultiSync.Load().Single();
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()));
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()));

                ms.Start = new DateTime(2001, 2, 3);
                ms.LastModifiedName1bx = new DateTime(2001, 2, 3);
                repository.TestComputedFrom.MultiSync.Update(new[] { ms });
                ms = repository.TestComputedFrom.MultiSync.Load().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);

                b.Info = "xxx";
                repository.TestComputedFrom.Base1.Update(new[] { b });
                ms = repository.TestComputedFrom.MultiSync.Load().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);

                b.Name1 = "b1new";
                repository.TestComputedFrom.Base1.Update(new[] { b });
                Assert.AreEqual("b1new b1newa b1newb ", TestUtility.DumpSorted(repository.TestComputedFrom.MultiSync.Query().ToList(), Dump));
                ms = repository.TestComputedFrom.MultiSync.Load().Single();
                AssertIsRecently(ms.Start, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()), false);
                AssertIsRecently(ms.LastModifiedName1bx, SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>()));
            }
        }

        [TestMethod]
        public void ComputedWithAutocode()
        {
            using (var container = new RhetosTestContainer())
            {
                var computedRepos = container.Resolve<GenericRepository<TestComputedFrom.ComputedWithAutoCode>>();
                var computedSourceRepos = container.Resolve<GenericRepository<TestComputedFrom.ComputedWithAutoCodeSource>>();

                var item1 = new TestComputedFrom.ComputedWithAutoCode { ID = Guid.NewGuid(), Code = "+" };
                computedRepos.Save(new[] { item1 }, null, computedRepos.Load());

                Assert.AreEqual("1 abc", TestUtility.DumpSorted(computedRepos.Load(), item => item.Code + " " + item.Comp));
            }
        }

        //==========================================================================================

        [TestMethod]
        public void KeyProperty()
        {
            TestKeyPropertyTarget<TestComputedFrom.SyncByKeyTarget>(
                (item, controlValue) => item.Control = controlValue,
                item => item.Name1 + item.Name2 + item.Data + item.Control);

            TestKeyPropertyTarget<TestComputedFrom.SyncByKeyTarget2>(
                (item, controlValue) => item.Control = controlValue,
                item => item.Name1x + item.Name2x + item.Datax + item.Control);

            TestKeyPropertyTarget<TestComputedFrom.SyncByKeyTarget3>(
                (item, controlValue) => item.Control = controlValue,
                item => item.Name1 + item.Name2 + item.Data + item.Control);
        }

        private void TestKeyPropertyTarget<TEntity>(Action<TEntity, string> setControlValue, Func<TEntity, string> report)
            where TEntity : class, IEntity
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var sourceRepository = context.Repository.TestComputedFrom.SyncByKeySource;
                var targetRepository = context.GenericRepository<TEntity>();

                // Clean:
                sourceRepository.Delete(sourceRepository.Load());
                targetRepository.Delete(targetRepository.Load());
                Assert.AreEqual("", TestUtility.DumpSorted(targetRepository.Load(), report));

                // Simple insert:
                var s1a = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "a", Data = "1a", ID = Guid.NewGuid() };
                var s1b = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "b", Data = "1b", ID = Guid.NewGuid() };
                var s1c = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "c", Data = "1c", ID = Guid.NewGuid() };
                var s1d = new TestComputedFrom.SyncByKeySource { Name1 = 1, Name2 = "d", Data = "1d", ID = Guid.NewGuid() };
                var s2a = new TestComputedFrom.SyncByKeySource { Name1 = 2, Name2 = "a", Data = "2a", ID = Guid.NewGuid() };
                sourceRepository.Insert(new[] { s1a, s1b, s1c, s1d, s2a });
                Assert.AreEqual("1a1a, 1b1b, 1c1c, 1d1d, 2a2a", TestUtility.DumpSorted(targetRepository.Load(), report));

                // Update the Control property:
                // Recomputing target data should not affect the Control property because it is not included in the ComputedFrom.
                foreach (var item in targetRepository.Load())
                {
                    setControlValue(item, "x");
                    targetRepository.Update(item);
                }
                Assert.AreEqual("1a1ax, 1b1bx, 1c1cx, 1d1dx, 2a2ax", TestUtility.DumpSorted(targetRepository.Load(), report));

                // Miscellaneous changes (insert, update, delete):
                var s2b = new TestComputedFrom.SyncByKeySource { Name1 = 2, Name2 = "b", Data = "2b", ID = Guid.NewGuid() };
                s1b.Name2 = "B"; // same key - expected for computed value to be updated
                s1c.Name2 = "cc"; // modified key - expected for computed value to delete 1c, insert 1cc (no control 'x')
                s2a.Data = "2aa";
                sourceRepository.Save(new[] { s2b }, new[] { s1b, s1c, s2a }, new[] { s1d });

                // Expected: insert 2b, 1cc; delete 1c, 1d; update 1B, 2a.
                Assert.AreEqual("1a1ax, 1B1bx, 1cc1c, 2a2aax, 2b2b", TestUtility.DumpSorted(targetRepository.Load(), report));
            }
        }

        [TestMethod]
        public void ChangesOnReferenced()
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log);

                var repository = container.Resolve<Common.DomRepository>();
                var test = repository.TestChangesOnReferenced;
                test.Tested.Delete(test.Tested.Query());
                test.Parent.Delete(test.Parent.Query());
                test.ImplementationSimple.Delete(test.ImplementationSimple.Query());
                test.ImplementationComplex.Delete(test.ImplementationComplex.Query());

                Assert.AreEqual("", TestUtility.DumpSorted(test.TestedInfo.Query(), item => item.Info));

                var p1 = new TestChangesOnReferenced.Parent { Name = "p1" };
                var p2 = new TestChangesOnReferenced.Parent { Name = "p2" };
                test.Parent.Insert(p1, p2);

                var s1 = new TestChangesOnReferenced.ImplementationSimple { Name = "s1" };
                test.ImplementationSimple.Insert(s1);

                var c1 = new TestChangesOnReferenced.ImplementationComplex { Name2 = "c1" };
                test.ImplementationComplex.Insert(c1);
                Guid c1AlternativeId = test.Poly.Query(item => item.ImplementationComplexAlternativeNameID == c1.ID).Select(item => item.ID).Single();

                var t1a = new TestChangesOnReferenced.Tested { Name = "t1a", ParentID = p1.ID, PolyID = s1.ID };
                var t1b = new TestChangesOnReferenced.Tested { Name = "t1b", ParentID = p1.ID, PolyID = s1.ID };
                var t2 = new TestChangesOnReferenced.Tested { Name = "t2", ParentID = p2.ID, PolyID = c1AlternativeId };
                test.Tested.Insert(t1a, t1b, t2);

                Assert.AreEqual("t1a-p1-s1, t1b-p1-s1, t2-p2-c1", TestUtility.DumpSorted(test.TestedInfo.Query(), item => item.Info));

                log.Clear();
                t1a = test.Tested.Load(new[] { t1a.ID }).Single();
                t1a.Name += "X";
                test.Tested.Update(t1a);
                Assert.AreEqual("t1aX-p1-s1, t1b-p1-s1, t2-p2-c1", TestUtility.DumpSorted(test.TestedInfo.Query(), item => item.Info));
                Assert.AreEqual("TestedInfo 1n 1o - 0i 1u 0d", TestUtility.DumpSorted(FindRecomputes(log)));

                log.Clear();
                p1 = test.Parent.Load(new[] { p1.ID }).Single();
                p1.Name += "X";
                test.Parent.Update(p1);
                Assert.AreEqual("t1aX-p1X-s1, t1b-p1X-s1, t2-p2-c1", TestUtility.DumpSorted(test.TestedInfo.Query(), item => item.Info));
                Assert.AreEqual("TestedInfo 2n 2o - 0i 2u 0d", TestUtility.DumpSorted(FindRecomputes(log)));

                log.Clear();
                s1 = test.ImplementationSimple.Load(new[] { s1.ID }).Single();
                s1.Name += "X";
                test.ImplementationSimple.Update(s1);
                Assert.AreEqual("t1aX-p1X-s1X, t1b-p1X-s1X, t2-p2-c1", TestUtility.DumpSorted(test.TestedInfo.Query(), item => item.Info));
                Assert.AreEqual("Poly_Materialized 1n 1o - 0i 0u 0d, TestedInfo 2n 2o - 0i 2u 0d", TestUtility.DumpSorted(FindRecomputes(log)));

                log.Clear();
                c1 = test.ImplementationComplex.Load(new[] { c1.ID }).Single();
                c1.Name2 += "X";
                test.ImplementationComplex.Update(c1);
                Assert.AreEqual("t1aX-p1X-s1X, t1b-p1X-s1X, t2-p2-c1X", TestUtility.DumpSorted(test.TestedInfo.Query(), item => item.Info));
                Assert.AreEqual("Poly_Materialized 1n 1o - 0i 0u 0d, TestedInfo 1n 1o - 0i 1u 0d", TestUtility.DumpSorted(FindRecomputes(log)));
            }
        }

        private static readonly Regex FindRecomputesRegex = new Regex(@"GenericRepository\((?<module>.+)\.(?<entity>.+)\)\.InsertOrUpdateOrDelete: Diff \((?<new>.+) new items, (?<old>.+) old items, (?<ins>.+) to insert, (?<upd>.+) to update, (?<del>.+) to delete\)");

        private List<string> FindRecomputes(List<string> log)
        {
            return log.Select(s => FindRecomputesRegex.Match(s))
                .Where(m => m.Success)
                .Select(m => m.Groups)
                .Select(g => g["entity"].Value + " " + g["new"] + "n " + g["old"] + "o - " + g["ins"] + "i " + g["upd"] + "u " + g["del"] + "d")
                .ToList();
        }

        [TestMethod]
        public void KeepSyncRepositoryMembers()
        {
            using (var container = new RhetosTestContainer())
            {
                container.AddFakeUser("bb");

                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestComputedFrom.KeepSyncRepositoryMembers");
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestComputedFrom.KeepSyncRepositoryMembers.RecomputeFromSource();
                Assert.AreEqual("bb 22", TestUtility.DumpSorted(repository.TestComputedFrom.KeepSyncRepositoryMembers.Query(), item => item.Name + " " + item.Code));
            }
        }
    }
}
