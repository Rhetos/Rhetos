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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ReferenceTest
    {
        [TestMethod]
        public void ErrorMetadataOnInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestReference.Child.Delete(repository.TestReference.Child.Query());
                repository.TestReference.Parent.Delete(repository.TestReference.Parent.Query());

                var invalidItem = new TestReference.Child { ParentID = Guid.NewGuid() };

                var ex = TestUtility.ShouldFail<UserException>(
                    () => repository.TestReference.Child.Insert(invalidItem),
                    "The entered value references nonexistent record");
                Assert.AreEqual("DataStructure:TestReference.Child,Property:ParentID,Referenced:TestReference.Parent", ex.SystemMessage);
            }
        }

        [TestMethod]
        public void ErrorMetadataOnUpdate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestReference.Child.Delete(repository.TestReference.Child.Query());
                repository.TestReference.Parent.Delete(repository.TestReference.Parent.Query());

                var p1 = new TestReference.Parent { ID = Guid.NewGuid(), Name = "p1" };
                repository.TestReference.Parent.Insert(p1);

                var c1 = new TestReference.Child { ParentID = p1.ID, Name = "c1" };
                repository.TestReference.Child.Insert(c1);

                c1.ParentID = Guid.NewGuid();
                var ex = TestUtility.ShouldFail<UserException>(
                    () => repository.TestReference.Child.Update(c1),
                    "The entered value references nonexistent record");
                Assert.AreEqual("DataStructure:TestReference.Child,Property:ParentID,Referenced:TestReference.Parent", ex.SystemMessage);
            }
        }

        [TestMethod]
        public void ErrorMetadataOnDelete()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestReference.Child.Delete(repository.TestReference.Child.Query());
                repository.TestReference.Parent.Delete(repository.TestReference.Parent.Query());

                var p1 = new TestReference.Parent { ID = Guid.NewGuid(), Name = "p1" };
                repository.TestReference.Parent.Insert(p1);

                var c1 = new TestReference.Child { ParentID = p1.ID, Name = "c1" };
                repository.TestReference.Child.Insert(c1);

                var ex = TestUtility.ShouldFail<UserException>(
                    () => repository.TestReference.Parent.Delete(p1),
                    "It is not allowed to delete a record that is referenced by other records.");
                Assert.AreEqual("DataStructure:TestReference.Child,Property:ParentID,Referenced:TestReference.Parent", ex.SystemMessage);
            }
        }
    }
}
