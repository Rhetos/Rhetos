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
using Rhetos.Extensibility.Test;
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
            Lazy<IEnumerable<PluginMetadata<IConceptDatabaseDefinition>>> pluginsWithMetadata = new Lazy<IEnumerable<PluginMetadata<IConceptDatabaseDefinition>>>(() =>
                conceptImplementations.Select(pm => new PluginMetadata<IConceptDatabaseDefinition>(pm.Plugin.GetType(), pm.Metadata)));
            Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>> pluginsByImplementation = new Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>>(() =>
                new StubIndex<IConceptDatabaseDefinition>(conceptImplementations));

            return new PluginsContainer<IConceptDatabaseDefinition>(plugins, pluginsByImplementation, new PluginsMetadataCache<IConceptDatabaseDefinition>(pluginsWithMetadata, new StubIndex<SuppressPlugin>()));
        }
    }
}
