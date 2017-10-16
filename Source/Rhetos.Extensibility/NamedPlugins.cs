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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Extensibility
{
    /// <summary>
    /// This is a simple wrapper around IIndex, allowing plugin projects to compile without referencing Autofac.
    /// </summary>
    public class NamedPlugins<TPlugin> : INamedPlugins<TPlugin>
    {
        private IIndex<string, IEnumerable<TPlugin>> _pluginsByName;
        private PluginsMetadataCache<TPlugin> _cache;

        public NamedPlugins(IIndex<string, IEnumerable<TPlugin>> pluginsByName, PluginsMetadataCache<TPlugin> cache)
        {
            _pluginsByName = pluginsByName;
            _cache = cache;
        }

        public IEnumerable<TPlugin> GetPlugins(string name)
        {
            IEnumerable<TPlugin> plugins;
            if (_pluginsByName.TryGetValue(name, out plugins))
                return _cache.RemoveSuppressedPlugins(plugins);
            return Enumerable.Empty<TPlugin>();
        }
    }
}
