using System;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.MappingViews;
using Rhetos.Logging;

namespace Rhetos.Persistence
{
    public class EfMappingViewCacheFactory : DbMappingViewCacheFactory
    {
        private readonly ILogger _logger;
        private readonly EfMappingViewsFileStore _mappingViewsFileStore;
        private readonly Lazy<EfMappingViewCache> _viewCache;

        public EfMappingViewCacheFactory(EfMappingViewsFileStore mappingViewsFileStore, ILogProvider logProvider)
        {
            _mappingViewsFileStore = mappingViewsFileStore;
            _logger = logProvider.GetLogger(nameof(EfMappingViewCacheFactory));
            _viewCache = new Lazy<EfMappingViewCache>(CreateEfMappingViewCache);
        }
        
        public override DbMappingViewCache Create(string conceptualModelContainerName, string storeModelContainerName)
        {
            return _viewCache.Value;
        }

        private EfMappingViewCache CreateEfMappingViewCache()
        {
            var views = _mappingViewsFileStore.Load();
            if (views == null)
            {
                _logger.Warning(() => $"Pre-generated mapping views not found. This will result in slower startup performance.");
                return null;
            }

            return new EfMappingViewCache(views);
        }

        public void RegisterFactoryForWorkspace(MetadataWorkspace metadataWorkspace)
        {
            var storageMappingItemCollection = (StorageMappingItemCollection)metadataWorkspace.GetItemCollection(DataSpace.CSSpace);
            storageMappingItemCollection.MappingViewCacheFactory = this;

            _logger.Trace(() => $"Registered {nameof(EfMappingViewCacheFactory)} to {nameof(MetadataWorkspace)}.");
        }
    }
}
