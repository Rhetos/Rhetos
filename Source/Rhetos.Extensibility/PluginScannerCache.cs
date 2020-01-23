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

using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Rhetos.Extensibility
{
    public class PluginScannerCache : IGenerator
    {
        private const string _pluginScannerBuildCacheFilename = "PluginScanner.BuildCache.json";
        private const string _pluginScannerRuntimeCacheFilename = "PluginScanner.RuntimeCache.json";

        private readonly ILogger _logger;
        private readonly string _buildCacheFilePath;
        private readonly string _runtimeCacheFilePath;
        private readonly FilesUtility _filesUtility;

        public PluginScannerCache(BuildOptions buildOptions, RhetosAppEnvironment rhetosAppEnvironment, ILogProvider logProvider, FilesUtility filesUtility)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            if (buildOptions?.CacheFolder != null)
                _buildCacheFilePath = Path.Combine(buildOptions.CacheFolder, _pluginScannerBuildCacheFilename);
            _runtimeCacheFilePath = Path.Combine(rhetosAppEnvironment.AssetsFolder, _pluginScannerRuntimeCacheFilename);
            _filesUtility = filesUtility;
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        public void Generate()
        {
            // Copy build cache to runtime cache, to reuse it.
            if (File.Exists(_buildCacheFilePath))
            {
                _logger.Trace($"Copying build cache to runtime cache.");
                _filesUtility.SafeCopyFile(_buildCacheFilePath, _runtimeCacheFilePath);
            }
        }

        internal PluginsCacheData LoadPluginsCacheData()
        {
            string cacheFilePath = GetExistingCacheFile();
            if (cacheFilePath != null)
            {
                _logger.Trace($"Reading cache from '{cacheFilePath}'.");
                return JsonConvert.DeserializeObject<PluginsCacheData>(File.ReadAllText(cacheFilePath));
            }
            else
            {
                _logger.Trace($"Cache file not found.");
                return new PluginsCacheData();
            }
        }

        internal void SavePluginsCacheData(PluginsCacheData cache)
        {
            string cacheFilePath = GetExistingCacheFile() ?? _buildCacheFilePath ?? _runtimeCacheFilePath;

            _logger.Trace($"Writing cache to '{cacheFilePath}'.");
            _filesUtility.SafeCreateDirectory(Path.GetDirectoryName(cacheFilePath)); // Plugin scanner can be executed before other Rhetos components are initialized.
            File.WriteAllText(cacheFilePath, JsonConvert.SerializeObject(cache, Formatting.Indented));
        }

        private string GetExistingCacheFile()
        {
            if (File.Exists(_runtimeCacheFilePath))
                return _runtimeCacheFilePath;
            else if (File.Exists(_buildCacheFilePath))
                return _buildCacheFilePath;
            else
                return null;
        }
    }
}
