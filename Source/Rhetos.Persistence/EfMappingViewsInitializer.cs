using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Linq;
using Rhetos.Logging;

namespace Rhetos.Persistence
{
    public class EfMappingViewsInitializer
    {
        private readonly EfMappingViewsFileStore _efMappingViewsFileStore;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IMetadataWorkspaceFileProvider _metadataWorkspaceFileProvider;

        public EfMappingViewsInitializer(EfMappingViewsFileStore efMappingViewsFileStore, IMetadataWorkspaceFileProvider metadataWorkspaceFileProvider, ILogProvider logProvider)
        {
            _efMappingViewsFileStore = efMappingViewsFileStore;
            _logger = logProvider.GetLogger(nameof(EfMappingViewsInitializer));
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(EfMappingViewsInitializer));
            _metadataWorkspaceFileProvider = metadataWorkspaceFileProvider;
        }

        public void Initialize()
        {
            GenerateAndSaveViews();
        }

        private void GenerateAndSaveViews()
        {
            var sw = Stopwatch.StartNew();

            var mappingCollection = (StorageMappingItemCollection)_metadataWorkspaceFileProvider.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);

            var hash = mappingCollection.ComputeMappingHashValue();
            _performanceLogger.Write(sw, () => $"Calculate hash for current model.");
            var currentViewCache = _efMappingViewsFileStore.Load();

            if (!string.IsNullOrEmpty(currentViewCache?.Hash) && currentViewCache.Hash == hash)
            {
                _logger.Trace(() => $"Hash not changed. View cache is valid. Skipping generation.");
                return;
            }

            sw.Restart();
            var errors = new List<EdmSchemaError>();
            var newViews = mappingCollection.GenerateViews(errors)
                .ToDictionary(a => EfMappingViewCache.GetExtentKey(a.Key), a => a.Value.EntitySql);

            foreach (var edmSchemaError in errors)
                _logger.Warning(() => $"{edmSchemaError}");

            var newViewCache = new EfMappingViews() {Hash = hash, Views = newViews};
            _performanceLogger.Write(sw, () => $"Generated new views. Old hash != new hash ('{currentViewCache?.Hash}' != '{hash}').");

            _efMappingViewsFileStore.Save(newViewCache);
        }
    }
}
