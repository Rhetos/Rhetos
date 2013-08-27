using Autofac.Features.Metadata;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public Dictionary<Type, IDictionary<string, object>> MetadataByPluginType { get; private set; }
        private object _metadataByPluginTypeLock = new object();

        public Dictionary<Type, Type[]> SortedImplementations { get; private set; }
        private object _sortedImplementationsLock = new object();

        public PluginsMetadataCache(Lazy<IEnumerable<Meta<TPlugin>>> pluginsWithMetadata)
        {
            if (MetadataByPluginType == null)
                lock (_metadataByPluginTypeLock)
                    if (MetadataByPluginType == null)
                        MetadataByPluginType = pluginsWithMetadata.Value.ToDictionary(pm => pm.Value.GetType(), pm => pm.Metadata);

            SortedImplementations = new Dictionary<Type, Type[]>();
        }

        public void SortByMetadataDependsOn(Type key, TPlugin[] plugins)
        {
            Type[] pluginTypesSorted;

            lock (_sortedImplementationsLock)
            {
                if (!SortedImplementations.TryGetValue(key, out pluginTypesSorted))
                {
                    var dependencies = plugins
                        .Select(plugin => Tuple.Create(GetMetadata(plugin.GetType(), MefProvider.DependsOn), plugin.GetType()))
                        .Where(dependency => dependency.Item1 != null)
                        .ToArray();

                    List<Type> pluginTypesSortedList = plugins.Select(plugin => plugin.GetType()).ToList();
                    DirectedGraph.TopologicalSort(pluginTypesSortedList, dependencies);

                    pluginTypesSorted = pluginTypesSortedList.ToArray();
                    SortedImplementations.Add(key, pluginTypesSorted);
                }
            }

            DirectedGraph.SortByGivenOrder(plugins, pluginTypesSorted.ToArray(), plugin => plugin.GetType());
        }

        public Type GetMetadata(Type pluginType, string metadataKey)
        {
            IDictionary<string, object> metadata;
            if (!MetadataByPluginType.TryGetValue(pluginType, out metadata))
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
