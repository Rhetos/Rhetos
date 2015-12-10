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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;

namespace CommonConcepts.Test
{
    [TestClass]
    public class UserRequiredPropertyTest
    {
        [TestMethod]
        public void ValidData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestUserRequired.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestUserRequired.Simple.Insert(new[] { new TestUserRequired.Simple { Count = 1, Name = "test1" } });
                repository.TestUserRequired.Simple.Insert(new[] { new TestUserRequired.Simple { Count = 0, Name = "test2" } });
                Assert.AreEqual("0-test2, 1-test1", TestUtility.DumpSorted(repository.TestUserRequired.Simple.Load(), item => item.Count + "-" + item.Name));
            }
        }

        [TestMethod]
        public void NullInteger()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestUserRequired.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                TestUtility.ShouldFail(() => repository.TestUserRequired.Simple.Insert(new[] { new TestUserRequired.Simple { Count = null, Name = "test3" } }), "required", "Count");
            }
        }

        [TestMethod]
        public void NullString()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestUserRequired.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                TestUtility.ShouldFail(() => repository.TestUserRequired.Simple.Insert(new[] { new TestUserRequired.Simple { Count = 4, Name = null } }), "required", "Name");
            }
        }

        [TestMethod]
        public void EmptyString()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestUserRequired.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                TestUtility.ShouldFail(() => repository.TestUserRequired.Simple.Insert(new[] { new TestUserRequired.Simple { Count = 5, Name = "" } }), "required", "Name");
            }
        }

        [TestMethod]
        public void BoolProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestUserRequired.Simple2.Delete(repository.TestUserRequired.Simple2.Load());

                repository.TestUserRequired.Simple2.Insert(new TestUserRequired.Simple2 { Name = "a", Tagged = false });
                repository.TestUserRequired.Simple2.Insert(new TestUserRequired.Simple2 { Name = "b", Tagged = true });

                Assert.AreEqual("a False, b True", TestUtility.DumpSorted(repository.TestUserRequired.Simple2.Query(), item => item.Name + " " + item.Tagged));

                var invalidItem = new TestUserRequired.Simple2 { ID = Guid.NewGuid(), Name = "c" };
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestUserRequired.Simple2.Insert(invalidItem),
                    "required", "TestUserRequired", "Simple2", "Tagged", invalidItem.ID.ToString());
            }
        }
    }
}
