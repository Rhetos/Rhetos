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
    public class SystemRequiredTest
    {
        [TestMethod]
        public void InsertSimple()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestSystemRequired.Child",
                        "DELETE FROM TestSystemRequired.Parent",
                    });
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestSystemRequired.Parent.Insert(new[] { new TestSystemRequired.Parent { Name = "Test" } });

                TestUtility.ShouldFail(
                    () => repository.TestSystemRequired.Parent.Insert(new[] { new TestSystemRequired.Parent { Name = null } }),
                    "Parent", "Name", "System required");
            }
        }

        [TestMethod]
        public void UpdateSimple()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestSystemRequired.Parent",
                        "DELETE FROM TestSystemRequired.Child"
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var parent = new TestSystemRequired.Parent { ID = Guid.NewGuid(), Name = "Test" };
                repository.TestSystemRequired.Parent.Insert(new[] { parent });

                parent.Name = null;
                TestUtility.ShouldFail(
                    () => repository.TestSystemRequired.Parent.Update(new[] { parent }),
                    "Parent", "Name", "System required");
            }
        }

        [TestMethod]
        public void InsertReference()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestSystemRequired.Child",
                        "DELETE FROM TestSystemRequired.Parent",
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var parentID = Guid.NewGuid();
                repository.TestSystemRequired.Parent.Insert(new[] { new TestSystemRequired.Parent { ID = parentID, Name = "Test" } });
                repository.TestSystemRequired.Child.Insert(new[] { new TestSystemRequired.Child { ParentID = parentID, Name = "Test2" } });

                TestUtility.ShouldFail(
                    () => repository.TestSystemRequired.Child.Insert(new[] { new TestSystemRequired.Child { ParentID = null, Name = "Test3" } }),
                    "Child", "System required");
            }
        }

        [TestMethod]
        public void BoolProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestSystemRequired.Simple2.Delete(repository.TestSystemRequired.Simple2.Load());

                repository.TestSystemRequired.Simple2.Insert(new TestSystemRequired.Simple2 { Name = "a", Tagged = false });
                repository.TestSystemRequired.Simple2.Insert(new TestSystemRequired.Simple2 { Name = "b", Tagged = true });

                Assert.AreEqual("a False, b True", TestUtility.DumpSorted(repository.TestSystemRequired.Simple2.Query(), item => item.Name + " " + item.Tagged));

                var invalidItem = new TestSystemRequired.Simple2 { ID = Guid.NewGuid(), Name = "c" };
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestSystemRequired.Simple2.Insert(invalidItem),
                    "required", "TestSystemRequired", "Simple2", "Tagged", invalidItem.ID.ToString());
            }
        }
    }
}
