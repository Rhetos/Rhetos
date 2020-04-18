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

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    /// <summary>
    /// Loads Rhetos run-time configuration from <see cref="RhetosAppEnvironment.ConfigurationFileName"/>
    /// and initializes RhetosAppEnvironment paths.
    /// </summary>
    public class RhetosAppEnvironmentSource : IConfigurationSource
    {
        private readonly string _configurationFolder;
        private readonly JsonFileSource _jsonFileSource;

        public RhetosAppEnvironmentSource(string configurationFolder)
        {
            _configurationFolder = configurationFolder;
            _jsonFileSource = new JsonFileSource(Path.Combine(configurationFolder, RhetosAppEnvironment.ConfigurationFileName), optional: true);
        }

        public string BaseFolder => _configurationFolder;

        public IDictionary<string, IConfigurationValue> Load()
        {
            var settings = new Dictionary<string, IConfigurationValue>(_jsonFileSource.Load());

            // Settings from configuration generated at build-time:

            string rhetosRuntimePath = settings.GetValueOrDefault(nameof(RhetosAppEnvironment.RhetosRuntimePath)).Value as string;
            if (string.IsNullOrEmpty(rhetosRuntimePath))
                throw new FrameworkException($"Missing '{nameof(RhetosAppEnvironment.RhetosRuntimePath)}' configuration setting. Please verify that the Rhetos build have passed successfully.");

            string assetsFolder = settings.GetValueOrDefault(nameof(RhetosAppEnvironment.AssetsFolder)).Value as string;
            if (string.IsNullOrEmpty(assetsFolder))
                throw new FrameworkException($"Missing '{nameof(RhetosAppEnvironment.AssetsFolder)}' configuration setting. Please verify that the Rhetos build have passed successfully.");

            // RhetosAppEnvironment: Converting to full path and adding computed properties.

            string absolutePath(string relativePath) => relativePath != null ? Path.GetFullPath(Path.Combine(_configurationFolder, relativePath)) : null;

            settings[nameof(RhetosAppEnvironment.ApplicationRootFolder)] = new VerbatimConfigurationValue(Path.GetFullPath(_configurationFolder));
            settings[nameof(RhetosAppEnvironment.RhetosRuntimePath)] = new VerbatimConfigurationValue(absolutePath(rhetosRuntimePath));
            settings[nameof(RhetosAppEnvironment.AssetsFolder)] = new VerbatimConfigurationValue(absolutePath(assetsFolder));
            settings[nameof(RhetosAppEnvironment.AssemblyFolder)] = new VerbatimConfigurationValue(Path.GetDirectoryName(absolutePath(rhetosRuntimePath)));
            
            return settings;
        }
    }
}
