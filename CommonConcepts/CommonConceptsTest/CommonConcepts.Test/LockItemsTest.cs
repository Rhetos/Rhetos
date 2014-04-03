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
    public class LockItemsTest
    {
        private static void AssertData(string expected, Common.DomRepository repository)
        {
            Assert.AreEqual(expected, TestUtility.DumpSorted(repository.TestLockItems.Simple.All(), item => item.Name));
        }

        [TestMethod]
        public void DeleteLockedData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s1" };
                var s2 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s2" };
                var s3Lock = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s3_lock" };
                var s4 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s4" };
                repository.TestLockItems.Simple.Insert(new[] { s1, s2, s3Lock, s4 });
                AssertData("s1, s2, s3_lock, s4", repository);

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Delete(new[] { s3Lock }), "Name contains lock mark.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Delete(new[] { s1, s3Lock }), "Name contains lock mark.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Delete(new[] { s3Lock, s4 }), "Name contains lock mark.");
                AssertData("s1, s2, s3_lock, s4", repository);

                repository.TestLockItems.Simple.Delete(new[] { s1 });
                AssertData("s2, s3_lock, s4", repository);
            }
        }

        [TestMethod]
        public void UpdateLockedData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s1" };
                var s2 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s2" };
                var s3Lock = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s3_lock" };
                var s4 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s4" };
                repository.TestLockItems.Simple.Insert(new[] { s1, s2, s3Lock, s4 });
                AssertData("s1, s2, s3_lock, s4", repository);

                foreach (var e in new[] {s1, s2, s3Lock, s4})
                    e.Name = e.Name + "x";
                AssertData("s1, s2, s3_lock, s4", repository);

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s3Lock }), "Name contains lock mark.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s1, s3Lock }), "Name contains lock mark.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s3Lock, s4 }), "Name contains lock mark.");
                AssertData("s1, s2, s3_lock, s4", repository);

                repository.TestLockItems.Simple.Update(new[] { s1 });
                AssertData("s1x, s2, s3_lock, s4", repository);
            }
        }

        [TestMethod]
        public void UpdateTryRemoveLock()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s3Lock = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s3_lock" };
                repository.TestLockItems.Simple.Insert(new[] { s3Lock });
                AssertData("s3_lock", repository);

                s3Lock.Name = "abc";
                AssertData("s3_lock", repository);

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s3Lock }), "Name contains lock mark.");
                AssertData("s3_lock", repository);
            }
        }

        [TestMethod]
        public void UpdatePersistentObject()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                {
                    var s3Lock = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s3_lock" };
                    repository.TestLockItems.Simple.Insert(new[] { s3Lock });
                    AssertData("s3_lock", repository);
                }

                {
                    var s3Persistent = repository.TestLockItems.Simple.All().Single();
                    s3Persistent.Name = "abc";
                    TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s3Persistent }),
                        "Name contains lock mark");
                    AssertData("s3_lock", repository);
                }
            }
        }

        [TestMethod]
        public void DeleteModifiedPersistentObject()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                {
                    var s3Lock = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s3_lock" };
                    repository.TestLockItems.Simple.Insert(new[] { s3Lock });

                    AssertData("s3_lock", repository);
                    container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                    AssertData("s3_lock", repository);
                }

                {
                    var s3Persistent = repository.TestLockItems.Simple.All().Single();
                    s3Persistent.Name = "abc";
                    TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Delete(new[] { s3Persistent }),
                        "Name contains lock mark");

                    AssertData("s3_lock", repository);
                    container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                    AssertData("s3_lock", repository);
                }
            }
        }

        [TestMethod]
        public void Spike_EvictContains()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "abc" };
                repository.TestLockItems.Simple.Insert(new[] { s });
                AssertData("abc", repository);
                Assert.IsFalse(container.Resolve<Common.ExecutionContext>().NHibernateSession.Contains(s));

                var s2 = repository.TestLockItems.Simple.All().Single();
                Assert.IsTrue(container.Resolve<Common.ExecutionContext>().NHibernateSession.Contains(s2));

                container.Resolve<Common.ExecutionContext>().NHibernateSession.Evict(s2);
                Assert.IsFalse(container.Resolve<Common.ExecutionContext>().NHibernateSession.Contains(s2));
            }
        }

        [TestMethod]
        public void Spike_EvictKeepsChangedData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;",
                    "INSERT INTO TestLockItems.Simple (Name) VALUES ('abc locked')" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = repository.TestLockItems.Simple.All().Single();
                Assert.AreEqual("abc locked", s1.Name);
                s1.Name = "def";

                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear(); // Same with Evict(s2)
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

                Assert.AreEqual("def", s1.Name);
                var s2 = repository.TestLockItems.Simple.All().Single();
                Assert.AreEqual("abc locked", s2.Name);
            }
        }

        [TestMethod]
        public void PersistentObjectIgnoredWhenVerifyingOldData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;",
                    "INSERT INTO TestLockItems.Simple (Name) VALUES ('abc locked')" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = repository.TestLockItems.Simple.All().Single();
                Assert.AreEqual("abc locked", s1.Name);
                s1.Name = "def";

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s1 }), "Name contains lock mark");

                Assert.AreEqual("def", s1.Name);
                Assert.AreEqual("abc locked", repository.TestLockItems.Simple.All().Single().Name);

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s1 }), "Name contains lock mark");

                Assert.AreEqual("def", s1.Name);
                Assert.AreEqual("abc locked", repository.TestLockItems.Simple.All().Single().Name);

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Insert(new[] { s1 }), "Inserting already existing object");

                Assert.AreEqual("def", s1.Name);
                Assert.AreEqual("abc locked", repository.TestLockItems.Simple.All().Single().Name);
            }
        }
    }
}
