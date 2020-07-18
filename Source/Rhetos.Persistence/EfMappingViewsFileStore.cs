using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.Persistence
{
    public class EfMappingViewsFileStore
    {
        private static readonly string _viewCacheFilename = "EfMappingGeneratedViews.json";
        private readonly ILogger _performanceLogger;
        private readonly Lazy<string> _fullCachePath;

        public EfMappingViewsFileStore(RhetosAppOptions rhetosAppOptions, ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(EfMappingViewsFileStore));
            _fullCachePath = new Lazy<string>(() => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(rhetosAppOptions.RhetosRuntimePath), _viewCacheFilename)));
        }

        public EfMappingViews Load()
        {
            if (!File.Exists(_fullCachePath.Value))
                return null;

            var sw = Stopwatch.StartNew();
            var jsonText = File.ReadAllText(_fullCachePath.Value);
            var views = JsonConvert.DeserializeObject<EfMappingViews>(jsonText);
            _performanceLogger.Write(sw, () => $"Loaded and deserialized views from '{_fullCachePath.Value}'.");

            return views;
        }

        public void Save(EfMappingViews views)
        {
            var sw = Stopwatch.StartNew();
            var jsonText = JsonConvert.SerializeObject(views);
            File.WriteAllText(_fullCachePath.Value, jsonText);
            _performanceLogger.Write(sw, () => $"Serialized and saved views to '{_fullCachePath.Value}'.");
        }
    }
}
