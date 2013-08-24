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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac.Features.Metadata;
using Rhetos.TestCommon;

namespace Rhetos.Extensibility.Test
{
    [TestClass]
    public class PluginsContainerTest
    {
        class BaseClass { public string Name; }

        class DerivedClass : BaseClass { }

        [TestMethod]
        public void GetTypeHierarchyTest()
        {
            object o = new DerivedClass();
            string expected = "BaseClass-DerivedClass";
            string actual = string.Join("-", PluginsContainerAccessor<object>.Access_GetTypeHierarchy(o.GetType()).Select(type => type.Name));
            Assert.AreEqual(expected, actual);

            o = new object();
            expected = "";
            actual = string.Join("-", PluginsContainerAccessor<object>.Access_GetTypeHierarchy(o.GetType()).Select(type => type.Name));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Scope()
        {
            var context = "1";

            var plugins = new PluginsContainer<BaseClass>(new[]
            {
                new Meta<Func<BaseClass>>(() => new BaseClass { Name = "base" + context }, new Dictionary<string, object> { { MefProvider.Implements, typeof(int) } }),
                new Meta<Func<BaseClass>>(() => new DerivedClass { Name = "derived" + context }, new Dictionary<string, object> { { MefProvider.Implements, typeof(int) } })
            });

            Assert.AreEqual("base1, derived1", TestUtility.DumpSorted(plugins.GetPlugins(), p => p.Name));
            Assert.AreEqual("base1, derived1", TestUtility.DumpSorted(plugins.GetImplementations(typeof(int)), p => p.Name));

            context = "2";
            Assert.AreEqual("base2, derived2", TestUtility.DumpSorted(plugins.GetPlugins(), p => p.Name));
            Assert.AreEqual("base2, derived2", TestUtility.DumpSorted(plugins.GetImplementations(typeof(int)), p => p.Name));
        }
    }
}
