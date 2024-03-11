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
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Extensibility
{
    /// <summary>
    /// This is a simple wrapper around IIndex, allowing plugin projects to compile without referencing Autofac.
    /// </summary>
    public class NamedPlugins<TPlugin> : INamedPlugins<TPlugin>
    {
        private readonly IIndex<string, IEnumerable<TPlugin>> _pluginsByName;
        private readonly PluginsMetadataCache<TPlugin> _cache;

        public NamedPlugins(IIndex<string, IEnumerable<TPlugin>> pluginsByName, PluginsMetadataCache<TPlugin> cache)
        {
            _pluginsByName = pluginsByName;
            _cache = cache;
        }

        public IReadOnlyCollection<TPlugin> GetPlugins(string name)
        {
            if (_pluginsByName.TryGetValue(name, out var allPlugins))
            {
                var plugins = _cache.RemoveSuppressedPlugins(CsUtility.Materialized(allPlugins));
                if (plugins.Count > 1)
                    plugins = plugins.OrderBy(p => p.GetType().Name).ToArray();
                return plugins;
            }
            return [];
        }
    }
}
