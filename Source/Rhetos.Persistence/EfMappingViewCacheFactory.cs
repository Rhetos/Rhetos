using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.MappingViews;
using System.Diagnostics;
using System.Security.Policy;
using System.Text;
using System.Threading;
using Rhetos.Logging;

namespace Rhetos.Persistence
{
    public class EfMappingViewCacheFactory : DbMappingViewCacheFactory
    {
        private readonly ILogger _log;
        private readonly EfMappingViewsFileStore _mappingViewsFileStore;
        private readonly Lazy<EfMappingViewCache> _viewCache;

        public EfMappingViewCacheFactory(EfMappingViewsFileStore mappingViewsFileStore, ILogProvider logProvider)
        {
            _mappingViewsFileStore = mappingViewsFileStore;
            _log = logProvider.GetLogger(nameof(EfMappingViewCacheFactory));
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
                _log.Warning(() => $"Pre-generated mapping views not found. This will result in slower startup performance.");
                return null;
            }

            return new EfMappingViewCache(views);
        }

        public void RegisterFactoryForWorkspace(MetadataWorkspace metadataWorkspace)
        {
            var storageMappingItemCollection = (StorageMappingItemCollection)metadataWorkspace.GetItemCollection(DataSpace.CSSpace);
            storageMappingItemCollection.MappingViewCacheFactory = this;

            _log.Trace(() => $"Registered {nameof(EfMappingViewCacheFactory)} to {nameof(MetadataWorkspace)}.");
        }
    }
}
