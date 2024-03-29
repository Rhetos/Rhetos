﻿/*
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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator.Test
{
    public static class MockDatabasePluginsContainer
    {
        public static PluginsContainer<IConceptDatabaseGenerator> Create(PluginsMetadataList<IConceptDatabaseGenerator> conceptImplementations = null)
        {
            if (conceptImplementations == null)
                conceptImplementations = new PluginsMetadataList<IConceptDatabaseGenerator>();

            Lazy<IEnumerable<IConceptDatabaseGenerator>> plugins = new Lazy<IEnumerable<IConceptDatabaseGenerator>>(() =>
                conceptImplementations.Select(pm => pm.Plugin));
            Lazy<IEnumerable<PluginMetadata<IConceptDatabaseGenerator>>> pluginsWithMetadata = new Lazy<IEnumerable<PluginMetadata<IConceptDatabaseGenerator>>>(() =>
                conceptImplementations.Select(pm => new PluginMetadata<IConceptDatabaseGenerator>(pm.Plugin.GetType(), pm.Metadata)));
            Lazy<IIndex<Type, IEnumerable<IConceptDatabaseGenerator>>> pluginsByImplementation = new Lazy<IIndex<Type, IEnumerable<IConceptDatabaseGenerator>>>(() =>
                new StubIndex<IConceptDatabaseGenerator>(conceptImplementations));

            return new PluginsContainer<IConceptDatabaseGenerator>(plugins, pluginsByImplementation, new PluginsMetadataCache<IConceptDatabaseGenerator>(pluginsWithMetadata, new StubIndex<SuppressPlugin>()), new ConsoleLogProvider());
        }
    }
}
