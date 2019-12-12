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

using Autofac.Features.Indexed;
using Autofac.Features.Metadata;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator.Test
{
    public static class MockDatabasePluginsContainer
    {
        public static PluginsContainer<IConceptDatabaseDefinition> Create(PluginsMetadataList<IConceptDatabaseDefinition> conceptImplementations = null)
        {
            if (conceptImplementations == null)
                conceptImplementations = new PluginsMetadataList<IConceptDatabaseDefinition>();

            Lazy<IEnumerable<IConceptDatabaseDefinition>> plugins = new Lazy<IEnumerable<IConceptDatabaseDefinition>>(() =>
                conceptImplementations.Select(pm => pm.Plugin));
            Lazy<IEnumerable<Meta<IConceptDatabaseDefinition>>> pluginsWithMetadata = new Lazy<IEnumerable<Meta<IConceptDatabaseDefinition>>>(() =>
                conceptImplementations.Select(pm => new Meta<IConceptDatabaseDefinition>(pm.Plugin, pm.Metadata)));
            Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>> pluginsByImplementation = new Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>>(() =>
                new StubIndex<IConceptDatabaseDefinition>(conceptImplementations));

            return new PluginsContainer<IConceptDatabaseDefinition>(plugins, pluginsByImplementation, new PluginsMetadataCache<IConceptDatabaseDefinition>(pluginsWithMetadata, new StubIndex<SuppressPlugin>()));
        }

        private class StubIndex<TPlugin> : IIndex<Type, IEnumerable<TPlugin>>
        {
            private readonly PluginsMetadataList<TPlugin> _pluginsWithMedata;

            public StubIndex(PluginsMetadataList<TPlugin> pluginsWithMedata)
            {
                _pluginsWithMedata = pluginsWithMedata;
            }
            public StubIndex()
            {
                _pluginsWithMedata = new PluginsMetadataList<TPlugin>();
            }
            public bool TryGetValue(Type key, out IEnumerable<TPlugin> value)
            {
                value = this[key];
                return true;
            }
            public IEnumerable<TPlugin> this[Type key]
            {
                get
                {
                    return _pluginsWithMedata
                        .Where(pm => pm.Metadata.Any(metadata => metadata.Key == MefProvider.Implements && (Type)metadata.Value == key))
                        .Select(pm => pm.Plugin)
                        .ToArray();
                }
            }
        }
    }

    public class PluginsMetadataList<TPlugin> : List<(TPlugin Plugin, Dictionary<string, object> Metadata)>
    {
        public void Add(TPlugin plugin, Dictionary<string, object> metadata)
        {
            Add((plugin, metadata));
        }

        public void Add(TPlugin plugin)
        {
            Add((plugin, new Dictionary<string, object> { }));
        }

        public void Add(TPlugin plugin, Type implementsConceptInfo)
        {
            Add((plugin, new Dictionary<string, object> { { MefProvider.Implements, implementsConceptInfo } }));
        }
    }
}
