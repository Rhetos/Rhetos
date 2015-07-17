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
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Persistence;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class LazyLoadTest
    {
        [TestMethod]
        public void LazyLoadReferenceBaseExtensionLinkedItems()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestLazyLoad.Simple.Delete(repository.TestLazyLoad.Simple.Load());
                repository.TestLazyLoad.SimpleBase.Delete(repository.TestLazyLoad.SimpleBase.Load());
                repository.TestLazyLoad.Parent.Delete(repository.TestLazyLoad.Parent.Load());

                var p1 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p1" };
                var sb1 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb1" };
                var sb2 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb2" };
                var s1 = new TestLazyLoad.Simple { ID = sb1.ID, ParentID = p1.ID };
                var s2 = new TestLazyLoad.Simple { ID = sb2.ID, ParentID = p1.ID };
                repository.TestLazyLoad.Parent.Insert(p1);
                repository.TestLazyLoad.SimpleBase.Insert(sb1, sb2);
                repository.TestLazyLoad.Simple.Insert(s1, s2);

                container.Resolve<IPersistenceCache>().ClearCache();
                var loadedSimple = repository.TestLazyLoad.Simple.Query().ToList();
                Assert.AreEqual("p1/sb1, p1/sb2", TestUtility.DumpSorted(loadedSimple, item => item.Parent.Name + "/" + item.Base.Name));

                container.Resolve<IPersistenceCache>().ClearCache();
                var loadedBase = repository.TestLazyLoad.SimpleBase.Query().ToList();
                Assert.AreEqual("p1/sb1, p1/sb2", TestUtility.DumpSorted(loadedBase, item => item.Extension_Simple.Parent.Name + "/" + item.Name));

                container.Resolve<IPersistenceCache>().ClearCache();
                var loadedParent = repository.TestLazyLoad.Parent.Query().ToList().Single();
                Assert.AreEqual("p1/sb1, p1/sb2", TestUtility.DumpSorted(loadedParent.Children, item => item.Parent.Name + "/" + item.Base.Name));
            }
        }

        [TestMethod]
        public void LinkedItems()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestLazyLoad.Simple.Delete(repository.TestLazyLoad.Simple.Load());
                repository.TestLazyLoad.SimpleBase.Delete(repository.TestLazyLoad.SimpleBase.Load());
                repository.TestLazyLoad.Parent.Delete(repository.TestLazyLoad.Parent.Load());

                var p1 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p1" };
                var p2 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p2" };
                repository.TestLazyLoad.Parent.Insert(p1, p2);

                var sb11 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb11" };
                var sb12 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb12" };
                var sb2 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb2" };
                repository.TestLazyLoad.SimpleBase.Insert(sb11, sb12, sb2);

                var s11 = new TestLazyLoad.Simple { ID = sb11.ID, ParentID = p1.ID };
                var s12 = new TestLazyLoad.Simple { ID = sb12.ID, ParentID = p1.ID };
                var s2 = new TestLazyLoad.Simple { ID = sb2.ID, ParentID = p2.ID };
                repository.TestLazyLoad.Simple.Insert(s11, s12, s2);

                {
                    // Using "parentsQuery", reading children's names results with a single SQL query.
                    var parentsQuery = repository.TestLazyLoad.Parent.Query();
                    var childrenNames = parentsQuery.SelectMany(parent => parent.Children.Select(child => child.Base.Name)).ToArray();
                    Assert.AreEqual("sb11, sb12, sb2", TestUtility.DumpSorted(childrenNames));
                }

                {
                    // Using "ToList()" in the following query results with lazy loading the "Children" members (2 additional SQL queryes,
                    // one for each parent), and lazy lading the "Base.Name" property (3 additional SQL queryes, one for each child).
                    var parentsList = repository.TestLazyLoad.Parent.Query().ToList();
                    var childrenNames = parentsList.SelectMany(parent => parent.Children.Select(child => child.Base.Name)).ToArray();
                    Assert.AreEqual("sb11, sb12, sb2", TestUtility.DumpSorted(childrenNames));
                }
            }
        }
    }
}
