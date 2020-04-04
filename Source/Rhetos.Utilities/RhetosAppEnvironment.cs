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

using Rhetos.Utilities.ApplicationConfiguration;
using System.IO;
using System.Reflection;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Run-time environment.
    /// </summary>
    public class RhetosAppEnvironment : IRhetosEnvironment
    {
        public string ApplicationRootFolder { get; set; }

        public string RhetosRuntimePath { get; set; }

        public string AssemblyFolder { get; set; }

        public string AssetsFolder { get; set; }

        /// <summary>
        /// Assembly that contains the generated object model.
        /// Null for old Rhetos applications with multiple generated ServerDom libraries.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Initializes RhetosAppEnvironment from runtime configuration.
        /// </summary>
        public static RhetosAppEnvironment FromRuntimeConfiguration(IConfigurationProvider configuration)
        {
            // Settings provided by host application:
            var appRootFolder = configuration.GetValue<string>(nameof(ApplicationRootFolder));
            if (string.IsNullOrEmpty(appRootFolder))
                throw new FrameworkException($"Missing '{nameof(ApplicationRootFolder)}' configuration setting. Please verify that the Rhetos build have passed successfully.");

            // Settings from configuration generated at build-time:
            var appConfiguration = configuration.GetOptions<RhetosAppConfiguration>();
            if (string.IsNullOrEmpty(appConfiguration.RhetosRuntimePath))
                throw new FrameworkException($"Missing '{nameof(RhetosAppConfiguration.RhetosRuntimePath)}' configuration setting. Please verify that the Rhetos build have passed successfully.");
            if (string.IsNullOrEmpty(appConfiguration.AssetsFolder))
                throw new FrameworkException($"Missing '{nameof(RhetosAppConfiguration.AssetsFolder)}' configuration setting. Please verify that the Rhetos build have passed successfully.");

            string absolutePath(string relativePath) => relativePath != null ? Path.GetFullPath(Path.Combine(appRootFolder, relativePath)) : null;

            return new RhetosAppEnvironment
            {
                ApplicationRootFolder = Path.GetFullPath(appRootFolder),
                RhetosRuntimePath = absolutePath(appConfiguration.RhetosRuntimePath),
                AssetsFolder = absolutePath(appConfiguration.AssetsFolder),
                AssemblyFolder = Path.GetDirectoryName(absolutePath(appConfiguration.RhetosRuntimePath)),
                AssemblyName = Assembly.Load(absolutePath(appConfiguration.RhetosRuntimePath)).GetName().Name,
            };
        }
    }
}
