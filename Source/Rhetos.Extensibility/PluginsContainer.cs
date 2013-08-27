using Autofac.Features.Indexed;
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
        Lazy<IEnumerable<Meta<TPlugin>>> _pluginsWithMetadata;
        Lazy<IIndex<Type, IEnumerable<TPlugin>>> _pluginsByImplementation;

        static Dictionary<Type, IDictionary<string, object>> _metadataByPluginType;
        static object _metadataByPluginTypeLock = new object();
        
        static Dictionary<Type, Type[]> _sortedImplementations = new Dictionary<Type, Type[]>();
        static object _sortedImplementationsLock = new object();

        public PluginsContainer(
            Lazy<IEnumerable<TPlugin>> plugins,
            Lazy<IEnumerable<Meta<TPlugin>>> pluginsWithMetadata,
            Lazy<IIndex<Type, IEnumerable<TPlugin>>> pluginsByImplementation)
        {
            _plugins = plugins;
            _pluginsWithMetadata = pluginsWithMetadata;
            _pluginsByImplementation = pluginsByImplementation;

            if (_metadataByPluginType == null)
                lock (_metadataByPluginTypeLock)
                    if (_metadataByPluginType == null)
                        _metadataByPluginType = pluginsWithMetadata.Value.ToDictionary(pm => pm.Value.GetType(), pm => pm.Metadata);
        }

        public IEnumerable<TPlugin> GetPlugins()
        {
            return _plugins.Value;
        }

        /// <param name="metadataKey">Use one of the constants from the Rhetos.Extensibility.MefProvider class.</param>
        public Type GetMetadata(TPlugin plugin, string metadataKey)
        {
            return GetMetadata(plugin.GetType(), metadataKey);
        }

        /// <param name="metadataKey">Use one of the constants from the Rhetos.Extensibility.MefProvider class.</param>
        public Type GetMetadata(Type pluginType, string metadataKey)
        {
            IDictionary<string, object> metadata;
            if (!_metadataByPluginType.TryGetValue(pluginType, out metadata))
                throw new FrameworkException(string.Format(
                    "There is no plugin {0} registered for {1}.",
                    pluginType.FullName, typeof(TPlugin).FullName));

            if (metadata == null)
                return null;

            object value;
            metadata.TryGetValue(metadataKey, out value);
            if (value == null)
                return null;

            if (!(value is Type))
                throw new FrameworkException(string.Format(
                    "Value of ExportMetadata attribute for '{2}' must be a Type (use typeof). Rhetos plugin {0} has '{2}' metadata value of type {1}.",
                    pluginType.FullName,
                    value.GetType().FullName,
                    metadataKey));

            return (Type)value;
        }

        public IEnumerable<TPlugin> GetImplementations(Type implements)
        {
            var typeHierarchy = GetTypeHierarchy(implements);
            var allImplementations = typeHierarchy.SelectMany(type => _pluginsByImplementation.Value[type]).ToArray();

            Type[] pluginTypesSorted;

            lock (_sortedImplementationsLock)
            {
                if (!_sortedImplementations.TryGetValue(implements, out pluginTypesSorted))
                {
                    var dependencies = allImplementations
                        .Select(plugin => Tuple.Create(GetMetadata(plugin, MefProvider.DependsOn), plugin.GetType()))
                        .Where(dependency => dependency.Item1 != null)
                        .ToArray();

                    List<Type> pluginTypesSortedList = allImplementations.Select(plugin => plugin.GetType()).ToList();
                    DirectedGraph.TopologicalSort(pluginTypesSortedList, dependencies);

                    pluginTypesSorted = pluginTypesSortedList.ToArray();
                    _sortedImplementations.Add(implements, pluginTypesSorted);
                }
            }

            DirectedGraph.SortByGivenOrder(allImplementations, pluginTypesSorted.ToArray(), plugin => plugin.GetType());

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
