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
        IEnumerable<TPlugin> _plugins;
        IDictionary<Type, IDictionary<string, object>> _metadataByPluginType;
        IDictionary<Type, IEnumerable<TPlugin>> _pluginsByImplementation;

        public PluginsContainer(IEnumerable<Meta<TPlugin>> pluginsWithMetadata)
        {
            _plugins = pluginsWithMetadata.Select(pm => pm.Value).ToArray();

            _metadataByPluginType = pluginsWithMetadata.ToDictionary(pm => pm.Value.GetType(), pm => pm.Metadata);

            _pluginsByImplementation = pluginsWithMetadata
                .Select(pm => new { Plugin = pm.Value, Implements = GetMetadata(pm.Value, MefProvider.Implements) })
                .Where(pi => pi.Implements != null)
                .GroupBy(pi => pi.Implements)
                .ToDictionary(
                    group => group.Key, // Organizes plugins in groups by Type that each plugin is handling (MEF metadata "Implements").
                    group => SortByDependencies(group.Select(pi => pi.Plugin))); // Sorts plugins of each by their explicitly given dependencies (MEF metadata "DependsOn").
        }

        protected IEnumerable<TPlugin> SortByDependencies(IEnumerable<TPlugin> pluginsInGroup)
        {
            var dependencies = pluginsInGroup
                .Select(plugin => Tuple.Create(GetMetadata(plugin, MefProvider.DependsOn), plugin.GetType()))
                .Where(dependency => dependency.Item1 != null)
                .ToArray();

            var pluginTypesSorted = pluginsInGroup.Select(plugin => plugin.GetType()).ToList();
            DirectedGraph.TopologicalSort(pluginTypesSorted, dependencies);

            var pluginsSorted = pluginsInGroup.ToArray();
            DirectedGraph.SortByGivenOrder(pluginsSorted, pluginTypesSorted.ToArray(), plugin => plugin.GetType());
            return pluginsSorted;
        }

        public IEnumerable<TPlugin> GetPlugins()
        {
            return _plugins;
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
            var plugins = typeHierarchy
                .Where(type => _pluginsByImplementation.ContainsKey(type))
                .SelectMany(type => _pluginsByImplementation[type]);
            return plugins;
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
