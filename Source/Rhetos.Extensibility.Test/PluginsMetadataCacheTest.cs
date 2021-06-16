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

using Autofac.Features.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Extensibility.Test
{
    [TestClass]
    public class PluginsMetadataCacheTest
    {
        [TestMethod]
        public void SortedByMetadataDependsOn()
        {
            var conceptImplementations = new PluginsMetadataList<ITestPluginType>
            {
                { new TestPlugin1(), new Dictionary<string, object> { { MefProvider.DependsOn, typeof(TestPlugin3) } } },
                { new TestPlugin2(), new Dictionary<string, object> { { MefProvider.DependsOn, typeof(TestPlugin1) } } },
                { new TestPlugin3(), new Dictionary<string, object> { } },
            };
            Lazy<IEnumerable<PluginMetadata<ITestPluginType>>> pluginsWithMetadata = new Lazy<IEnumerable<PluginMetadata<ITestPluginType>>>(() =>
                conceptImplementations.Select(pm => new PluginMetadata<ITestPluginType>(pm.Plugin.GetType(), pm.Metadata)));

            var plugins = new ITestPluginType[] { new TestPlugin1(), new TestPlugin2(), new TestPlugin3() };
            var pmc = new PluginsMetadataCache<ITestPluginType>(pluginsWithMetadata, new StubIndex<SuppressPlugin>());
            var sortedPlugins = pmc.SortedByMetadataDependsOnAndRemoveSuppressed(this.GetType(), plugins);
            Assert.AreEqual("TestPlugin3, TestPlugin1, TestPlugin2", TestUtility.Dump(sortedPlugins, p => p.GetType().Name));
        }

        [TestMethod]
        public void SortedByMetadataDependsOnAndRemoveSuppressed()
        {
            var conceptImplementations = new PluginsMetadataList<ITestPluginType>
            {
                { new TestPlugin1(), new Dictionary<string, object> { { MefProvider.DependsOn, typeof(TestPlugin3) } } },
                { new TestPlugin2(), new Dictionary<string, object> { { MefProvider.DependsOn, typeof(TestPlugin1) } } },
                { new TestPlugin3(), new Dictionary<string, object> { } },
            };
            Lazy<IEnumerable<PluginMetadata<ITestPluginType>>> pluginsWithMetadata = new Lazy<IEnumerable<PluginMetadata<ITestPluginType>>>(() =>
                conceptImplementations.Select(pm => new PluginMetadata<ITestPluginType>(pm.Plugin.GetType(), pm.Metadata)));

            var plugins = new ITestPluginType[] { new TestPlugin1(), new TestPlugin2(), new TestPlugin3() };
            var suppressPlugins = new StubIndex<SuppressPlugin>(new PluginsMetadataList<SuppressPlugin> {
                { new SuppressPlugin(typeof(TestPlugin1)), typeof(ITestPluginType) } });
            var pmc = new PluginsMetadataCache<ITestPluginType>(pluginsWithMetadata, suppressPlugins);
            var sortedPlugins = pmc.SortedByMetadataDependsOnAndRemoveSuppressed(this.GetType(), plugins);
            Assert.AreEqual("TestPlugin3, TestPlugin2", TestUtility.Dump(sortedPlugins, p => p.GetType().Name));
        }

        private interface ITestPluginType { }

        private class TestPlugin1 : ITestPluginType { }

        private class TestPlugin2 : ITestPluginType { }

        private class TestPlugin3 : ITestPluginType { }

    }
}
