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
                var px = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "px" };
                repository.TestLazyLoad.Parent.Insert(p1);

                var sb1 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb1" };
                var sb2 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb2" };
                var sbx = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sbx" };
                repository.TestLazyLoad.SimpleBase.Insert(sb1, sb2);

                var s1 = new TestLazyLoad.Simple { ID = sb1.ID, ParentID = p1.ID };
                var s2 = new TestLazyLoad.Simple { ID = sb2.ID, ParentID = p1.ID };
                var sx = new TestLazyLoad.Simple { ID = sbx.ID, ParentID = px.ID };
                repository.TestLazyLoad.Simple.Insert(s1, s2);

                var loadedBase = repository.TestLazyLoad.SimpleBase.Query().ToList();
                repository.TestLazyLoad.SimpleBase.Insert(sbx);
                Assert.AreEqual("p1/sb1, p1/sb2", TestUtility.DumpSorted(loadedBase, item => item.Extension_Simple.Parent.Name + "/" + item.Name));

                var loadedParent = repository.TestLazyLoad.Parent.Query().ToList().Single();
                repository.TestLazyLoad.Parent.Insert(px);
                Assert.AreEqual("p1/sb1, p1/sb2", TestUtility.DumpSorted(loadedParent.Children, item => item.Parent.Name + "/" + item.Base.Name));

                var loadedSimple = repository.TestLazyLoad.Simple.Query().ToList();
                repository.TestLazyLoad.Simple.Insert(sx);
                Assert.AreEqual("p1/sb1, p1/sb2", TestUtility.DumpSorted(loadedSimple, item => item.Parent.Name + "/" + item.Base.Name));
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
                    // Using "ToList()" in the following query results with lazy loading the "Children" members (2 additional SQL queries,
                    // one for each parent), and lazy lading the "Base.Name" property (3 additional SQL queries, one for each child).
                    var parentsList = repository.TestLazyLoad.Parent.Query().ToList();
                    var childrenNames = parentsList.SelectMany(parent => parent.Children.Select(child => child.Base.Name)).ToArray();
                    Assert.AreEqual("sb11, sb12, sb2", TestUtility.DumpSorted(childrenNames));
                }
            }
        }

        [TestMethod]
        public void UsableObjectsAfterClearCache()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestLazyLoad.Simple.Delete(repository.TestLazyLoad.Simple.Load());
                repository.TestLazyLoad.SimpleBase.Delete(repository.TestLazyLoad.SimpleBase.Load());
                repository.TestLazyLoad.Parent.Delete(repository.TestLazyLoad.Parent.Load());

                var p0 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p0" };
                repository.TestLazyLoad.Parent.Insert(p0);

                var sb0 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb0" };
                var sb1 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb1" };
                repository.TestLazyLoad.SimpleBase.Insert(sb0, sb1);

                var s0 = new TestLazyLoad.Simple { ID = sb0.ID, ParentID = p0.ID };
                var s1 = new TestLazyLoad.Simple { ID = sb1.ID, ParentID = p0.ID };
                repository.TestLazyLoad.Simple.Insert(s0, s1);

                //container.Resolve<IPersistenceCache>().ClearCache(); // TODO: Update or delete this unit test.

                var parents = repository.TestLazyLoad.Parent.Query().OrderBy(sb => sb.Name).ToList();
                var simpleBases = repository.TestLazyLoad.SimpleBase.Query().OrderBy(sb => sb.Name).ToList();
                var simples = repository.TestLazyLoad.Simple.Query().OrderBy(s => s.Base.Name).ToList();

                Assert.AreEqual("sb0", simples[0].Base.Name);
                Assert.AreEqual("p0", simples[0].Parent.Name);
                Assert.AreEqual("sb0, sb1", TestUtility.DumpSorted(parents[0].Children.Select(c => c.Base.Name)));

                // When removing objects from Entity Framework's cache, the EF will automatically set references
                // between objects to null. Rhetos includes a hack to keep the references, so some data will
                // be available even though the proxies will probably not work.
                //container.Resolve<IPersistenceCache>().ClearCache();; // TODO: Update or delete this unit test.

                Assert.AreEqual("sb0", simples[0].Base.Name);
                Assert.AreEqual("p0", simples[0].Parent.Name);
                Assert.AreEqual("sb0, sb1", TestUtility.DumpSorted(parents[0].Children.Select(c => c.Base.Name)));
            }
        }

        [TestMethod]
        public void LoadAndFilterShouldNotReturnNavigationProperties()
        {
            using (var container = new RhetosTestContainer())
            {
                var simpleBase = container.Resolve<Common.DomRepository>().TestLazyLoad.SimpleBase;
                simpleBase.Delete(simpleBase.Load());

                var sb0 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb0" };
                simpleBase.Insert(sb0);

                Action<string, IEnumerable<object>> testType = (testName, items) =>
                    Assert.AreEqual("TestLazyLoad.SimpleBase", items.First().GetType().ToString(), testName);

#pragma warning disable CS0618 // Type or member is obsolete
                testType("All", simpleBase.All());
#pragma warning restore CS0618 // Type or member is obsolete
                testType("Load", simpleBase.Load());
                testType("Load Expression", simpleBase.Load(item => true));
                testType("Load FilterAll", simpleBase.Load(new FilterAll()));
                testType("Load Guid[]", simpleBase.Load(new[] { sb0.ID }));
                testType("Load FilterCriteria[]", simpleBase.Load(new[] { new FilterCriteria("ID", "equal", sb0.ID) }));
#pragma warning disable CS0618 // Type or member is obsolete
                testType("FilterLoad FilterAll", simpleBase.Filter(new FilterAll()));
                testType("FilterLoad Guid[]", simpleBase.Filter(new[] { sb0.ID }));
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [TestMethod]
        public void QueryLoaded()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestLazyLoad.Simple.Delete(repository.TestLazyLoad.Simple.Load());
                repository.TestLazyLoad.SimpleBase.Delete(repository.TestLazyLoad.SimpleBase.Load());
                repository.TestLazyLoad.Parent.Delete(repository.TestLazyLoad.Parent.Load());

                var p0 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p0" };
                var p1 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p1" };
                var p2 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p2" };
                repository.TestLazyLoad.Parent.Insert(p0, p1, p2);

                var sb0 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb0" };
                var sb1 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb1" };
                repository.TestLazyLoad.SimpleBase.Insert(sb0, sb1);

                var s0 = new TestLazyLoad.Simple { ID = sb0.ID, ParentID = p0.ID };
                var s1 = new TestLazyLoad.Simple { ID = sb1.ID, ParentID = p0.ID };
                repository.TestLazyLoad.Simple.Insert(s0, s1);

                var loadedSimple = repository.TestLazyLoad.Simple.Query().OrderBy(item => item.Base.Name).ToList();
                Assert.AreEqual("p0/sb0, p0/sb1", TestUtility.DumpSorted(loadedSimple, item => item.Parent.Name + "/" + item.Base.Name));
            }
        }

        [TestMethod]
        public void OrmCacheShouldNotLeak()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var sqlExecuter = container.Resolve<ISqlExecuter>();

                var p0 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p0" };
                var p1 = new TestLazyLoad.Parent { ID = Guid.NewGuid(), Name = "p1" };
                repository.TestLazyLoad.Parent.Insert(p0, p1);

                var sb0 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb0" };
                var sb1 = new TestLazyLoad.SimpleBase { ID = Guid.NewGuid(), Name = "sb1" };
                repository.TestLazyLoad.SimpleBase.Insert(sb0, sb1);

                var s0 = new TestLazyLoad.Simple { ID = sb0.ID, ParentID = p0.ID, Name = "s0" };
                var s1 = new TestLazyLoad.Simple { ID = sb1.ID, ParentID = p1.ID, Name = "s1" };
                repository.TestLazyLoad.Simple.Insert(s0, s1);

                Func<string> report = () => TestUtility.DumpSorted(repository.TestLazyLoad.Simple.Query(), item => item.Name + "-" + item.Parent.Name + "-" + item.Base.Name);

                Assert.AreEqual("s0-p0-sb0, s1-p1-sb1", report());

                var sbItems = repository.TestLazyLoad.SimpleBase.Query(new[] { sb0.ID, sb1.ID }).ToList();
                var sItems = repository.TestLazyLoad.Simple.Query(new[] { s0.ID, s1.ID }).ToList();

                foreach (var item in sItems)
                    item.Name += "a";
                foreach (var item in sbItems)
                    item.Name += "b";

                Assert.AreEqual("s0-p0-sb0, s1-p1-sb1", report());

                repository.TestLazyLoad.Simple.Update(sItems);
                Assert.AreEqual("s0a-p0-sb0, s1a-p1-sb1", report());

                repository.TestLazyLoad.SimpleBase.Update(sbItems);
                Assert.AreEqual("s0a-p0-sb0b, s1a-p1-sb1b", report());

                sqlExecuter.ExecuteSql(
                    "UPDATE TestLazyLoad.Parent SET Name = Name + 'x'",
                    "UPDATE TestLazyLoad.SimpleBase SET Name = Name + 'x'",
                    "UPDATE TestLazyLoad.Simple SET Name = Name + 'x'");

                Assert.AreEqual("s0ax-p0x-sb0bx, s1ax-p1x-sb1bx", report());
            }
        }
    }
}
