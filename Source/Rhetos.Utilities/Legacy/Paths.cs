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
        private static RhetosAppEnvironment _rhetosAppEnvironment;

        /// <summary>
        /// Initialize Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(RhetosAppEnvironment rhetosAppEnvironment)
        {
            _rhetosAppEnvironment = rhetosAppEnvironment;
        }

        public static string RhetosServerRootPath => Path.GetFullPath(Directory.GetParent(SafeGetAppEnvironment().AssemblyFolder).FullName)
                ?? throw new FrameworkException($"'{nameof(RhetosAppEnvironment.AssemblyFolder)}' is expected to be configured with valid value, but is empty.");

        public static string ResourcesFolder => SafeGetAppEnvironment().LegacyAssetsFolder
                ?? throw new FrameworkException($"'{nameof(RhetosAppEnvironment.LegacyAssetsFolder)}' is expected to be configured with valid value, but is empty.");

        public static string BinFolder => SafeGetAppEnvironment().AssemblyFolder
                ?? throw new FrameworkException($"'{nameof(RhetosAppEnvironment.AssemblyFolder)}' is expected to be configured with valid value, but is empty.");

        public static string GeneratedFolder => SafeGetAppEnvironment().AssetsFolder
                ?? throw new FrameworkException($"'{nameof(RhetosAppEnvironment.AssetsFolder)}' is expected to be configured with valid value, but is empty.");

        public static string PluginsFolder => SafeGetAppEnvironment().LegacyPluginsFolder
                ?? throw new FrameworkException($"'{nameof(RhetosAppEnvironment.LegacyPluginsFolder)}' is expected to be configured with valid value, but is empty.");

        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");

        /// <summary>
        /// List of the generated DLL files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));

        private static RhetosAppEnvironment SafeGetAppEnvironment()
        {
            return _rhetosAppEnvironment
                ?? throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities.");
        }
    }
}
