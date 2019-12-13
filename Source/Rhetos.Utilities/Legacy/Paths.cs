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
    [Obsolete("Use RhetosAppOptions or BuildOptions instead.")]
    public static class Paths
    {
        private static string _rootPath;
        private static RhetosAppOptions _appOptions;
        private static BuildOptions _buildOptions;

        /// <summary>
        /// Initialize Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(string rootPath, RhetosAppOptions appOptions, BuildOptions buildOptions)
        {
            _rootPath = rootPath;
            _appOptions = appOptions;
            _buildOptions = buildOptions;
        }

        public static string RhetosServerRootPath => NonNullRhetosRootPath;
        public static string PackagesCacheFolder => Path.Combine(NonNullRhetosRootPath, "PackagesCache");
        public static string ResourcesFolder => Path.Combine(NonNullRhetosRootPath, "Resources");
        public static string BinFolder => NonNullRhetosAppOptions.BinFolder;
        public static string GeneratedFolder => NotNullGeneratedFolder;
        public static string GeneratedFilesCacheFolder => Path.Combine(NonNullRhetosRootPath, "GeneratedFilesCache");
        public static string PluginsFolder => Path.Combine(NonNullRhetosRootPath, "bin\\Plugins");
        public static string RhetosServerWebConfigFile => Path.Combine(NonNullRhetosRootPath, "Web.config");
        public static string ConnectionStringsFile => Path.Combine(NonNullRhetosRootPath, @"bin\ConnectionStrings.config");
        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(NotNullGeneratedFolder, $"ServerDom.{domAssembly}.dll");
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));

        private static void AssertRhetosAppOptionsNotNull()
        {
            if (_appOptions == null)
                throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities" +
                    $" or use {nameof(RhetosAppOptions)}.");
        }

        private static void AssertBuildOptionsNotNull()
        {
            if (_appOptions == null)
                throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities" +
                    $" or use {nameof(BuildOptions)}.");
        }

        private static void AssertRhetosRootPathNotNull()
        {
            if (_appOptions == null)
                throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities");
        }

        private static void ValidateGeneratedFolder()
        {
            if (_appOptions == null && _buildOptions == null)
                throw new FrameworkException($@"One of the following value should be set. {nameof(RhetosAppOptions.AssetsFolder)} or {nameof(BuildOptions.GeneratedAssetsFolder)}");

            if (_appOptions != null && _buildOptions != null && _appOptions.AssetsFolder != _buildOptions.GeneratedAssetsFolder)
                throw new FrameworkException($@"Invalid initialization of class {nameof(Paths)}. The value of {nameof(RhetosAppOptions.AssetsFolder)} and {nameof(BuildOptions.GeneratedAssetsFolder)} should be equal.");
        }

        private static RhetosAppOptions NonNullRhetosAppOptions
        {
            get
            {
                AssertRhetosAppOptionsNotNull();
                return _appOptions;
            }
        }

        private static BuildOptions NonNullBuildOptions
        {
            get
            {
                AssertBuildOptionsNotNull();
                return _buildOptions;
            }
        }

        private static string NonNullRhetosRootPath
        {
            get
            {
                AssertRhetosRootPathNotNull();
                return _rootPath;
            }
        }

        private static string NotNullGeneratedFolder
        {
            get
            {
                ValidateGeneratedFolder();
                if (_appOptions.AssetsFolder != null)
                    return _appOptions.AssetsFolder;
                else
                    return _buildOptions.GeneratedAssetsFolder;
            }
        }
    }
}
