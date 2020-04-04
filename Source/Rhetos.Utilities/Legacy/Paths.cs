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
        private static string _environment;
        private static string _rhetosServerRootPath;
        private static string _binFolder;
        private static string _generatedFolder;
        private static string _pluginsFolder;

        /// <summary>
        /// Initialize legacy Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(IConfigurationProvider configurationProvider)
        {
            var runtimeEnvironment = configurationProvider.GetOptions<RhetosAppEnvironment>();
            var buildEnvironment = configurationProvider.GetOptions<RhetosBuildEnvironment>();

            if (buildEnvironment?.ProjectFolder != null)
                Initialize(buildEnvironment);
            else if (runtimeEnvironment?.ApplicationRootFolder != null)
                Initialize(runtimeEnvironment);
            else
                InitializeNone();
        }

        private static void Initialize(RhetosBuildEnvironment buildEnvironment)
        {
            _environment = "build";
            _rhetosServerRootPath = buildEnvironment.ProjectFolder;
            _binFolder = null;
            _generatedFolder = buildEnvironment.GeneratedAssetsFolder;
            _pluginsFolder = null;
        }

        private static void Initialize(RhetosAppEnvironment runtimeEnvironment)
        {
            _environment = "run-time";
            _rhetosServerRootPath = runtimeEnvironment.ApplicationRootFolder;
            _binFolder = runtimeEnvironment.AssemblyFolder;
            _generatedFolder = runtimeEnvironment.AssetsFolder;
            _pluginsFolder = runtimeEnvironment.Legacy__PluginsFolder;
        }

        private static void InitializeNone()
        {
            _environment = "uninitialized";
            _rhetosServerRootPath = null;
            _binFolder = null;
            _generatedFolder = null;
            _pluginsFolder = null;
        }

        public static string RhetosServerRootPath => ErrorIfNull(_rhetosServerRootPath, "RhetosServerRootPath");

        public static string ResourcesFolder => Path.Combine(RhetosServerRootPath, "Resources");

        public static string BinFolder => ErrorIfNull(_binFolder, "BinFolder");

        public static string GeneratedFolder => ErrorIfNull(_generatedFolder, "GeneratedFolder");

        public static string PluginsFolder => ErrorIfNull(_pluginsFolder, "PluginsFolder");

        private static T ErrorIfNull<T>(T value, string name) where T : class
        {
            if (value is null)
                throw new FrameworkException($"Paths property '{name}' is not configured in {_environment} environment.");
            return value;
        }

        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");

        /// <summary>
        /// List of the generated DLL files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));
    }
}
