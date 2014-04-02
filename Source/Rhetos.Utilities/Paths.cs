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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    public static class Paths
    {
        private static string _rhetosServerRootPath;

        public static void InitializeRhetosServerRootPath(string rhetosServerRootPath)
        {
            if (_rhetosServerRootPath != null)
                throw new FrameworkException(string.Format(
                    "RhetosServerRootPath is already initialized. Old value = '', new value = ''.",
                    _rhetosServerRootPath, rhetosServerRootPath));

            if (rhetosServerRootPath == null)
                throw new FrameworkException("RhetosServerRootPath is set to null.");

            _rhetosServerRootPath = rhetosServerRootPath;
        }

        public static string RhetosServerRootPath
        {
            get
            {
                if (_rhetosServerRootPath == null)
                    throw new FrameworkException("RhetosServerRootPath is not initialized.");
                return _rhetosServerRootPath;
            }
        }

        public static string DslScriptsFolder { get { return Path.Combine(RhetosServerRootPath, "DslScripts"); } }
        public static string DataMigrationScriptsFolder { get { return Path.Combine(RhetosServerRootPath, "DataMigration"); } }
        public static string ResourcesFolder { get { return Path.Combine(RhetosServerRootPath, "Resources"); } }
        public static string BinFolder { get { return Path.Combine(RhetosServerRootPath, "bin"); } }
        public static string GeneratedFolder { get { return Path.Combine(RhetosServerRootPath, "bin\\Generated"); } }
        public static string PluginsFolder { get { return Path.Combine(RhetosServerRootPath, "bin\\Plugins"); } }

        public static string RhetosServerWebConfigFile { get { return Path.Combine(RhetosServerRootPath, "Web.config"); } }
        public static string NHibernateMappingFile { get { return Path.Combine(RhetosServerRootPath, "bin\\ServerDomNHibernateMapping.xml"); } }
        public static string DomAssemblyFile { get { return Path.Combine(RhetosServerRootPath, "bin", DomAssemblyName + ".dll"); } }
        public static string ConnectionStringsFile { get { return Path.Combine(RhetosServerRootPath, @"bin\ConnectionStrings.config"); } }

        public const string DomAssemblyName = "ServerDom";
    }
}
