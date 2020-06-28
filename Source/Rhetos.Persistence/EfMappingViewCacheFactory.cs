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
    public class EfMappingViewCacheFactory : DbMappingViewCacheFactory, IEfMappingViewCacheFactory
    {
        private readonly ILogger _log;
        private readonly IEfMappingViewsStore _mappingViewsStore;
        private readonly Lazy<EfMappingViewCache> _viewCache;

        public EfMappingViewCacheFactory(IEfMappingViewsStore mappingViewsStore, ILogProvider logProvider)
        {
            _mappingViewsStore = mappingViewsStore;
            _log = logProvider.GetLogger(nameof(EfMappingViewCacheFactory));
            _viewCache = new Lazy<EfMappingViewCache>(() => CreateEfMappingViewCache());
        }
        
        public override DbMappingViewCache Create(string conceptualModelContainerName, string storeModelContainerName)
        {
            return _viewCache.Value;
        }

        private EfMappingViewCache CreateEfMappingViewCache()
        {
            var views = _mappingViewsStore.Load();
            if (views == null)
            {
                _log.Warning(() => $"Pre-generated mapping views not found. This will result in slower startup performance.");
                return null;
            }

            return new EfMappingViewCache(views);
        }

        public void RegisterFactoryForContext(IPersistenceCache persistenceCache)
        {
            if (!(persistenceCache is DbContext dbContext))
            {
                throw new FrameworkException($"Unable to register {nameof(DbMappingViewCacheFactory)} to context of type {persistenceCache.GetType().Name}. Expected type {nameof(DbContext)}.");
            }

            var objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            var storageMappingItemCollection = (StorageMappingItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
            storageMappingItemCollection.MappingViewCacheFactory = this;
            _log.Trace(() => $"Registered {nameof(EfMappingViewCacheFactory)} to {persistenceCache.GetType().Name} context object.");
        }
    }
}
