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
