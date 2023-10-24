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

using System;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.MappingViews;
using System.Threading.Tasks;
using Rhetos.Logging;

namespace Rhetos.Persistence
{
    public class EfMappingViewCacheFactory : DbMappingViewCacheFactory
    {
        private readonly ILogger _logger;
        private readonly EfMappingViewsFileStore _mappingViewsFileStore;
        private readonly Lazy<EfMappingViewsInitializer> _efMappingViewsInitializer;
        private readonly object _buildCacheLock = new object();
        private bool _buildCacheExecuted = false;
        private readonly Lazy<EfMappingViewCache> _viewCache;

        public EfMappingViewCacheFactory(EfMappingViewsFileStore mappingViewsFileStore, ILogProvider logProvider, Lazy<EfMappingViewsInitializer> efMappingViewsInitializer)
        {
            _mappingViewsFileStore = mappingViewsFileStore;
            _efMappingViewsInitializer = efMappingViewsInitializer;
            _logger = logProvider.GetLogger(nameof(EfMappingViewCacheFactory));
            _viewCache = new Lazy<EfMappingViewCache>(CreateEfMappingViewCache);
        }
        
        public override DbMappingViewCache Create(string conceptualModelContainerName, string storeModelContainerName)
        {
            return _viewCache.Value;
        }

        private EfMappingViewCache CreateEfMappingViewCache()
        {
            var views = _mappingViewsFileStore.Load(onlyIfNewerThanApp: true);
            if (views == null)
            {
                _logger.Info(() => $"Pre-generated mapping views not found or older then the app.");
                Task.Run(() => BuildViewCache()); // Generate cached mapping views in backgound, to optimize the next startup.
                return null; // Returing 'null' since the cached mapping views are not found. It will result with a slower initial startup of Entity Framework.
            }

            return new EfMappingViewCache(views);
        }

        void BuildViewCache()
        {
            if (!_buildCacheExecuted)
                lock (_buildCacheLock)
                {
                    if (!_buildCacheExecuted)
                    {
                        _logger.Info(() => $"Building the mapping views cache in background.");
                        try
                        {
                            _efMappingViewsInitializer.Value.Initialize();
                        }
                        catch (Exception ex)
                        {
                            // There might be some issues with file write permissions.
                            // The error can be ignored, since the application can run successfully without the cache (with slower starup).
                            _logger.Warning(() => $"Error while building the mapping views cache. {ex.GetType()}: {ex.Message}");
                        }
                    }
                    _buildCacheExecuted = true;
                }
        }

        public void RegisterFactoryForWorkspace(MetadataWorkspace metadataWorkspace)
        {
            var storageMappingItemCollection = (StorageMappingItemCollection)metadataWorkspace.GetItemCollection(DataSpace.CSSpace);
            storageMappingItemCollection.MappingViewCacheFactory = this;

            _logger.Trace(() => $"Registered {nameof(EfMappingViewCacheFactory)} to {nameof(MetadataWorkspace)}.");
        }
    }
}
