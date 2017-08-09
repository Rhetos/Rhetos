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

namespace CommonConcepts.Test
{
    [TestClass]
    public class LockPropertyTest
    {
        private static void AssertData(string expected, Common.DomRepository repository)
        {
            Assert.AreEqual(expected, TestUtility.DumpSorted(repository.TestLockItems.Simple.Query(), item => item.Name));
        }
        
        private static void AssertDataSimple2(string expected, Common.DomRepository repository)
        {
            Assert.AreEqual(expected, TestUtility.DumpSorted(repository.TestLockItems.Simple2.Query(), item => item.Name));
        }

        [TestMethod]
        public void UpdateLockedData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s1", Count = -1 };
                var s2 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s2", Count = 1 };

                repository.TestLockItems.Simple.Insert(new[] { s1, s2 });
                AssertData("s1, s2", repository);

                foreach (var e in new[] { s1, s2 })
                    e.Name = e.Name + "x";
                AssertData("s1, s2", repository);

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s1 }), "Name is locked if count negative.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s2, s1 }), "Name is locked if count negative.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s1, s2 }), "Name is locked if count negative.");
                AssertData("s1, s2", repository);

                repository.TestLockItems.Simple.Update(new[] { s2 });
                AssertData("s1, s2x", repository);
            }
        }

        [TestMethod]
        public void UpdateTryRemoveLock()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s1", Count = -1 };
                repository.TestLockItems.Simple.Insert(new[] { s1 });
                AssertData("s1", repository);

                s1.Name = "abc";
                s1.Count = 1;
                AssertData("s1", repository);

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple.Update(new[] { s1 }), "Name is locked if count negative.");
                AssertData("s1", repository);
            }
        }

        [TestMethod]
        public void UpdateTrySetLock()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestLockItems.Simple { ID = Guid.NewGuid(), Name = "s1", Count = 1 };
                repository.TestLockItems.Simple.Insert(new[] { s1 });
                AssertData("s1", repository);

                s1.Name = "abc";
                s1.Count = -1;
                AssertData("s1", repository);

                repository.TestLockItems.Simple.Update(new[] { s1 });
                AssertData("abc", repository);
            }
        }


        [TestMethod]
        public void UpdateLockedDataReference()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestLockItems.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();
                Guid[] guids = new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                var s1 = new TestLockItems.Simple { ID = guids[0], Name = "s1", Count = -1 };
                var s2 = new TestLockItems.Simple { ID = guids[1], Name = "s2", Count = 1 };

                var t1 = new TestLockItems.Simple2 { ID = guids[2], Name = "t1", TestReferenceID = s1.ID, Count = -1 };
                var t2 = new TestLockItems.Simple2 { ID = guids[3], Name = "t2", TestReferenceID = s1.ID, Count = 1 };

                repository.TestLockItems.Simple.Insert(new[] { s1, s2 });
                AssertData("s1, s2", repository);
                repository.TestLockItems.Simple2.Insert(new[] { t1, t2 });
                AssertDataSimple2("t1, t2", repository);

                foreach (var e in new[] { t1, t2 })
                    e.TestReferenceID = s1.ID;
                repository.TestLockItems.Simple2.Update(new[] { t1 });

                AssertDataSimple2("t1, t2", repository);
                foreach (var e in new[] { t1, t2 })
                    e.TestReferenceID = s2.ID;

                TestUtility.ShouldFail(() => repository.TestLockItems.Simple2.Update(new[] { t1 }), "TestReference is locked if count negative.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple2.Update(new[] { t2, t1 }), "TestReference is locked if count negative.");
                TestUtility.ShouldFail(() => repository.TestLockItems.Simple2.Update(new[] { t1, t2 }), "TestReference is locked if count negative.");
                AssertDataSimple2("t1, t2", repository);

                repository.TestLockItems.Simple2.Update(new[] { t2 });
                AssertDataSimple2("t1, t2", repository);
            }
        }

    }
}
