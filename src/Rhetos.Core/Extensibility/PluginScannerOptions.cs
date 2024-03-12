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

using System.Collections.Generic;

namespace Rhetos.Extensibility
{
    /// <summary>
    /// Build-time configuration.
    /// </summary>
    [Options("Rhetos:PluginScanner")]
    public class PluginScannerOptions
    {
        /// <summary>
        /// List of file names or file name prefixes that will be ignored when scanning for plugins.
        /// If an entry ends with '*', it will be considered a file name prefix, otherwise an exact match will be used.
        /// Names are case-insensitive.
        /// These entries will be appended to <see cref="PredefinedIgnoreAssemblyFiles"/>.
        /// </summary>
        public IEnumerable<string> IgnoreAssemblyFiles { get; set; }

        public IEnumerable<string> PredefinedIgnoreAssemblyFiles { get; set; } = _predefinedIgnoreAssemblyFiles;

        private static readonly IEnumerable<string> _predefinedIgnoreAssemblyFiles = new[]
        {
			// Including all Rhetos libraries that do not contain "Export" attribute for plugin registration.
            "Autofac.*",
            "Azure.*",
            "Castle.*",
            "EntityFramework.*",
            "Microsoft.*",
            "Newtonsoft.Json.dll",
            "NLog.*",
            "NuGet.*",
            "Oracle.ManagedDataAccess.dll", // Keeping this to avoid slowing down legacy apps.
            "Rhetos.Core.dll",
            "Rhetos.Core.DslParser.dll",
            "Rhetos.Core.Integration.dll",
            "rhetos.dll",
            "rhetos.exe",
            "Rhetos.Host.AspNet.dll",
            "Rhetos.TestCommon.dll",
            "RhetosVSIntegration.dll",
            "sni.dll",
            "System.*",
        };
    }
}
