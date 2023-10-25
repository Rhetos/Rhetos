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
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Rhetos.CommonConcepts.Test.Mocks;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class GenericRepositoryInstantiationTest
    {
        interface ISimpleEntity : IEntity
        {
            string Name { get; set; }
        }

        class SimpleEntity : ISimpleEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return Name != null ? Name : "<null>";
            }
        }

        static TestGenericRepository<ISimpleEntity, SimpleEntity> NewSimpleRepos(IEnumerable<SimpleEntity> items = null)
        {
            return new TestGenericRepository<ISimpleEntity, SimpleEntity>(items);
        }

        [TestMethod]
        public void CreateInstance()
        {
            var r = NewSimpleRepos();
            var a = r.CreateInstance();
            var b = r.CreateInstance();
            a.Name = "a";
            b.Name = "b";
            Assert.AreEqual("a", a.ToString());
            Assert.AreEqual("b", b.ToString());
        }

        [TestMethod]
        public void CreateList()
        {
            int queryCount = 0;
            var source = new[] { 11, 12, 13 }.Select(x => { queryCount++; return x; });

            var repos = NewSimpleRepos();
            Assert.AreEqual(0, queryCount);
            var list1 = repos.CreateList(source, (sourceItem, newItem) => { newItem.Name = sourceItem.ToString(); });
            Assert.AreEqual(3, queryCount, "CreateList should query the source only once.");

            var list2 = repos.CreateList(2);
            int nextName = 101;
            foreach (var item in list2)
                item.Name = (nextName++).ToString();

            Assert.AreEqual("11, 12, 13", TestUtility.DumpSorted(list1));
            Assert.AreEqual("101, 102", TestUtility.DumpSorted(list2));

            string expectedType = typeof(List<SimpleEntity>).FullName;
            TestUtility.AssertContains(list1.GetType().FullName, expectedType, "Instance should be a list of the entity type, not a list of interfaces.");
        }
    }
}
