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
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class RhetosAppEnvironment
    {
        public string RootPath { get; }
        public string PackagesCacheFolder { get; }
        public string ResourcesFolder { get; }
        public string BinFolder { get; }
        public string GeneratedFolder { get; }
        public string GeneratedFilesCacheFolder { get; }
        public string PluginsFolder { get; }

        public RhetosAppEnvironment(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
                throw new FrameworkException($"Can't initialize {nameof(RhetosAppEnvironment)}. RootPath is null or not configured.");

            RootPath = Path.GetFullPath(rootPath);
            PackagesCacheFolder = Path.Combine(RootPath, "PackagesCache");
            ResourcesFolder = Path.Combine(RootPath, "Resources");
            BinFolder = Path.Combine(RootPath, "bin");
            GeneratedFolder = Path.Combine(RootPath, "bin\\Generated");
            GeneratedFilesCacheFolder = Path.Combine(RootPath, "GeneratedFilesCache");
            PluginsFolder = Path.Combine(RootPath, "bin\\Plugins");
        }

        public string GetDomAssemblyFile(DomAssemblies domAssembly) => Path.Combine(GeneratedFolder, $"ServerDom.{domAssembly}.dll");
        /// <summary>
        /// List of the generated dll files that make the domain object model (ServerDom.*.dll).
        /// </summary>
        public IEnumerable<string> DomAssemblyFiles => Enum.GetValues(typeof(DomAssemblies)).Cast<DomAssemblies>().Select(domAssembly => GetDomAssemblyFile(domAssembly));
    }
}
