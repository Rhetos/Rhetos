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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Extensibility
{
    /// <summary>
    /// This class must not cache TPlugin instances, because it is registered as SingleInstance (singleton).
    /// New TPlugin instances must be created separately for each client request, so they can use current
    /// contextual information such as IUserInfo or IPersistenceTransaction.
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public class PluginsMetadataCache<TPlugin>
    {
        private readonly Lazy<Dictionary<Type, IDictionary<string, object>>> _metadataByPluginType;
        private readonly ConcurrentDictionary<Type, List<Type>> _sortedImplementations = new();
        private readonly HashSet<Type> _suppressedPlugins;

        public PluginsMetadataCache(
            Lazy<IEnumerable<PluginMetadata<TPlugin>>> pluginsWithMetadata,
            IIndex<Type, IEnumerable<SuppressPlugin>> suppressPlugins)
        {
            _metadataByPluginType = new Lazy<Dictionary<Type, IDictionary<string, object>>>(
                () => pluginsWithMetadata.Value.ToDictionary(pm => pm.PluginType, pm => pm.Metadata),
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

            _suppressedPlugins = new HashSet<Type>(suppressPlugins[typeof(TPlugin)].Select(sp => sp.PluginType));
        }

        public IReadOnlyCollection<TPlugin> SortedByMetadataDependsOnAndRemoveSuppressed(Type cacheKey, IEnumerable<TPlugin> plugins)
        {
            var sortedPlugins = plugins.ToArray();
            if (sortedPlugins.Length > 1)
            {
                List<Type> pluginTypesSorted = _sortedImplementations.GetOrAdd(cacheKey, k => GetSortedPluginTypesByDependency(sortedPlugins.Select(p => p.GetType()).ToList()));
                Graph.SortByGivenOrder(sortedPlugins, pluginTypesSorted, plugin => plugin.GetType());
            }

            return RemoveSuppressedPlugins(sortedPlugins);
        }

        private List<Type> GetSortedPluginTypesByDependency(List<Type> pluginTypes)
        {
            var dependencies = pluginTypes
                .Select(pluginType => Tuple.Create(GetMetadata(pluginType, MefProvider.DependsOn), pluginType))
                .Where(dependency => dependency.Item1 != null)
                .ToArray();

            Graph.TopologicalSort(pluginTypes, dependencies);
            return pluginTypes;
        }

        public IReadOnlyCollection<TPlugin> RemoveSuppressedPlugins(IReadOnlyCollection<TPlugin> plugins)
        {
            if (_suppressedPlugins.Count > 0 && plugins.Any(p => _suppressedPlugins.Contains(p.GetType()))) // Optimization: Check if we can return the original argument, since most plugin types are not suppressed.
                return plugins.Where(p => !_suppressedPlugins.Contains(p.GetType())).ToArray();
            else
                return CsUtility.Materialized(plugins);
        }

        public Type GetMetadata(Type pluginType, string metadataKey)
        {
            IDictionary<string, object> metadata;
            if (!_metadataByPluginType.Value.TryGetValue(pluginType, out metadata))
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
    }
}
