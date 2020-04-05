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

namespace Rhetos.Utilities
{
    [Obsolete("Use RhetosAppEnvironment instead.")]
    public static class Paths
    {
        private static string _rhetosServerRootPath;
        private static string _binFolder;
        private static string _generatedFolder;
        private static string _pluginsFolder;
        private static string _resourcesFolder;
        private static string _environment;

        /// <summary>
        /// Initialize legacy Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(IConfigurationProvider configurationProvider)
        {
            var runtimeEnvironment = configurationProvider.GetOptions<RhetosAppEnvironment>();
            var buildEnvironment = configurationProvider.GetOptions<RhetosBuildEnvironment>();
            var legacyPaths = configurationProvider.GetOptions<LegacyPathsOptions>();

            _rhetosServerRootPath = buildEnvironment.ProjectFolder ?? runtimeEnvironment.ApplicationRootFolder;
            _binFolder = legacyPaths.BinFolder ?? runtimeEnvironment.AssemblyFolder;
            _generatedFolder = buildEnvironment.GeneratedAssetsFolder ?? runtimeEnvironment.AssetsFolder;
            _pluginsFolder = legacyPaths.PluginsFolder;
            _resourcesFolder = legacyPaths.ResourcesFolder;

            if (buildEnvironment?.ProjectFolder != null)
                _environment = "build";
            else if (runtimeEnvironment?.ApplicationRootFolder != null)
                _environment = "run-time";
            else
                _environment = "unspecified";
        }

        public static string RhetosServerRootPath => PathOrError(_rhetosServerRootPath, "RhetosServerRootPath");

        public static string ResourcesFolder => AbsolutePathOrError(_resourcesFolder, "ResourcesFolder");

        public static string BinFolder => AbsolutePathOrError(_binFolder, "BinFolder");

        public static string GeneratedFolder => AbsolutePathOrError(_generatedFolder, "GeneratedFolder");

        public static string PluginsFolder => AbsolutePathOrError(_pluginsFolder, "PluginsFolder");

        private static string PathOrError(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
                throw new FrameworkException($"Paths property '{name}' is not configured in '{_environment}' environment.");
            return path;
        }

        private static string AbsolutePathOrError(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
                throw new FrameworkException($"Paths property '{name}' is not configured in '{_environment}' environment.");
            if (string.IsNullOrEmpty(_rhetosServerRootPath))
            {
                if (path == Path.GetFullPath(path))
                    return path;
                else
                    throw new FrameworkException($"Paths property '{name}' is not configured correctly in '{_environment}' environment." +
                        $" Specified relative path '{path}' without known root folder ({nameof(RhetosServerRootPath)}).");
            }
            return Path.GetFullPath(Path.Combine(_rhetosServerRootPath, path));
        }

        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");

        /// <summary>
        /// List of the generated DLL files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));
    }
}
