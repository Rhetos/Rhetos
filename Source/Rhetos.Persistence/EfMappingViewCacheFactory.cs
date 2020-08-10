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
