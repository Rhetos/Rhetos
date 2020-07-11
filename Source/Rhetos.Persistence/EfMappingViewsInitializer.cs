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
    public class EfMappingViewsInitializer
    {
        public IEnumerable<string> Dependencies => null;

        private readonly EfMappingViewsFileStore _efMappingViewsFileStore;
        private readonly ILogger _log;
        private readonly ILogger _performanceLogger;
        private readonly IMetadataWorkspaceFileLoader _metadataWorkspaceFileLoader;

        public EfMappingViewsInitializer(EfMappingViewsFileStore efMappingViewsFileStore, IMetadataWorkspaceFileLoader metadataWorkspaceFileLoader, ILogProvider logProvider)
        {
            _efMappingViewsFileStore = efMappingViewsFileStore;
            _log = logProvider.GetLogger(nameof(EfMappingViewsInitializer));
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(EfMappingViewsInitializer));
            _metadataWorkspaceFileLoader = metadataWorkspaceFileLoader;
        }

        public void Initialize()
        {
            GenerateAndSaveViews();
        }

        private void GenerateAndSaveViews()
        {
            var sw = Stopwatch.StartNew();

            var mappingCollection = (StorageMappingItemCollection)_metadataWorkspaceFileLoader.LoadFromFiles().GetItemCollection(DataSpace.CSSpace);

            var hash = mappingCollection.ComputeMappingHashValue();
            _performanceLogger.Write(sw, () => $"Calculate hash for current model.");
            var currentViewCache = _efMappingViewsFileStore.Load();

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

            _efMappingViewsFileStore.Save(newViewCache);
        }
    }
}
