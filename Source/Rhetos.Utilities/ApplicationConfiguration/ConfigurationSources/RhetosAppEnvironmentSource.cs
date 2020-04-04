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
using System.IO;
using System.Reflection;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    public class RhetosAppEnvironmentSource : IConfigurationSource
    {
        private readonly string _applicationRootFolder;
        private readonly JsonFileSource _jsonFileSource;

        public RhetosAppEnvironmentSource(string applicationRootFolder)
        {
            _applicationRootFolder = applicationRootFolder;
            _jsonFileSource = new JsonFileSource(Path.Combine(applicationRootFolder, RhetosAppConfiguration.ConfigurationFileName), optional: true);
        }

        public IDictionary<string, object> Load()
        {
            var settings = new Dictionary<string, object>(_jsonFileSource.Load());

            // Settings provided by host application:

            var appRootFolder = settings.GetValueOrDefault(nameof(RhetosAppEnvironment.ApplicationRootFolder)) as string;
            if (string.IsNullOrEmpty(_applicationRootFolder))
                throw new FrameworkException($"Missing '{nameof(RhetosAppEnvironment.ApplicationRootFolder)}' configuration setting. Please verify that the Rhetos build have passed successfully.");

            // Settings from configuration generated at build-time:

            string rhetosRuntimePath = settings.GetValueOrDefault(nameof(RhetosAppConfiguration.RhetosRuntimePath)) as string;
            if (string.IsNullOrEmpty(rhetosRuntimePath))
                throw new FrameworkException($"Missing '{nameof(RhetosAppConfiguration.RhetosRuntimePath)}' configuration setting. Please verify that the Rhetos build have passed successfully.");

            string assetsFolder = settings.GetValueOrDefault(nameof(RhetosAppConfiguration.AssetsFolder)) as string;
            if (string.IsNullOrEmpty(assetsFolder))
                throw new FrameworkException($"Missing '{nameof(RhetosAppConfiguration.AssetsFolder)}' configuration setting. Please verify that the Rhetos build have passed successfully.");

            // RhetosAppEnvironment: Converting to full path and adding computed properties.

            string absolutePath(string relativePath) => relativePath != null ? Path.GetFullPath(Path.Combine(appRootFolder, relativePath)) : null;

            settings[nameof(RhetosAppEnvironment.ApplicationRootFolder)] = Path.GetFullPath(appRootFolder);
            settings[nameof(RhetosAppEnvironment.RhetosRuntimePath)] = absolutePath(rhetosRuntimePath);
            settings[nameof(RhetosAppEnvironment.AssetsFolder)] = absolutePath(assetsFolder);
            settings[nameof(RhetosAppEnvironment.AssemblyFolder)] = Path.GetDirectoryName(absolutePath(rhetosRuntimePath));
            settings[nameof(RhetosAppEnvironment.AssemblyName)] = Assembly.Load(absolutePath(assetsFolder)).GetName().Name;
            
            return settings;
        }
    }
}
