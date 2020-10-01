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
    [Obsolete("Use RhetosAppOptions and RhetosAppEnvironment for run-time folders. Use RhetosBuildEnvironment for build-time folder. Use IAssetsOptions.AssetsFolder for files generated at build-time and read at run-time.")]
    public static class Paths
    {
        private static string _rhetosServerRootPath;
        private static string _binFolder;
        private static string _generatedFolder;
        private static string _pluginsFolder;
        private static string _resourcesFolder;
        private static string _environment;
        private static Lazy<string[]> _domAssemblyFiles;

        /// <summary>
        /// Initialize legacy Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            var runtimeEnvironment = configuration.GetOptions<RhetosAppEnvironment>();
            var runtimeOptions = configuration.GetOptions<RhetosAppOptions>();
            var buildEnvironment = configuration.GetOptions<RhetosBuildEnvironment>();
            var legacyPaths = configuration.GetOptions<LegacyPathsOptions>();

            string runtimeAssemblyFolder = !string.IsNullOrEmpty(runtimeOptions.RhetosRuntimePath)
                ? Path.GetDirectoryName(runtimeOptions.RhetosRuntimePath) : null;

            _rhetosServerRootPath = buildEnvironment.ProjectFolder ?? runtimeEnvironment.ApplicationRootFolder;
            _binFolder = legacyPaths.BinFolder ?? runtimeAssemblyFolder;
            _generatedFolder = buildEnvironment.GeneratedAssetsFolder ?? runtimeOptions.AssetsFolder;
            _pluginsFolder = legacyPaths.PluginsFolder ?? runtimeAssemblyFolder;
            _resourcesFolder = legacyPaths.ResourcesFolder;

            if (buildEnvironment?.ProjectFolder != null)
                _environment = "build";
            else if (runtimeEnvironment?.ApplicationRootFolder != null)
                _environment = "run-time";
            else
                _environment = "unspecified";

            _domAssemblyFiles = new Lazy<string[]>(() => string.IsNullOrEmpty(buildEnvironment.GeneratedSourceFolder)
                ? Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(GetDomAssemblyFile).ToArray()
                : Array.Empty<string>());
        }

        public static string RhetosServerRootPath => PathOrError(_rhetosServerRootPath, "RhetosServerRootPath");

        public static string ResourcesFolder => AbsolutePathOrError(_resourcesFolder, "ResourcesFolder");

        public static string BinFolder => AbsolutePathOrError(_binFolder, "BinFolder");

        [Obsolete("If generating and reading assets files use IAssetsOptions.AssetsFolder instead. If used within SearchForAssembly, migrate to Rhetos.ProcessContainer instead.")]
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
        /// Empty for applications built with Rhetos CLI, as they does not generate the ServerDom assemblies
        /// (only source, compiled into the main application).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => _domAssemblyFiles.Value;
    }
}
