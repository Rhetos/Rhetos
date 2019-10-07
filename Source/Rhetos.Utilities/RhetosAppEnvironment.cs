﻿using Rhetos.Dom;
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
        private readonly Lazy<string> _packagesCacheFolder;
        private readonly Lazy<string> _resourcesFolder;
        private readonly Lazy<string> _binFolder;
        private readonly Lazy<string> _generatedFolder;
        private readonly Lazy<string> _generatedFilesCacheFolder;
        private readonly Lazy<string> _pluginsFolder;

        public RhetosAppEnvironment(RhetosAppOptions options)
        {
            RootPath = options.RootPath;
            _packagesCacheFolder = new Lazy<string>(() => Path.Combine(RootPath, "PackagesCache"));
            _resourcesFolder = new Lazy<string>(() => Path.Combine(RootPath, "Resources"));
            _binFolder = new Lazy<string>(() => Path.Combine(RootPath, "bin"));
            _generatedFolder = new Lazy<string>(() => Path.Combine(RootPath, "bin\\Generated"));
            _generatedFilesCacheFolder = new Lazy<string>(() => Path.Combine(RootPath, "GeneratedFilesCache"));
            _pluginsFolder = new Lazy<string>(() => Path.Combine(RootPath, "bin\\Plugins"));
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