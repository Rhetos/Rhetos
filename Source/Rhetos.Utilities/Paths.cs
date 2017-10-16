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
    public static class Paths
    {
        /// <summary>
        /// Initialize Paths for the Rhetos server.
        /// </summary>
        public static void InitializeRhetosServer()
        {
            RhetosServerRootPath = AppDomain.CurrentDomain.BaseDirectory;
            IsRhetosServer = true;
        }

        /// <summary>
        /// Initialize Paths for any application other then Rhetos server.
        /// </summary>
        public static void InitializeRhetosServerRootPath(string rhetosServerRootPath)
        {
            RhetosServerRootPath = rhetosServerRootPath;
            IsRhetosServer = false;
        }

        private static bool? _isRhetosServer = null;

        public static bool IsRhetosServer
        {
            get
            {
                if (_isRhetosServer == null)
                    throw new FrameworkException("Rhetos server is not initialized (Paths class).");

                return _isRhetosServer.Value;
            }

            private set
            {
                if (_isRhetosServer != null && _isRhetosServer != value)
                    throw new FrameworkException($"{nameof(IsRhetosServer)} is already initialized to a different value. Old value = '{_isRhetosServer}', new value = '{value}'.");

                _isRhetosServer = value;
            }
        }

        private static string _rhetosServerRootPath;

        public static string RhetosServerRootPath
        {
            get
            {
                if (_rhetosServerRootPath == null)
                    throw new FrameworkException("Rhetos server is not initialized (Paths class).");

                return _rhetosServerRootPath;
            }

            private set
            {
                if (value == null)
                    throw new FrameworkException($"{nameof(RhetosServerRootPath)} is set to null.");

                value = Path.GetFullPath(value);

                if (_rhetosServerRootPath != null && _rhetosServerRootPath != value)
                    throw new FrameworkException($"{nameof(RhetosServerRootPath)} is already initialized to a different value. Old value = '{_rhetosServerRootPath}', new value = '{value}'.");

                _rhetosServerRootPath = value;
            }
        }

        public static string PackagesCacheFolder => Path.Combine(RhetosServerRootPath, "PackagesCache");
        public static string ResourcesFolder => Path.Combine(RhetosServerRootPath, "Resources");
        public static string BinFolder => Path.Combine(RhetosServerRootPath, "bin");
        public static string GeneratedFolder => Path.Combine(RhetosServerRootPath, "bin\\Generated");
        public static string GeneratedFilesCacheFolder => Path.Combine(RhetosServerRootPath, "GeneratedFilesCache");
        public static string PluginsFolder => Path.Combine(RhetosServerRootPath, "bin\\Plugins");
        public static string RhetosServerWebConfigFile => Path.Combine(RhetosServerRootPath, "Web.config");
        public static string ConnectionStringsFile => Path.Combine(RhetosServerRootPath, @"bin\ConnectionStrings.config");
        public static string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public static IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));
    }
}
