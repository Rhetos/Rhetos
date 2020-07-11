using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.EntitySql;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.Persistence
{
    public class EfMappingViewsFileStore
    {
        private static readonly string _viewCacheFilename = "EfMappingGeneratedViews.json";
        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly ILogger _performanceLogger;

        public EfMappingViewsFileStore(RhetosAppOptions rhetosAppOptions, ILogProvider logProvider)
        {
            _rhetosAppOptions = rhetosAppOptions;
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(EfMappingViewsFileStore));
        }

        public EfMappingViews Load()
        {
            if (!File.Exists(FullCachePath.Value))
                return null;

            var sw = Stopwatch.StartNew();
            var jsonText = File.ReadAllText(FullCachePath.Value);
            var views = JsonConvert.DeserializeObject<EfMappingViews>(jsonText);
            _performanceLogger.Write(sw, () => $"Loaded and deserialized views from '{FullCachePath.Value}'.");

            return views;
        }

        public void Save(EfMappingViews views)
        {
            var sw = Stopwatch.StartNew();
            var jsonText = JsonConvert.SerializeObject(views);
            File.WriteAllText(FullCachePath.Value, jsonText);
            _performanceLogger.Write(sw, () => $"Serialized and saved views to '{FullCachePath.Value}'.");
        }

        private Lazy<string> FullCachePath
            => new Lazy<string>(() => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_rhetosAppOptions.RhetosRuntimePath), _viewCacheFilename)));
    }
}
