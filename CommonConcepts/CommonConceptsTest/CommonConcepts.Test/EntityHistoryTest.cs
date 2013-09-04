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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using NHibernate;
using NHibernate.Transform;
using System.Diagnostics;

namespace CommonConcepts.Test
{
    [TestClass]
    public class EntityHistoryTest
    {
        //======================================================================
        // (SIMPLE) BASIC FUNCTIONALITY:

        private static string Dump(DateTime? dateTime)
        {
            return dateTime.HasValue ? dateTime.Value.ToString("s") : "null";
        }

        private static string Dump(IEnumerable<TestHistory.Simple> items)
        {
            return TestUtility.DumpSorted(items, item => item.Code + " " + item.Name);
        }

        private static string Dump(IEnumerable<TestHistory.BasicAutocode_Changes> items)
        {
            return TestUtility.DumpSorted(items, item => item.Code + " " + item.Name);
        }

        private static string Dump(IEnumerable<TestHistory.BasicAutocode> items)
        {
            return TestUtility.DumpSorted(items, item => item.Code + " " + item.Name);
        }

        private static string DumpFull(IEnumerable<TestHistory.Simple> items)
        {
            return TestUtility.DumpSorted(items, item => item.Code + " " + item.Name + " " + Dump(item.ActiveSince));
        }

        private static string Dump(IEnumerable<TestHistory.Simple_Changes> items)
        {
            return TestUtility.DumpSorted(items, item => item.Code);
        }

        private static string DumpFull(IEnumerable<TestHistory.Simple_Changes> items)
        {
            return TestUtility.DumpSorted(items, item => item.Code + " " + Dump(item.ActiveSince));
        }

        private static string Dump(IEnumerable<TestHistory.Standard> items)
        {
            return TestUtility.DumpSorted(items, item => item.Code + " " + item.Name + " " + Dump(item.Birthday));
        }

        private static DateTime Day(int d)
        {
            return new DateTime(2001, 1, 1).AddDays(d-1);
        }

        private static DateTime GetServerTime(CommonTestExecutionContext executionContext)
        {
            var serverTime = MsSqlUtility.GetDatabaseTime(executionContext.SqlExecuter);
            Console.WriteLine("Server time: " + serverTime.ToString("s") + ". Local time: " + DateTime.Now.ToString("s") + ".");
            return serverTime;
        }

        private static void AssertIsRecently(DateTime? time, DateTime now)
        {
            string msg = "Time " + Dump(time.Value) + " should be recent to " + Dump(now) + ".";
            Console.WriteLine(msg);
            Assert.IsTrue(time.Value <= now.AddSeconds(1), msg);
            Assert.IsTrue(time.Value >= now.AddSeconds(-10), msg);
        }

        [TestMethod]
        public void MinimalHistoryInsert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Minimal" });
                var repository = new Common.DomRepository(executionContext);

                Assert.AreEqual(0, repository.TestHistory.Minimal.All().Count());
                Assert.AreEqual(0, repository.TestHistory.Minimal_Changes.All().Count());

                var m1 = new TestHistory.Minimal { Code = 1 };
                var m2 = new TestHistory.Minimal { Code = 2 };
                repository.TestHistory.Minimal.Insert(new[] { m1, m2 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                Assert.AreEqual("1, 2", TestUtility.DumpSorted(repository.TestHistory.Minimal.All(), item => item.Code.ToString()));
                Assert.AreEqual(0, repository.TestHistory.Minimal_Changes.All().Count());
                Assert.AreEqual(2, repository.TestHistory.Minimal_History.All().Count());
                foreach (var item in repository.TestHistory.Minimal.All())
                    AssertIsRecently(item.ActiveSince, now);
            }
        }

        [TestMethod]
        public void MinimalHistoryUpdate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Minimal",
                    "INSERT INTO TestHistory.Minimal (ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Minimal.All().Single();
                m1.ActiveSince = null;
                m1.Code = 11;
                repository.TestHistory.Minimal.Update(new[] { m1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var h = repository.TestHistory.Minimal_Changes.All().Single();

                Assert.AreEqual(11, h.Entity.Code);
                AssertIsRecently(h.Entity.ActiveSince, now);
                AssertIsRecently(h.ActiveSince, Day(1));
            }
        }

        [TestMethod]
        public void ActiveUntilCheck()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Minimal",
                    "INSERT INTO TestHistory.Minimal (ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Minimal.All().Single();
                m1.ActiveSince = null;
                m1.Code = 11;
                repository.TestHistory.Minimal.Update(new[] { m1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var h = repository.TestHistory.Minimal_History.All().OrderBy(t => t.ActiveSince).FirstOrDefault();
                var m = repository.TestHistory.Minimal.All().Single();

                Assert.AreEqual(h.ActiveUntil, m.ActiveSince);
            }
        }

        [TestMethod]
        public void ActiveUntilEditingCurrentVersionActiveFromCheck()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Minimal",
                    "INSERT INTO TestHistory.Minimal (ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, '2012-01-01')",
                    "INSERT INTO TestHistory.Minimal_Changes (ID, EntityID, ActiveSince) VALUES ('"+Guid.NewGuid().ToString()+"'," + SqlUtility.QuoteGuid(id1) + ", '2000-01-01')"                    
                });
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Minimal.All().Single();
                m1.ActiveSince = new DateTime(2012, 12, 25);
                repository.TestHistory.Minimal.Update(new[] { m1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var h = repository.TestHistory.Minimal_History.All().OrderBy(t => t.ActiveSince).ToList();
                var m = repository.TestHistory.Minimal.All().Single();

                Assert.AreEqual(h[0].ActiveUntil, h[1].ActiveSince);
                Assert.AreEqual(h[1].ActiveUntil, m.ActiveSince);
            }
        }

        [TestMethod]
        public void ActiveUntilAsExtensionInHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Minimal",
                    "INSERT INTO TestHistory.Minimal (ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Minimal.All().Single();
                m1.ActiveSince = null;
                m1.Code = 11;
                repository.TestHistory.Minimal.Update(new[] { m1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var hist = repository.TestHistory.Minimal_Changes.Query().Where(item => item.Entity == m1).Single();
                var m = repository.TestHistory.Minimal.All().Single();

                Assert.AreEqual(hist.Extension_Minimal_ChangesActiveUntil.ActiveUntil, m.ActiveSince);
            }
        }

        [TestMethod]
        public void ActiveUntilInHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Minimal",
                    "INSERT INTO TestHistory.Minimal (ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Minimal.All().Single();
                m1.ActiveSince = null;
                m1.Code = 11;
                repository.TestHistory.Minimal.Update(new[] { m1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fullh = repository.TestHistory.Minimal_History.Query().Where(item => item.Entity == m1).OrderBy(item => item.ActiveSince).Select(item => item).ToList();

                Assert.AreEqual(fullh[0].ActiveUntil, fullh[1].ActiveSince);
                Assert.IsNull(fullh[1].ActiveUntil);
            }
        }

        [TestMethod]
        public void HistoryWithDenySave()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Standard" });
                var repository = new Common.DomRepository(executionContext);
                var standardRepos = repository.TestHistory.Standard;

                var e = new TestHistory.Standard { Code = 1, Name = "a", Birthday = new DateTime(2001, 2, 3, 4, 5, 6) };
                standardRepos.Insert(new[] { e });
                DateTime t1 = GetServerTime(executionContext);
                string v1 = "1 a 2001-02-03T04:05:06";
                Assert.AreEqual(v1, Dump(standardRepos.All()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                e.Code = 2;
                e.Name = "baaaaaaaaaaaaaaaaaaaa";
                e.ActiveSince = null;
                e.Birthday = new DateTime(2011, 12, 13, 14, 15, 16);
                TestUtility.ShouldFail(() => standardRepos.Update(new[] { e }), "Name TooLong deny save.");
            }
        }

        [TestMethod]
        public void HistoryWithReference()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                Guid c1ID = Guid.NewGuid();
                Guid c2ID = Guid.NewGuid();
                Guid rcID = Guid.NewGuid();

                executionContext.SqlExecuter.ExecuteSql(new[] { 
                    "DELETE FROM TestHistory.ReferenceClean_Changes;",
                    "DELETE FROM TestHistory.ReferenceClean;",
                    "DELETE FROM TestHistory.Clean;",
                    "INSERT INTO TestHistory.Clean (ID, Name) VALUES ('" + c1ID.ToString() + "', 'c1');",
                    "INSERT INTO TestHistory.Clean (ID, Name) VALUES ('" + c2ID.ToString() + "', 'c1');",
                    "INSERT INTO TestHistory.ReferenceClean (ID, AddName, CleanID) VALUES ('" + rcID.ToString() + "', 'rc','" + c1ID.ToString() + "');"
                });
                var repository = new Common.DomRepository(executionContext);
                var cleanRepos = repository.TestHistory.Clean;

                var c1 = repository.TestHistory.Clean.Query().Where(item => item.ID == c1ID).SingleOrDefault();
                var c2 = repository.TestHistory.Clean.Query().Where(item => item.ID == c2ID).SingleOrDefault();
                
                var refCleanRepos = repository.TestHistory.ReferenceClean;
                var rc = repository.TestHistory.ReferenceClean.Query().Where(item => item.ID == rcID).SingleOrDefault();

                rc.Clean = c2;
                refCleanRepos.Update(new[] { rc });

                TestUtility.ShouldFail(() => cleanRepos.Delete(new[] { c1 }), "History still references that object.");
            }
        }

        [TestMethod]
        public void HistoryWithAutocode()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                Guid c1ID = Guid.NewGuid();
                Guid c2ID = Guid.NewGuid();
                Guid rcID = Guid.NewGuid();

                executionContext.SqlExecuter.ExecuteSql(new[] { 
                    "DELETE FROM TestHistory.BasicAutocode_Changes;",
                    "DELETE FROM TestHistory.BasicAutocode;",
                    "INSERT INTO TestHistory.BasicAutocode (ID, Name, Code) VALUES ('" + c1ID.ToString() + "', 'c1', '+');",
                    "INSERT INTO TestHistory.BasicAutocode (ID, Name, Code) VALUES ('" + c2ID.ToString() + "', 'c2', '+');",
                });
                var repository = new Common.DomRepository(executionContext);
                var cleanRepos = repository.TestHistory.BasicAutocode;

                var c1 = repository.TestHistory.BasicAutocode.Query().Where(item => item.ID == c1ID).SingleOrDefault();
                var c2 = repository.TestHistory.BasicAutocode.Query().Where(item => item.ID == c2ID).SingleOrDefault();
                c1.Name = "C1New";
                c2.Name = "C2New";

                cleanRepos.Update(new[] { c1, c2 });
                
                string v1 = "1 c1, 2 c2";
                Assert.AreEqual(v1, Dump(repository.TestHistory.BasicAutocode_Changes.All()));
            }
        }

        [TestMethod]
        public void HistoryWithUnique()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                Guid c1ID = Guid.NewGuid();
                Guid c2ID = Guid.NewGuid();
                Guid c3ID = Guid.NewGuid();

                executionContext.SqlExecuter.ExecuteSql(new[] { 
                    "DELETE FROM TestHistory.BasicUnique_Changes;",
                    "DELETE FROM TestHistory.BasicUnique;",
                    "INSERT INTO TestHistory.BasicUnique (ID, Name, ActiveSince) VALUES ('" + c1ID.ToString() + "', 'c1', '2013-01-01');",
                    "INSERT INTO TestHistory.BasicUnique (ID, Name, ActiveSince) VALUES ('" + c2ID.ToString() + "', 'c2', '2013-01-01');",
                    "INSERT INTO TestHistory.BasicUnique (ID, Name, ActiveSince) VALUES ('" + c3ID.ToString() + "', 'c3', '2013-01-01');",
                    "INSERT INTO TestHistory.BasicUnique_Changes (ID, EntityID, Name, ActiveSince) VALUES ('" + Guid.NewGuid().ToString() + "', '" + c1ID.ToString() + "', 'oldc1', '2012-01-01');",
                    "INSERT INTO TestHistory.BasicUnique_Changes (ID, EntityID, Name, ActiveSince) VALUES ('" + Guid.NewGuid().ToString() + "', '" + c2ID.ToString() + "', 'c3', '2012-01-01');"
                });
                var repository = new Common.DomRepository(executionContext);
                var cleanRepos = repository.TestHistory.BasicUnique;

                var c1 = repository.TestHistory.BasicUnique.Query().Where(item => item.ID == c1ID).SingleOrDefault();
                c1.Name = "oldc2";

                cleanRepos.Update(new[] { c1 });

                var c3 = repository.TestHistory.BasicUnique.Query().Where(item => item.ID == c3ID).SingleOrDefault();
                c3.Name = "newc3";

                // Current name of c3 already exists as old name of c1, but, since all that will be in history table, update should pass just fine.
                cleanRepos.Update(new[] { c3 });

                // revert to c3 name
                c3.Name = "c3";
                cleanRepos.Update(new[] { c3 });

                var c2 = repository.TestHistory.BasicUnique.Query().Where(item => item.ID == c2ID).SingleOrDefault();
                c2.Name = "c3";
                TestUtility.ShouldFail(() => cleanRepos.Update(new[] { c2 }), "Simply test basic unique funcionality on base item.");
            }
        }

        [TestMethod]
        public void MinimalHistoryUpdateInvalidActiveSince()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Minimal",
                    "INSERT INTO TestHistory.Minimal (ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, '2013-01-01')",
                    "INSERT INTO TestHistory.Minimal_Changes (ID, EntityID, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(Guid.NewGuid()) + ", " + SqlUtility.QuoteGuid(id1) + ", '2012-12-30')"});
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Minimal.All().Single();
                m1.ActiveSince = new DateTime(2012, 12, 25);
                m1.Code = 11;
                // Should fail
                TestUtility.ShouldFail(() => repository.TestHistory.Minimal.Update(new[] { m1 }), "ActiveSince set older than last history entry");
            }
        }

        [TestMethod]
        public void CrudWithExplicitTime()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);

                // Insert:
                var s = new TestHistory.Simple { ID = Guid.NewGuid(), Code = 1, ActiveSince = Day(1), Name = "a" };
                repository.TestHistory.Simple.Insert(new[] { s });
                Assert.AreEqual(1, repository.TestHistory.Simple_History.All().Count());

                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("1 a 2001-01-01T00:00:00", DumpFull(repository.TestHistory.Simple.All()));
                Assert.AreEqual("", DumpFull(repository.TestHistory.Simple_Changes.All()));

                // Update:
                s.Code = 2;
                s.Name = "b";
                s.ActiveSince = Day(2);
                repository.TestHistory.Simple.Update(new[] { s });

                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("2 b 2001-01-02T00:00:00", DumpFull(repository.TestHistory.Simple.All()));
                Assert.AreEqual("1 2001-01-01T00:00:00", DumpFull(repository.TestHistory.Simple_Changes.All()));
                Assert.AreEqual(2, repository.TestHistory.Simple_History.All().Count());

                // Another update:
                s.Code = 3;
                s.Name = "c";
                s.ActiveSince = Day(3);
                repository.TestHistory.Simple.Update(new[] { s });

                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("3 c 2001-01-03T00:00:00", DumpFull(repository.TestHistory.Simple.All()));
                Assert.AreEqual("1 2001-01-01T00:00:00, 2 2001-01-02T00:00:00", DumpFull(repository.TestHistory.Simple_Changes.All()));
                Assert.AreEqual(3, repository.TestHistory.Simple_History.All().Count());

                // Delete:
                repository.TestHistory.Simple.Delete(new[] { s });

                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("", DumpFull(repository.TestHistory.Simple.All()));
                Assert.AreEqual("", DumpFull(repository.TestHistory.Simple_Changes.All()));
            }
        }

        [TestMethod]
        public void InsertFuture()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var future = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter).AddMinutes(1);

                TestUtility.ShouldFail(
                    () => repository.TestHistory.Simple.Insert(new[] { new TestHistory.Simple { ActiveSince = future } }),
                    "insert with future time",
                    "ActiveSince", "TestHistory.Simple", "future");
            }
        }

        [TestMethod]
        public void UpdateFuture()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var s = new TestHistory.Simple { ID = Guid.NewGuid(), Code = 1 };
                repository.TestHistory.Simple.Insert(new[] { s });

                var future = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter).AddMinutes(1);
                s.ActiveSince = future;

                TestUtility.ShouldFail(
                    () => repository.TestHistory.Simple.Update(new[] { s }),
                    "insert with future time",
                    "ActiveSince", "TestHistory.Simple", "future");
            }
        }

        [TestMethod]
        public void NormalUpdateIfExistsNewerHistoryEntryDiffBase()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple_Changes",
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'Test1', '2001-01-01')",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id2) + ", 1, 'Test2', '2013-12-12')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, Code, ActiveSince, ID) VALUES (" + SqlUtility.QuoteGuid(id2) + ", 1, '2013-10-10', '"+(Guid.NewGuid()).ToString()+"')"});
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Simple.Query().Where(t => t.ID == id1).SingleOrDefault();
                m1.ActiveSince = new DateTime(2013, 5, 5);
                m1.Code = 11;
                repository.TestHistory.Simple.Update(new[] { m1 });
            }
        }

        [TestMethod]
        public void NormalUpdate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'Test', '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                var m1 = repository.TestHistory.Simple.All().Single();
                m1.ActiveSince = null;
                m1.Code = 11;
                repository.TestHistory.Simple.Update(new[] { m1 });
            }
        }

        [TestMethod]
        public void UpdateWithoutSettingActiveSince()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var inPast = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter).AddMinutes(-1);
                var s = new TestHistory.Simple { ID = Guid.NewGuid(), Code = 1, ActiveSince = inPast };
                repository.TestHistory.Simple.Insert(new[] { s });

                s.ActiveSince = null;
                s.Name = "test";

                repository.TestHistory.Simple.Update(new[] { s });
            }
        }

        /// <summary>
        /// Fast updates could cause a problem with low resolution of ActiveSince datetime property.
        /// An error could be caused by the "unique index" in history table (EntityID, ActiveSince).
        /// In addition, ORM may round the time to a second to decrease the resolution.
        /// </summary>
        [TestMethod]
        public void FastUpdateNHibernate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);
                var simpleRepos = repository.TestHistory.Simple;

                Assert.AreEqual("", DumpFull(simpleRepos.All()));

                var sw = Stopwatch.StartNew();
                var s1 = new TestHistory.Simple { Code = 1, Name = "a" };
                simpleRepos.Insert(new[] { s1 });
                Assert.AreEqual(1, simpleRepos.All().Single().Code);

                int lastCode = 1;
                var s1Array = new[] { s1 };
                const bool SlowTest = false;
                for (int i = 0; i < 5 * (SlowTest ? 20 : 1); i++)
                {
                    for (int j = 0; j < 3 * (SlowTest ? 20 : 1); j++)
                    {
                        s1.ActiveSince = null;
                        s1.Code = ++lastCode;
                        simpleRepos.Update(s1Array);
                    }
                    Assert.AreEqual(lastCode, simpleRepos.All().Single().Code);
                }
                sw.Stop();

                var h = repository.TestHistory.Simple_Changes.Query().OrderBy(item => item.ActiveSince).ToArray();
                Console.WriteLine(DumpFull(h));
                string msg = string.Format(
                    "Number of history records ({0}) is expected to be around the number of elapsed seconds ({1}).",
                    h.Count(), sw.Elapsed.TotalSeconds);
                Console.WriteLine(msg);
                Assert.IsTrue(sw.Elapsed.TotalSeconds-1 < h.Count() && h.Count() < sw.Elapsed.TotalSeconds+1, msg);
            }
        }

        [TestMethod]
        public void HistoryAtTime()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Standard" });
                var repository = new Common.DomRepository(executionContext);
                var standardRepos = repository.TestHistory.Standard;

                var e = new TestHistory.Standard { Code = 1, Name = "a", Birthday = new DateTime(2001, 2, 3, 4, 5, 6) };
                standardRepos.Insert(new[] { e });
                DateTime t1 = GetServerTime(executionContext);
                string v1 = "1 a 2001-02-03T04:05:06";
                Assert.AreEqual(v1, Dump(standardRepos.All()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

                e.Code = 2;
                e.Name = "b";
                e.ActiveSince = null; // TODO: Should we change the behaveour to automatically reset ActiveSince if the developer does not use it.
                e.Birthday = new DateTime(2011, 12, 13, 14, 15, 16);
                standardRepos.Update(new[] { e });
                DateTime t2 = GetServerTime(executionContext);
                string v2 = "2 b 2011-12-13T14:15:16";
                Assert.AreEqual(v2, Dump(standardRepos.All()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

                e.Code = 3;
                e.Name = "c";
                e.ActiveSince = null; // TODO: Should we change the behaveour to automatically reset ActiveSince if the developer does not use it.
                standardRepos.Update(new[] { e });
                DateTime t3 = GetServerTime(executionContext);
                string v3 = "3 c 2011-12-13T14:15:16";
                Assert.AreEqual(v3, Dump(standardRepos.All()));

                executionContext.NHibernateSession.Clear(); // TODO: Turn off NHibernate caching to have a more robust reading without Clear(). The problem is disabling caching but keeping the reference evaluation enabled (Entity Framework?).
                Assert.AreEqual(v1, Dump(standardRepos.Filter(t1)), "At time 1");
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual(v2, Dump(standardRepos.Filter(t2)), "At time 2");
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual(v3, Dump(standardRepos.Filter(t3)), "At time 3");
            }
        }

        [TestMethod]
        public void PartialHistoryAtTime()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);
                var er = repository.TestHistory.Simple;
                var hr = repository.TestHistory.Simple_Changes;

                var e = new TestHistory.Simple { Code = 1, Name = "a" };
                er.Insert(new[] { e });
                DateTime t1 = GetServerTime(executionContext);
                Assert.AreEqual("1 a", Dump(er.All()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

                e.Code = 2;
                e.Name = "b";
                e.ActiveSince = null; // TODO: Should we change the behaveour to automatically reset ActiveSince if the developer does not use it.
                er.Update(new[] { e });
                DateTime t2 = GetServerTime(executionContext);
                Assert.AreEqual("2 b", Dump(er.All()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

                e.Code = 3;
                e.Name = "c";
                e.ActiveSince = null; // TODO: Should we change the behaveour to automatically reset ActiveSince if the developer does not use it.
                er.Update(new[] { e });
                DateTime t3 = GetServerTime(executionContext);
                Assert.AreEqual("3 c", Dump(er.All()));

                executionContext.NHibernateSession.Clear(); // TODO: Turn off NHibernate caching to have a more robust reading without Clear(). The problem is disabling caching but keeping the reference evaluation enabled (Entity Framework?).
                Assert.AreEqual("1", Dump(hr.Filter(t1)), "At time 1");
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("2", Dump(hr.Filter(t2)), "At time 2");
                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("3", Dump(hr.Filter(t3)), "At time 3");
            }
        }

        [TestMethod]
        public void HistoryBeforeFirstRecord()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Standard" });
                var repository = new Common.DomRepository(executionContext);
                var er = repository.TestHistory.Standard;

                var e = new TestHistory.Standard { Code = 1, Name = "a", Birthday = new DateTime(2001, 2, 3, 4, 5, 6) };
                er.Insert(new[] { e });
                const string v1 = "1 a 2001-02-03T04:05:06";
                DateTime t1 = GetServerTime(executionContext);
                Assert.AreEqual(v1, Dump(er.All()));
                Assert.AreEqual(v1, Dump(er.Filter(t1)));

                TestHistory.Standard[] loaded = er.Filter(t1.AddSeconds(-5));
                Assert.AreEqual(0, loaded.Length);
            }
        }

//        [TestMethod]
//        public void ManualHistoryManagement()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple_Base" });
//                var repository = new Common.DomRepository(executionContext);

//                var id = Guid.NewGuid();
//                var hBase = new TestHistory.Simple_Base { ID = id };
//                repository.TestHistory.Simple_Base.Insert(new [] { hBase });

//                var t1 = DateTime.Today.Add(new TimeSpan(1, 2, 3));
//                var t2 = t1.AddSeconds(1);
//                var h1 = new TestHistory.Simple_Changes { ActiveSince = t1, Code = 1, Name = "a", Birthday = new DateTime(2001, 2, 3, 4, 5, 6), Base = hBase };
//                var h2 = new TestHistory.Simple_Changes { ActiveSince = t2, Code = 2, Name = "b", Birthday = new DateTime(2002, 2, 3, 4, 5, 6), Base = hBase };
//                repository.TestHistory.Simple_Changes.Insert(new [] { h1, h2 });

//                const string v1 = "1 a 2001-02-03T04:05:06";
//                const string v2 = "2 b 2002-02-03T04:05:06";
//                executionContext.NHibernateSession.Clear(); // TODO: Turn off NHibernate caching to have a more robust reading without Clear(). The problem is disabling caching but keeping the reference evaluation enabled (Entity Framework?).
//                Assert.AreEqual(v1, DumpSorted(repository.TestHistory.Simple.Filter(t1)));
//                executionContext.NHibernateSession.Clear();
//                Assert.AreEqual(v2, DumpSorted(repository.TestHistory.Simple.Filter(t2)));

//                executionContext.NHibernateSession.Clear();
//                Assert.AreEqual(v2, DumpSorted(repository.TestHistory.Simple.All()));

//                executionContext.NHibernateSession.Clear();
//                Assert.AreEqual("", DumpSorted(repository.TestHistory.Simple.Filter(t1.AddSeconds(-1))));
//            }
//        }

        [TestMethod]
        public void InsertHistoryFuture()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var e = new TestHistory.Simple { Code = 1 };
                repository.TestHistory.Simple.Insert(new[] { e });

                var future = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter).AddMinutes(1);

                TestUtility.ShouldFail(
                    () => repository.TestHistory.Simple_Changes.Insert(new[] { new TestHistory.Simple_Changes { Entity = e, ActiveSince = future } }),
                    "insert with future time",
                    "ActiveSince", "TestHistory.Simple_Changes", "future");
            }
        }

        [TestMethod]
        public void UpdateHistoryFuture()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var s = new TestHistory.Simple { ID = Guid.NewGuid(), Code = 1, ActiveSince = Day(2) };
                repository.TestHistory.Simple.Insert(new[] { s });

                var h = new TestHistory.Simple_Changes { EntityID = s.ID, Code = 2, ActiveSince = Day(1) };
                repository.TestHistory.Simple_Changes.Insert(new[] { h });

                var future = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter).AddMinutes(1);
                h.ActiveSince = future;

                TestUtility.ShouldFail(
                    () => repository.TestHistory.Simple_Changes.Update(new[] { h }),
                    "insert with future time",
                    "ActiveSince", "TestHistory.Simple_Changes", "future");
            }
        }

//        //======================================================================
//        // (COMPLEX) INTERACTIONS WITH OTHER CONCEPTS:

//        [TestMethod]
//        public void RequiredBase()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var other = new TestHistory.Other { ID = Guid.NewGuid() };
//                repository.TestHistory.Other.Insert(new[] { other });

//                executionContext.NHibernateSession.Clear();
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex.Insert(new[] { 
//                    new TestHistory.Complex { Name = null, Code = "1", Other = other }}),
//                    "Required property", "required property", "Name");
//            }
//        }

//        [TestMethod]
//        public void RequiredHistory()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var other = new TestHistory.Other { ID = Guid.NewGuid() };
//                repository.TestHistory.Other.Insert(new[] { other });

//                var complexBase = new TestHistory.Complex_Base { ID = Guid.NewGuid() };
//                repository.TestHistory.Complex_Base.Insert(new[] { complexBase });

//                executionContext.NHibernateSession.Clear();
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex_Changes.Insert(new[] {
//                    new TestHistory.Complex_Changes { Name = null, Code = "1", Other = other,
//                        Base = complexBase, ActiveSince = DateTime.Now.AddDays(-1) }}),
//                    "Required property", "required property", "Name");
//            }
//        }

//        [TestMethod]
//        public void Reference()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var other = new TestHistory.Other { ID = Guid.NewGuid() };
//                repository.TestHistory.Other.Insert(new[] { other });

//                var complex = new TestHistory.Complex { ID = Guid.NewGuid(), Name = "a", Code = "1", Other = other };
//                repository.TestHistory.Complex.Insert(new[] { complex });

//                var sub = new TestHistory.Sub { Complex = complex };
//                repository.TestHistory.Sub.Insert(new[] { sub });

//                executionContext.NHibernateSession.Clear();
//                var query = repository.TestHistory.Sub.Query().Select(item => item.ID + " " + item.Complex.Name + " " + item.Complex.Other.ID).Single();
//                Console.WriteLine(query);
//                Assert.AreEqual(sub.ID + " a " + other.ID, query);
//            }
//        }

//        [TestMethod]
//        public void SqlIndex()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                var sql = @"SELECT
//	i.name, c.name, ic.*
//FROM
//	sys.indexes i
//	INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
//	INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
//WHERE
//	i.object_id = OBJECT_ID('TestHistory.Complex_Changes')
//ORDER BY
//	i.name, ic.key_ordinal";

//                var expected =
//@"IX_Complex_Changes_Base	BaseID
//IX_Complex_Changes_Base_ActiveSince	BaseID
//IX_Complex_Changes_Base_ActiveSince	ActiveSince
//IX_Complex_Changes_Name	Name
//IX_Complex_Changes_Other	OtherID
//IX_Complex_Changes_Parent_Code	ParentID
//IX_Complex_Changes_Parent_Code	Code
//PK_Complex_Changes	ID
//";
//                var actual = new StringBuilder();
//                executionContext.SqlExecuter.ExecuteReader(sql, reader => actual.AppendLine(reader.GetString(0) + "\t" + reader.GetString(1)));

//                Assert.AreEqual(expected, actual.ToString());
//            }
//        }

//        [TestMethod]
//        public void UniqueBase()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var other = new TestHistory.Other { ID = Guid.NewGuid() };
//                repository.TestHistory.Other.Insert(new[] { other });

//                var c1 = new TestHistory.Complex { Name = "abc", Code = "1", Other = other };
//                var c2 = new TestHistory.Complex { Name = "abc", Code = "2", Other = other };
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex.Insert(new[] { c1, c2 }),
//                    "Unique fail on multiple insert", "duplicate record", "Name", "abc");
//            }
//        }

//        [TestMethod]
//        public void UniqueHistory()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var other = new TestHistory.Other { ID = Guid.NewGuid() };
//                repository.TestHistory.Other.Insert(new[] { other });

//                executionContext.NHibernateSession.Clear();
//                var b1 = new TestHistory.Complex_Base { ID = Guid.NewGuid() };
//                var b2 = new TestHistory.Complex_Base { ID = Guid.NewGuid() };
//                repository.TestHistory.Complex_Base.Insert(new[] { b1, b2 });

//                executionContext.NHibernateSession.Clear();
//                var h1 = new TestHistory.Complex_Changes { Name = "abc", Code = "1", Other = other, BaseID = b1.ID, ActiveSince = Day(10) };
//                var h1b = new TestHistory.Complex_Changes { Name = "abc", Code = "1", Other = other, BaseID = b1.ID, ActiveSince = Day(11) };
//                repository.TestHistory.Complex_Changes.Insert(new[] { h1, h1b });

//                executionContext.NHibernateSession.Clear();
//                var h2 = new TestHistory.Complex_Changes { Name = "abc", Code = "2", Other = other, BaseID = b2.ID, ActiveSince = Day(1) };
//                var h2b = new TestHistory.Complex_Changes { Name = "abcx", Code = "2", Other = other, BaseID = b2.ID, ActiveSince = Day(2) };
//                repository.TestHistory.Complex_Changes.Insert(new[] { h2, h2b });

//                executionContext.NHibernateSession.Clear();
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex_Changes.Delete(new[] { h2b }),
//                    "Deleting history made the older record active", "duplicate record", "Name", "abc");
//            }
//        }

//        [TestMethod]
//        public void UniqueMultiple()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var other = new TestHistory.Other { ID = Guid.NewGuid() };
//                repository.TestHistory.Other.Insert(new[] { other });

//                var complex1a = new TestHistory.Complex { ID = Guid.NewGuid(), Name = "complex1a", Code = "1", Other = other, Parent = null };
//                var complex1b = new TestHistory.Complex { ID = Guid.NewGuid(), Name = "complex1b", Code = "1", Other = other, Parent = null };
//                var complex2a = new TestHistory.Complex { ID = Guid.NewGuid(), Name = "complex2a", Code = "2", Other = other, Parent = complex1a };
//                var complex2b = new TestHistory.Complex { ID = Guid.NewGuid(), Name = "complex2b", Code = "2", Other = other, Parent = complex1a };
//                repository.TestHistory.Complex.Insert(new[] { complex1a, complex2a });

//                executionContext.NHibernateSession.Clear();
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex.Insert(new[] { complex2b }),
//                    "Inserting duplicate value.",
//                    "not allowed", "duplicate record",
//                    "Parent", complex2b.Parent.ID.ToString(),
//                    "Code", complex2b.Code.ToString());

//                executionContext.NHibernateSession.Clear();
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex.Insert(new[] { complex1b }),
//                    "Inserting duplicate value with null.",
//                    "not allowed", "duplicate record",
//                    "Parent", "<null>",
//                    "Code", complex1b.Code.ToString());
//            }
//        }

//        [TestMethod]
//        public void AutoCodeForEachBase()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                repository.TestHistory.Complex.Insert(new[] {
//                    new TestHistory.Complex { Name = "a", Code = "+" },
//                    new TestHistory.Complex { Name = "b", Code = "+" }});
//                Assert.AreEqual("1, 2", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code.ToString()));

//                executionContext.NHibernateSession.Clear();
//                var id = Guid.NewGuid();
//                repository.TestHistory.Complex.Insert(new[] {
//                    new TestHistory.Complex { ID = id, Name = "c", Code = "+" }});
//                Assert.AreEqual("1, 2, 3", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code.ToString()));

//                executionContext.NHibernateSession.Clear();
//                repository.TestHistory.Complex.Insert(new[] { new TestHistory.Complex { Name = "d", Code = "+", Other = null, ParentID = id } });
//                Assert.AreEqual("1, 1, 2, 3", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code.ToString()));

//                executionContext.NHibernateSession.Clear();
//                var b = new TestHistory.Complex_Base { ID = Guid.NewGuid() };
//                repository.TestHistory.Complex_Base.Insert(new[] { b });

//                executionContext.NHibernateSession.Clear();
//                var h = new TestHistory.Complex_Changes { Base = b, Code = "44", Name = "h", ActiveSince = DateTime.Today };
//                repository.TestHistory.Complex_Changes.Insert(new[] { h });
//                Assert.AreEqual("1, 1, 2, 3, 44", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code.ToString()));

//                executionContext.NHibernateSession.Clear();
//                var hh = repository.TestHistory.Complex.Query().Where(item => item.Name == "h").Single();
//                hh.Name = "hh";
//                repository.TestHistory.Complex.Update(new[] { hh });
//                executionContext.NHibernateSession.Clear();
//                Assert.AreEqual("1 a, 1 d, 2 b, 3 c, 44 hh", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code + " " + item.Name));
//            }
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void Detail()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var other = new TestHistory.Other { ID = Guid.NewGuid() };
//                repository.TestHistory.Other.Insert(new[] { other });
//                repository.TestHistory.Complex.Insert(new[] { new TestHistory.Complex { Name = "a", Code = "1", Other = other } });
//                Assert.AreEqual("1", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code));

//                repository.TestHistory.Other.Delete(new[] { other });
//                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code));
//            }
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void Detail2()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "DELETE FROM TestHistory.Other" });
//                var repository = new Common.DomRepository(executionContext);

//                var complex = new TestHistory.Complex { Name = "a", Code = "1" };
//                repository.TestHistory.Complex.Insert(new[] { complex });
//                var sub = new TestHistory.Sub { Complex = complex };
//                repository.TestHistory.Sub.Insert(new[] { sub });
//                Assert.AreEqual(1, repository.TestHistory.Sub.Query().Count());

//                repository.TestHistory.Complex.Delete(new[] { complex });
//                Assert.AreEqual(1, repository.TestHistory.Sub.Query().Count());
//            }
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void Hierarchy()
//        {
//            throw new NotImplementedException("");
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void PessimisticLocking()
//        {
//            throw new NotImplementedException("");
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void Logging()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestHistory.Complex_Base", "TRUNCATE TABLE Common.Log" });
//                var repository = new Common.DomRepository(executionContext);

//                var c = new TestHistory.Complex { Code = "1", Name = "a" };
//                repository.TestHistory.Complex.Insert(new[] { c });
//                Assert.AreEqual("1 a", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code + " " + item.Name));

//                executionContext.NHibernateSession.Clear();
//                var h = repository.TestHistory.Complex_Changes.All().Single();
//                h.ActiveSince = h.ActiveSince.Value.AddDays(-1);
//                repository.TestHistory.Complex_Changes.Update(new[] { h });
//                executionContext.NHibernateSession.Clear();

//                c.Name += "x";
//                repository.TestHistory.Complex.Update(new[] { c });
//                Assert.AreEqual("1 ax", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code + " " + item.Name));

//                repository.TestHistory.Complex.Delete(new[] { c });
//                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestHistory.Complex.Query(), item => item.Code + " " + item.Name));

//                Assert.AreEqual(@"Delete: <PREVIOUS />, Insert: ",
//                    GetLog(repository, "[TestHistory].[Complex_Base]"));

//                Assert.AreEqual(@"Delete: <PREVIOUS Name=""a""/>, Delete: <PREVIOUS Name=""ax""/>, Insert: , Insert: , Update: <PREVIOUS />",
//                    GetLog(repository, "[TestHistory].[Complex_Changes]"));
//            }
//        }

//        private static string GetLog(Common.DomRepository repository, string table)
//        {
//            return TestUtility.DumpSorted(
//                repository.Common.Log.Query().Where(item => item.TableName == table),
//                item => item.Action + ": " + item.Description);
//        }

//        [TestMethod]
//        public void ItemFilter()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                var ids = Enumerable.Range(1, 4).Select(x => Guid.NewGuid()).ToArray();
//                executionContext.SqlExecuter.ExecuteSql(new[]
//                    {
//                        "DELETE FROM TestHistory.Complex_Base",
//                        "INSERT INTO TestHistory.Complex_Base (ID) SELECT '"+ids[0]+"'",
//                        "INSERT INTO TestHistory.Complex_Base (ID) SELECT '"+ids[1]+"'",
//                        "INSERT INTO TestHistory.Complex_Base (ID) SELECT '"+ids[2]+"'",
//                        "INSERT INTO TestHistory.Complex_Base (ID) SELECT '"+ids[3]+"'",
//                        "INSERT INTO TestHistory.Complex_Changes (BaseID, Code, Name, ActiveSince) SELECT '"+ids[0]+"', '1', 'a', '2001-01-01'",
//                        "INSERT INTO TestHistory.Complex_Changes (BaseID, Code, Name, ActiveSince) SELECT '"+ids[1]+"', '5', 'aaaaa', '2001-01-01'",
//                        "INSERT INTO TestHistory.Complex_Changes (BaseID, Code, Name, ActiveSince) SELECT '"+ids[2]+"', '15', 'aaaaaaaaaaaaaaa', '2001-01-01'",
//                        "INSERT INTO TestHistory.Complex_Changes (BaseID, Code, Name, ActiveSince) SELECT '"+ids[3]+"', '16', 'aaaaaaaaaaaaaaaa', '2001-01-01'"
//                    });
//                var repository = new Common.DomRepository(executionContext);

//                Assert.AreEqual("15, 16", TestUtility.DumpSorted(repository.TestHistory.Complex.Filter(new TestHistory.TooLong()), item => item.Code));
//            }
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void DenySave()
//        {
//            using (var executionContext = new CommonTestExecutionContext())
//            {
//                var ids = Enumerable.Range(1, 3).Select(x => Guid.NewGuid()).ToArray();
//                executionContext.SqlExecuter.ExecuteSql(new[]
//                    {
//                        "DELETE FROM TestHistory.Complex_Base",
//                        "INSERT INTO TestHistory.Complex_Base (ID) SELECT '"+ids[0]+"'",
//                        "INSERT INTO TestHistory.Complex_Base (ID) SELECT '"+ids[1]+"'",
//                        "INSERT INTO TestHistory.Complex_Base (ID) SELECT '"+ids[2]+"'",
//                        "INSERT INTO TestHistory.Complex_Changes (BaseID, Code, Name, ActiveSince) SELECT '"+ids[0]+"', '1', 'a', '2001-01-01'",
//                        "INSERT INTO TestHistory.Complex_Changes (BaseID, Code, Name, ActiveSince) SELECT '"+ids[1]+"', '5', 'aaaaa', '2001-01-01'",
//                        "INSERT INTO TestHistory.Complex_Changes (BaseID, Code, Name, ActiveSince) SELECT '"+ids[2]+"', '15', 'aaaaaaaaaaaaaaa', '2001-01-01'",
//                    });
//                var repository = new Common.DomRepository(executionContext);

//                var c = new TestHistory.Complex { Code = "3", Name = "bbb" };
//                repository.TestHistory.Complex.Insert(new[] { c });
//                c.Name = "bbbbbbbbbbbbb";

//                executionContext.NHibernateSession.Clear();
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex.Update(new[] { c }), "Updating to invalid data", "Name too long");

//                executionContext.NHibernateSession.Clear();
//                var b = new TestHistory.Complex_Base { ID = Guid.NewGuid() };
//                repository.TestHistory.Complex_Base.Insert(new[] { b });
//                executionContext.NHibernateSession.Clear();
//                var h = new TestHistory.Complex_Changes { Base = b, Code = "12", Name = "hhhhhhhhhhhh", ActiveSince = DateTime.Today };
//                TestUtility.ShouldFail(() => repository.TestHistory.Complex_Changes.Insert(new[] { h }), "Updating to invalid data", "Name too long");

//                executionContext.NHibernateSession.Clear();
//                h.Name = "h";
//                repository.TestHistory.Complex_Changes.Insert(new[] { h });
//                executionContext.NHibernateSession.Clear();
//                Assert.AreEqual("h", repository.TestHistory.Complex.Query().Where(item => item.Code == "12").Select(item => item.Name).Single());
//            }
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void Extension()
//        {
//            throw new NotImplementedException("");
//        }

//        [TestMethod]
//        [Ignore] // Not yet implemented.
//        public void Extended()
//        {
//            throw new NotImplementedException("");
//        }

        [TestMethod]
        public void HistoryEditHistorySimple()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry in history table
                var h1 = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince).Take(1).Single();
                h1.Code = 3;
                repository.TestHistory.Simple_History.Update(new[] { h1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var h = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);

                Assert.AreEqual("3 2001-01-01T00:00:00,1 2011-01-01T00:00:00", h.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }

        [TestMethod]
        public void HistoryEditHistoryActiveSinceNewerThanActiveItem()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry in history table
                var h1 = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince).Take(1).Single();
                h1.Code = 3;
                h1.ActiveSince = new DateTime(2012, 1, 1);
                TestUtility.ShouldFail(() => repository.TestHistory.Simple_History.Update(new[] { h1 }), "History item moved to far in future", "ActiveSince of history entry is not allowed to be newer than current entry.");
            }
        }

        [TestMethod]
        public void HistoryEditHistoryChangingActiveSinceOK()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry in history table
                var h1 = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince).Take(1).Single();
                h1.Code = 3;
                h1.ActiveSince = new DateTime(2010, 1, 1);
                repository.TestHistory.Simple_History.Update(new[] { h1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var h = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);

                Assert.AreEqual("3 2010-01-01T00:00:00,1 2011-01-01T00:00:00", h.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }

        [TestMethod]
        public void HistoryUpdateActiveItemOK()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry in history table
                var a1 = repository.TestHistory.Simple_History.Query().OrderByDescending(x => x.ActiveSince).Take(1).Single();
                a1.Code = 3;
                a1.ActiveSince = new DateTime(2010, 1, 1);
                repository.TestHistory.Simple_History.Update(new[] { a1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var h = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);

                Assert.AreEqual("2 2001-01-01T00:00:00,3 2010-01-01T00:00:00", h.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }

        [TestMethod]
        public void HistoryUpdateActiveItemFailOlderThanLastInHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry in history table
                var a1 = repository.TestHistory.Simple_History.Query().OrderByDescending(x => x.ActiveSince).Take(1).Single();
                a1.Code = 3;
                a1.ActiveSince = new DateTime(2000, 1, 1);
                TestUtility.ShouldFail(() => repository.TestHistory.Simple_History.Update(new[] { a1 }), "Active item moved to far in history", "ActiveSince is not allowed to be older than last entry in history");
            }
        }


        [TestMethod]
        public void HistoryDeleteActiveItemReplaceWithHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is current
                var a1 = repository.TestHistory.Simple_History.Query().OrderByDescending(x => x.ActiveSince).Take(1).Single();
                repository.TestHistory.Simple_History.Delete(new[] { a1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, fh.Count());
                var currentItem = repository.TestHistory.Simple.All();

                Assert.AreEqual("2 a 2001-01-01T00:00:00", currentItem.Select(item => item.Code + " " + item.Name + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }

        [TestMethod]
        public void HistoryDeleteActiveItemOnlyItemInHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is current
                var a1 = repository.TestHistory.Simple_History.Query().OrderByDescending(x => x.ActiveSince).Take(1).Single();
                repository.TestHistory.Simple_History.Delete(new[] { a1 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(0, fh.Count());
            }
        }

        [TestMethod]
        public void HistoryDeleteHistoryOnlyItemInHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is only item in history table
                var a2 = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince).Take(1).Single();
                repository.TestHistory.Simple_History.Delete(new[] { a2 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, fh.Count());

                var he = repository.TestHistory.Simple_Changes.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(0, he.Count());

                var ent = repository.TestHistory.Simple.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, ent.Count());

                Assert.AreEqual("1 2011-01-01T00:00:00", fh.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }

        [TestMethod]
        public void HistoryDeleteHistoryMiddleInHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id3) + ", 3, '2000-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is in the middle of history
                var a2 = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince).Skip(1).Take(1).Single();
                repository.TestHistory.Simple_History.Delete(new[] { a2 });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(2, fh.Count());

                var he = repository.TestHistory.Simple_Changes.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, he.Count());

                var ent = repository.TestHistory.Simple.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, ent.Count());

                Assert.AreEqual("3 2000-01-01T00:00:00,1 2011-01-01T00:00:00", fh.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }


        [TestMethod]
        public void HistoryInsertAsActive()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id3) + ", 3, '2000-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is in the middle of history
                var a2 = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince).Skip(1).Take(1).Single();
                repository.TestHistory.Simple_History.Insert(new[] { new TestHistory.Simple_History() {
                    ActiveSince = new DateTime(2013, 1, 1),
                    Code = 4,
                    EntityID = id1
                } });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(4, fh.Count());

                var currentItem = repository.TestHistory.Simple.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, currentItem.Count());

                Assert.AreEqual("4 a 2013-01-01T00:00:00", currentItem.Select(item => item.Code + " " + item.Name + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
                Assert.AreEqual("3 2000-01-01T00:00:00,2 2001-01-01T00:00:00,1 2011-01-01T00:00:00,4 2013-01-01T00:00:00", fh.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }


        [TestMethod]
        public void HistoryInsertAsHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id3) + ", 3, '2000-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is in the middle of history
                var a2 = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince).Skip(1).Take(1).Single();
                repository.TestHistory.Simple_History.Insert(new[] { new TestHistory.Simple_History() {
                    ActiveSince = new DateTime(2010, 1, 1),
                    Code = 4,
                    EntityID = id1
                } });

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(4, fh.Count());

                var currentItem = repository.TestHistory.Simple.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, currentItem.Count());

                Assert.AreEqual("1 a 2011-01-01T00:00:00", currentItem.Select(item => item.Code + " " + item.Name + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
                Assert.AreEqual("3 2000-01-01T00:00:00,2 2001-01-01T00:00:00,4 2010-01-01T00:00:00,1 2011-01-01T00:00:00", fh.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }


        [TestMethod]
        public void HistoryDeleteCurrentItemAndLastInHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id3) + ", 3, '2000-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is in the middle of history
                var delEnt = repository.TestHistory.Simple_History.Query().OrderByDescending(x => x.ActiveSince).Take(2).ToArray();
                repository.TestHistory.Simple_History.Delete(delEnt);

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, fh.Count());

                var he = repository.TestHistory.Simple_Changes.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(0, he.Count());

                var ent = repository.TestHistory.Simple.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(1, ent.Count());

                Assert.AreEqual("3 2000-01-01T00:00:00", fh.Select(item => item.Code + " " + Dump(item.ActiveSince)).Aggregate((i1, i2) => i1 + "," + i2));
            }
        }


        [TestMethod]
        public void HistoryDeleteAllHistory()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestHistory.Simple",
                    "INSERT INTO TestHistory.Simple (ID, Code, Name, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 1, 'a', '2011-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id2) + ", 2, '2001-01-01')",
                    "INSERT INTO TestHistory.Simple_Changes (EntityID, ID, Code, ActiveSince) VALUES (" + SqlUtility.QuoteGuid(id1) + "," + SqlUtility.QuoteGuid(id3) + ", 3, '2000-01-01')"});
                var repository = new Common.DomRepository(executionContext);

                // take entry that is in the middle of history
                var delEnt = repository.TestHistory.Simple_History.All().ToArray();
                repository.TestHistory.Simple_History.Delete(delEnt);

                executionContext.NHibernateSession.Clear();
                var now = SqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                var fh = repository.TestHistory.Simple_History.Query().OrderBy(x => x.ActiveSince);
                Assert.AreEqual(0, fh.Count());
            }
        }
    }
}
