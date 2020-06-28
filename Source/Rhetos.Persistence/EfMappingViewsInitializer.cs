using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.Persistence
{
    // not being discovered because it is not a plugin?
    //[Export(typeof(IServerInitializer))]
    public class EfMappingViewsInitializer : IServerInitializer
    {
        public IEnumerable<string> Dependencies => null;

        private readonly IPersistenceCache _persistenceCache;
        private readonly IEfMappingViewsStore _efMappingViewsStore;
        private readonly ILogger _log;
        private readonly ILogger _performanceLogger;

        public EfMappingViewsInitializer(IPersistenceCache persistenceCache, IEfMappingViewsStore efMappingViewsStore, ILogProvider logProvider)
        {
            _persistenceCache = persistenceCache;
            _efMappingViewsStore = efMappingViewsStore;
            _log = logProvider.GetLogger(nameof(EfMappingViewsInitializer));
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(EfMappingViewsInitializer));
        }

        public void Initialize()
        {
            if (!(_persistenceCache is DbContext))
            {
                _log.Info(() => $"{nameof(IPersistenceCache)} implementation {_persistenceCache.GetType().Name} is not {nameof(DbContext)}. Skipping EfMappingViews generation.");
                return;
            }

            GenerateAndSaveViews();
        }

        private void GenerateAndSaveViews()
        {
            var sw = Stopwatch.StartNew();

            var objectContext = ((IObjectContextAdapter)_persistenceCache).ObjectContext;
            var mappingCollection = (StorageMappingItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
            var hash = mappingCollection.ComputeMappingHashValue();
            _performanceLogger.Write(sw, () => $"Calculate hash for current model.");
            var currentViewCache = _efMappingViewsStore.Load();

            if (!string.IsNullOrEmpty(currentViewCache?.Hash) && currentViewCache.Hash == hash)
            {
                _log.Trace(() => $"Hash not changed. View cache is valid. Skipping generation.");
                return;
            }

            sw.Restart();
            var errors = new List<EdmSchemaError>();
            var newViews = mappingCollection.GenerateViews(errors)
                .ToDictionary(a => EfMappingViewCache.GetExtentKey(a.Key), a => a.Value.EntitySql);
            var newViewCache = new EfMappingViews() {Hash = hash, Views = newViews};
            _performanceLogger.Write(sw, () => $"Generated new views. Old hash != new hash ('{currentViewCache?.Hash}' != '{hash}').");

            _efMappingViewsStore.Save(newViewCache);
        }
    }
}
