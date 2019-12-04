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
        public static PluginsContainer<IConceptDatabaseDefinition> Create(PluginsMetadataList<IConceptDatabaseDefinition> conceptImplementations)
        {
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
            this.Add((plugin, metadata));
        }

        public void Add(TPlugin plugin)
        {
            this.Add((plugin, new Dictionary<string, object> { }));
        }
    }
}
