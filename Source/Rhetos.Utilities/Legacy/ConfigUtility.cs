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
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    [Obsolete("Use IConfigurationProvider instead.")]
    public static class ConfigUtility
    {
        private static IConfigurationProvider _configurationProvider;

        public static void Initialize(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        /// <summary>
        /// Use "Configuration.GetInt" or "Configuration.GetBool" instead.
        /// Reads the web service configuration from appSettings group in web.config file.
        /// When used in another application (for example, DeployPackages.exe),
        /// the application's ".config" file can be used to override the default settings from the web.config.
        /// </summary>
        public static string GetAppSetting(string key)
        {
            ThrowIfNotInitialized();
            return _configurationProvider.GetValue<string>(key);
        }

        private const string ServerConnectionStringName = "ServerConnectionString";

        public static System.Configuration.ConnectionStringSettings GetConnectionString()
        {
            ThrowIfNotInitialized();

            var connectionStringOptions = _configurationProvider.GetOptions<ConnectionStringOptions>($"ConnectionStrings:{ServerConnectionStringName}");

            if (string.IsNullOrEmpty(connectionStringOptions.Name))
                throw new FrameworkException("Missing '" + ServerConnectionStringName + "' connection string in the Rhetos server's configuration.");

            var connectionStringSettings = new System.Configuration.ConnectionStringSettings(
                connectionStringOptions.Name,
                connectionStringOptions.ConnectionString,
                connectionStringOptions.ProviderName);

            return connectionStringSettings;
        }

        private static void ThrowIfNotInitialized()
        {
            if (_configurationProvider == null)
                throw new FrameworkException("ConfigUtility is not initialized. Use LegacyUtilities.Initialize() to initialize obsolete static utilities or use the new IConfigurationProvider.");
        }
    }
}
