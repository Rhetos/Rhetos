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

using Rhetos.Logging;
using Rhetos.Utilities;
using System.Diagnostics;
using System.IO;

namespace Rhetos.Persistence
{
    public class EfMappingViewsFileStore
    {
        const string _cacheFilename = "EfMappingGeneratedViews.json";
        private readonly ILogger _performanceLogger;
        private readonly string _cacheFilePath;

        public EfMappingViewsFileStore(RhetosAppOptions rhetosAppOptions, ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(EfMappingViewsFileStore));
            _cacheFilePath = Path.GetFullPath(Path.Combine(rhetosAppOptions.CacheFolder, _cacheFilename));
        }

        public EfMappingViews Load()
        {
            if (!File.Exists(_cacheFilePath))
                return null;

            var sw = Stopwatch.StartNew();
            var views = JsonUtility.DeserializeFromFile<EfMappingViews>(_cacheFilePath);
            _performanceLogger.Write(sw, () => $"Loaded and deserialized views from '{_cacheFilePath}'.");

            return views;
        }

        public void Save(EfMappingViews views)
        {
            var sw = Stopwatch.StartNew();
            JsonUtility.SerializeToFile(views, _cacheFilePath);
            _performanceLogger.Write(sw, () => $"Serialized and saved views to '{_cacheFilePath}'.");
        }
    }
}
