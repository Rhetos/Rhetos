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

using System.Reflection;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Database connection string for Rhetos components.
    /// </summary>
    public class ConnectionString
    {
        /// <summary>
        /// Default configuration settings key for the connection string.
        /// </summary>
        /// <remarks>
        /// Note that the connection string could be registered to the application's context in different ways, circumventing the configuration settings.
        /// In application code, do not resolve the connection string directly from the application's configuration using this key,
        /// instead use the <see cref="ConnectionString"/> class from dependency injection.
        /// </remarks>
        public static readonly string ConnectionStringConfigurationKey = "ConnectionStrings:RhetosConnectionString";

        private readonly string _value;

        /// <summary>
        /// Reads the connection string from configuration and updates the application name (optional).
        /// </summary>
        public ConnectionString(IConfiguration configuration, ISqlUtility sqlUtility, DatabaseOptions databaseOptions)
        {
            var optionsConnectionString = configuration.GetValue<string>(ConnectionStringConfigurationKey);
            _value = GetConnectionStringWithAppName(optionsConnectionString, sqlUtility, databaseOptions);
        }

        /// <summary>
        /// Alternative constructor to simplify overriding connection string when needed.
        /// </summary>
        public ConnectionString(string connectionString)
        {
            _value = connectionString;
        }

        /// <summary>
        /// Helper for constructing Rhetos connection strings.
        /// </summary>
        public static string GetConnectionStringWithAppName(string connectionString, ISqlUtility sqlUtility, DatabaseOptions databaseOptions, string appName = null, string appNameSuffix = null)
        {
            if (databaseOptions.SetApplicationName)
            {
                string hostAppName = (appName ?? Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Rhetos") + (appNameSuffix ?? "");
                connectionString = sqlUtility.SetApplicationName(connectionString, hostAppName);
            }

            return connectionString;
        }

        public override string ToString()
        {
            return _value;
        }

        public static implicit operator string(ConnectionString connectionString)
        {
            return connectionString._value;
        }
    }
}
