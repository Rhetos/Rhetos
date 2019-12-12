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

using Rhetos.Dom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    [Obsolete("Use RhetosAppEnvironment instead.")]
    public static class Paths
    {
        private static RhetosAppOptions _rhetosAppOptions;
        private static string _rhetosRootPath;

        /// <summary>
        /// Initialize Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(IConfigurationProvider configurationProvider)
        {
            _rhetosAppOptions = configurationProvider.GetOptions<RhetosAppOptions>();
            _rhetosRootPath = configurationProvider.GetValue<string>("RootPath");
            if (_rhetosAppOptions == null) 
                throw new ArgumentNullException(nameof(RhetosAppOptions), "Can't initialize utility with null RhetosAppEnvironment.");
        }

        public static string RhetosServerRootPath => _rhetosRootPath;
        public static string PackagesCacheFolder => Paths.GetPackagesCacheFolder(_rhetosRootPath);
        public static string ResourcesFolder => Paths.GetResourcesFolder(_rhetosRootPath);
        public static string BinFolder => NonNullRhetosAppOptions.BinFolder;
        public static string GeneratedFolder => NonNullRhetosAppOptions.AssetsFolder;
        public static string GeneratedFilesCacheFolder => Paths.GetGeneratedFilesCacheFolder(_rhetosRootPath);
        public static string PluginsFolder => Paths.GetPluginsFolder(_rhetosRootPath);
        public static string RhetosServerWebConfigFile => Path.Combine(_rhetosRootPath, "Web.config");
        public static string ConnectionStringsFile => Path.Combine(_rhetosRootPath, @"bin\ConnectionStrings.config");
        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));

        public static string GetResourcesFolder(string rootFolder)
        {
            return Path.Combine(rootFolder, "Resources");
        }

        public static string GetPluginsFolder(string rootFolder)
        {
            return Path.Combine(rootFolder, "bin\\Plugins");
        }

        public static string GetPackagesCacheFolder(string rootFolder)
        {
            return Path.Combine(rootFolder, "PackagesCache");
        }

        public static string GetGeneratedFolder(string rootFolder)
        {
            return Path.Combine(rootFolder, "bin\\Generated");
        }

        public static string GetBinFolder(string rootFolder)
        {
            return Path.Combine(rootFolder, "bin");
        }

        public static string GetGeneratedFilesCacheFolder(string rootFolder)
        {
            return Path.Combine(rootFolder, "GeneratedFilesCache");
        }

        private static void AssertRhetosAppEnvironmentNotNull()
        {
            if (_rhetosAppOptions == null)
                throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities" +
                    $" or use {nameof(RhetosAppOptions)}.");
        }

        private static RhetosAppOptions NonNullRhetosAppOptions
        {
            get
            {
                AssertRhetosAppEnvironmentNotNull();
                return _rhetosAppOptions;
            }
        }
    }
}
