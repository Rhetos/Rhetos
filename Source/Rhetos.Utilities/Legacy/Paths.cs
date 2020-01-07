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
        private static AssetsOptions _assetsOptions;

        /// <summary>
        /// Initialize Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(string rootPath, RhetosAppOptions appOptions, BuildOptions buildOptions, AssetsOptions assetsOptions)
        {
            _rootPath = rootPath;
            _appOptions = appOptions;
            _buildOptions = buildOptions;
            _assetsOptions = assetsOptions;
        }

        public static string RhetosServerRootPath => NonNullRhetosRootPath;
        public static string ResourcesFolder => Path.Combine(NonNullRhetosRootPath, "Resources");
        public static string BinFolder => NonNullRhetosAppOptions.BinFolder;
        public static string GeneratedFolder => NotNullGeneratedFolder;
        public static string GeneratedFilesCacheFolder => NonNullBuildOptions.GeneratedFilesCacheFolder;
        public static string PluginsFolder => Path.Combine(NonNullRhetosRootPath, "bin\\Plugins");
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
            if (_rootPath == null)
                throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities");
        }

        private static void ValidateGeneratedFolder()
        {
            if (_assetsOptions == null)
                throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities");

            if (string.IsNullOrEmpty(_assetsOptions.AssetsFolder))
                throw new FrameworkException($"{nameof(AssetsOptions.AssetsFolder)} expected to be configured with valid value, but is empty.");
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
                return _assetsOptions.AssetsFolder;
            }
        }
    }
}
