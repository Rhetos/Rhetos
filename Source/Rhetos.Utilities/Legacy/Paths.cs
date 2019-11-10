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
        private static RhetosAppEnvironment _rhetosAppEnvironment;

        /// <summary>
        /// Initialize Paths for the Rhetos server.
        /// </summary>
        public static void Initialize(RhetosAppEnvironment rhetosAppEnvironment)
        {
            _rhetosAppEnvironment = rhetosAppEnvironment;
        }

        public static string RhetosServerRootPath
        {
            get
            {
                if (_rhetosAppEnvironment == null)
                    throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                        $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities" +
                        $" or use {nameof(RhetosAppEnvironment)}.");

                return _rhetosAppEnvironment.RootPath;
            }
        }

        public static string PackagesCacheFolder => _rhetosAppEnvironment.PackagesCacheFolder;
        public static string ResourcesFolder => _rhetosAppEnvironment.ResourcesFolder;
        public static string BinFolder => _rhetosAppEnvironment.BinFolder;
        public static string GeneratedFolder => _rhetosAppEnvironment.GeneratedFolder;
        public static string GeneratedFilesCacheFolder => _rhetosAppEnvironment.GeneratedFilesCacheFolder;
        public static string PluginsFolder => _rhetosAppEnvironment.PluginsFolder;
        public static string RhetosServerWebConfigFile => Path.Combine(_rhetosAppEnvironment.RootPath, "Web.config");
        public static string ConnectionStringsFile => Path.Combine(_rhetosAppEnvironment.RootPath, @"bin\ConnectionStrings.config");
        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => _rhetosAppEnvironment.GetDomAssemblyFile(domAssembly);
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => _rhetosAppEnvironment.DomAssemblyFiles;
    }
}
