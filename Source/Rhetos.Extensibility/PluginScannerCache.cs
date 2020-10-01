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
    public class PluginScannerCache
    {
        private const string _pluginScannerCacheFilename = "PluginScanner.json";

        private readonly string _cacheFilePath;
        private readonly ILogger _logger;
        private readonly FilesUtility _filesUtility;

        public PluginScannerCache(string cacheFolder, ILogProvider logProvider, FilesUtility filesUtility)
        {
            _cacheFilePath = Path.Combine(cacheFolder, _pluginScannerCacheFilename);
            _logger = logProvider.GetLogger(GetType().Name);
            _filesUtility = filesUtility;
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        internal PluginsCacheData LoadPluginsCacheData()
        {
            if (File.Exists(_cacheFilePath))
            {
                _logger.Trace($"Reading cache from '{_cacheFilePath}'.");
                return JsonUtility.DeserializeFromFile<PluginsCacheData>(_cacheFilePath);
            }
            else
            {
                _logger.Trace($"Cache file not found.");
                return new PluginsCacheData();
            }
        }

        internal void SavePluginsCacheData(PluginsCacheData cache)
        {

            _logger.Trace($"Writing cache to '{_cacheFilePath}'.");
            _filesUtility.SafeCreateDirectory(Path.GetDirectoryName(_cacheFilePath)); // Plugin scanner can be executed before other Rhetos components are initialized.
            JsonUtility.SerializeToFile(cache, _cacheFilePath, Formatting.Indented);
        }
    }
}
