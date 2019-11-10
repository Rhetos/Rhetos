using Rhetos.Dom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class RhetosAppEnvironment
    {
        public string RootPath { get; }
        public string PackagesCacheFolder { get; }
        public string ResourcesFolder { get; }
        public string BinFolder { get; }
        public string GeneratedFolder { get; }
        public string GeneratedFilesCacheFolder { get; }
        public string PluginsFolder { get; }

        public RhetosAppEnvironment(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
                throw new FrameworkException($"Can't initialize {nameof(RhetosAppEnvironment)}. RootPath is null or not configured.");

            RootPath = Path.GetFullPath(rootPath);
            PackagesCacheFolder = Path.Combine(RootPath, "PackagesCache");
            ResourcesFolder = Path.Combine(RootPath, "Resources");
            BinFolder = Path.Combine(RootPath, "bin");
            GeneratedFolder = Path.Combine(RootPath, "bin\\Generated");
            GeneratedFilesCacheFolder = Path.Combine(RootPath, "GeneratedFilesCache");
            PluginsFolder = Path.Combine(RootPath, "bin\\Plugins");
        }

        public string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));
    }
}
