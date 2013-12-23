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
using Autofac.Features.Indexed;
using Autofac.Features.Metadata;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Extensibility
{
    public class PluginsContainer<TPlugin> : IPluginsContainer<TPlugin>
    {
        Lazy<IEnumerable<TPlugin>> _plugins;
        Lazy<IIndex<Type, IEnumerable<TPlugin>>> _pluginsByImplementation;
        PluginsMetadataCache<TPlugin> _cache;

        public PluginsContainer(
            Lazy<IEnumerable<TPlugin>> plugins,
            Lazy<IIndex<Type, IEnumerable<TPlugin>>> pluginsByImplementation,
            PluginsMetadataCache<TPlugin> cache)
        {
            _plugins = plugins;
            _pluginsByImplementation = pluginsByImplementation;
            _cache = cache;
        }

        public IEnumerable<TPlugin> GetPlugins()
        {
            return _plugins.Value;
        }

        /// <param name="metadataKey">Use one of the constants from the Rhetos.Extensibility.MefProvider class.</param>
        public Type GetMetadata(TPlugin plugin, string metadataKey)
        {
            return _cache.GetMetadata(plugin.GetType(), metadataKey);
        }

        /// <param name="metadataKey">Use one of the constants from the Rhetos.Extensibility.MefProvider class.</param>
        public Type GetMetadata(Type pluginType, string metadataKey)
        {
            return _cache.GetMetadata(pluginType, metadataKey);
        }

        public IEnumerable<TPlugin> GetImplementations(Type implements)
        {
            var typeHierarchy = GetTypeHierarchy(implements);
            var allImplementations = typeHierarchy.SelectMany(type => _pluginsByImplementation.Value[type]).ToArray();

            _cache.SortByMetadataDependsOn(implements, allImplementations);

            return allImplementations;
        }

        protected static List<Type> GetTypeHierarchy(Type type)
        {
            var types = new List<Type>();
            while (type != typeof(object))
            {
                types.Add(type);
                type = type.BaseType;
            }
            types.Reverse();
            return types;
        }
    }
}
