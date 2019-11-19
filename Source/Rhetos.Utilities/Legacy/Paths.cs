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
            if (rhetosAppEnvironment == null) 
                throw new ArgumentNullException(nameof(rhetosAppEnvironment), "Can't initialize utility with null RhetosAppEnvironment.");
        }

        public static string RhetosServerRootPath => NonNullRhetosAppEnvironment.RootPath;
        public static string PackagesCacheFolder => NonNullRhetosAppEnvironment.PackagesCacheFolder;
        public static string ResourcesFolder => NonNullRhetosAppEnvironment.ResourcesFolder;
        public static string BinFolder => NonNullRhetosAppEnvironment.BinFolder;
        public static string GeneratedFolder => NonNullRhetosAppEnvironment.GeneratedFolder;
        public static string GeneratedFilesCacheFolder => NonNullRhetosAppEnvironment.GeneratedFilesCacheFolder;
        public static string PluginsFolder => NonNullRhetosAppEnvironment.PluginsFolder;
        public static string RhetosServerWebConfigFile => Path.Combine(NonNullRhetosAppEnvironment.RootPath, "Web.config");
        public static string ConnectionStringsFile => Path.Combine(NonNullRhetosAppEnvironment.RootPath, @"bin\ConnectionStrings.config");
        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => NonNullRhetosAppEnvironment.GetDomAssemblyFile(domAssembly);
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => NonNullRhetosAppEnvironment.DomAssemblyFiles;

        private static void AssertRhetosAppEnvironmentNotNull()
        {
            if (_rhetosAppEnvironment == null)
                throw new FrameworkException($"Rhetos server is not initialized ({nameof(Paths)} class)." +
                    $" Use {nameof(LegacyUtilities)}.{nameof(LegacyUtilities.Initialize)}() to initialize obsolete static utilities" +
                    $" or use {nameof(RhetosAppEnvironment)}.");
        }

        private static RhetosAppEnvironment NonNullRhetosAppEnvironment
        {
            get
            {
                AssertRhetosAppEnvironmentNotNull();
                return _rhetosAppEnvironment;
            }
        }
    }
}
