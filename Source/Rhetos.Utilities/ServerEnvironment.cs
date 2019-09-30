using Rhetos.Dom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class ServerEnvironment
    {
        public string RhetosServerRootPath { get; }
        private readonly Lazy<string> _packagesCacheFolder;
        private readonly Lazy<string> _resourcesFolder;
        private readonly Lazy<string> _binFolder;
        private readonly Lazy<string> _generatedFolder;
        private readonly Lazy<string> _generatedFilesCacheFolder;
        private readonly Lazy<string> _pluginsFolder;

        public ServerEnvironment(RhetosOptions options)
        {
            RhetosServerRootPath = options.RhetosServerRootPath;
            _packagesCacheFolder = new Lazy<string>(() => Path.Combine(RhetosServerRootPath, "PackagesCache"));
            _resourcesFolder = new Lazy<string>(() => Path.Combine(RhetosServerRootPath, "Resources"));
            _binFolder = new Lazy<string>(() => Path.Combine(RhetosServerRootPath, "bin"));
            _generatedFolder = new Lazy<string>(() => Path.Combine(RhetosServerRootPath, "bin\\Generated"));
            _generatedFilesCacheFolder = new Lazy<string>(() => Path.Combine(RhetosServerRootPath, "GeneratedFilesCache"));
            _pluginsFolder = new Lazy<string>(() => Path.Combine(RhetosServerRootPath, "bin\\Plugins"));
        }

        public string PackagesCacheFolder => _packagesCacheFolder.Value;
        public string ResourcesFolder => _resourcesFolder.Value;
        public string BinFolder => _binFolder.Value;
        public string GeneratedFolder => _generatedFolder.Value;
        public string GeneratedFilesCacheFolder => _generatedFilesCacheFolder.Value;
        public string PluginsFolder => _pluginsFolder.Value;

        public string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));
    }
}
