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

using Rhetos.MsSqlEf6;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Persistence
{
    /// <summary>
    /// This class is used both at *dbupdate* and at *runtime*, to initialize the EF views cache.
    /// 1. At dbupdate it is used as an <see cref="IServerInitializer"/>, but it has specifically set <see cref="DbUpdateOptions.OverrideServerInitializerOrdering"></see>
    /// to execute before other initializers, see <see cref="AutofacModuleConfigurationEf6"/>.
    /// 2. At runtime it will be used to initialized the EF cache in background if it has not been initialized earlier, <see cref="EfMappingViewCacheFactory"/>.
    /// This typically happens for example if deploying the Rhetos app on multiple servers, and then running dbupdate only on the first server.
    /// </summary>
    [Export(typeof(IServerInitializer))]
    public class EfMappingViewsInitializer : IServerInitializer
    {
        private readonly EfMappingViewsFileStore _efMappingViewsFileStore;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IMetadataWorkspaceFileProvider _metadataWorkspaceFileProvider;
        private readonly IEfMappingViewsHash _efMappingViewsHash;

        public EfMappingViewsInitializer(EfMappingViewsFileStore efMappingViewsFileStore, IMetadataWorkspaceFileProvider metadataWorkspaceFileProvider, ILogProvider logProvider, IEfMappingViewsHash efMappingViewsHash)
        {
            _efMappingViewsFileStore = efMappingViewsFileStore;
            _logger = logProvider.GetLogger(nameof(EfMappingViewsInitializer));
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(EfMappingViewsInitializer));
            _metadataWorkspaceFileProvider = metadataWorkspaceFileProvider;
            _efMappingViewsHash = efMappingViewsHash;
        }

        public IEnumerable<string> Dependencies => null;

        public void Initialize()
        {
            GenerateAndSaveViews();
        }

        private void GenerateAndSaveViews()
        {
            var sw = Stopwatch.StartNew();

            var mappingCollection = (StorageMappingItemCollection)_metadataWorkspaceFileProvider.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
            var hash = mappingCollection.ComputeMappingHashValue();
            _performanceLogger.Write(sw, () => $"Calculated hash for current model.");
            string additionalHash = _efMappingViewsHash.GetAdditionalHash();
            _performanceLogger.Write(sw, () => $"Calculated additional hash.");

            var currentViewCache = _efMappingViewsFileStore.Load(onlyIfNewerThanApp: false);
            if (!string.IsNullOrEmpty(currentViewCache?.Hash)
                && currentViewCache.Hash == hash
                && currentViewCache.AdditionalHash == additionalHash)
            {
                _logger.Trace(() => $"Hash not changed. View cache is valid. Skipping generation.");
                _efMappingViewsFileStore.Touch();
                return;
            }

            sw.Restart();
            var errors = new List<EdmSchemaError>();
            var newViews = mappingCollection.GenerateViews(errors)
                .ToDictionary(a => EfMappingViewCache.GetExtentKey(a.Key), a => a.Value.EntitySql);

            foreach (var edmSchemaError in errors)
                _logger.Warning(() => $"{edmSchemaError}");

            var newViewCache = new EfMappingViews { Hash = hash, Views = newViews, AdditionalHash = additionalHash };
            _performanceLogger.Write(sw, () => $"Generated new views. Old hash != new hash ('{currentViewCache?.Hash}' != '{hash}').");

            _efMappingViewsFileStore.Save(newViewCache);
        }
    }
}
