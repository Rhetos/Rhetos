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
                repository.TestLazyLoad.Simple.Delete(repository.TestLazyLoad.Simple.All());
                repository.TestLazyLoad.SimpleBase.Delete(repository.TestLazyLoad.SimpleBase.All());
                repository.TestLazyLoad.Parent.Delete(repository.TestLazyLoad.Parent.All());

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
    }
}
