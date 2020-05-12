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
using System.Configuration;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    /// <summary>
    /// Used internally to handle multiple scenarios of System.Configuration configuration sources
    /// </summary>
    class DotNetConfigurationSource : IConfigurationSource
    {
        private readonly IList<KeyValuePair<string, string>> appSettings;
        private readonly IEnumerable<ConnectionStringSettings> connectionStrings;

        public DotNetConfigurationSource(IList<KeyValuePair<string, string>> appSettings, IEnumerable<ConnectionStringSettings> connectionStrings)
        {
            this.appSettings = appSettings;
            this.connectionStrings = connectionStrings;
        }

        public IDictionary<string, ConfigurationValue> Load()
        {
            var settings = new Dictionary<string, ConfigurationValue>();

            foreach (var pair in appSettings)
                settings[pair.Key] = new ConfigurationValue(pair.Value, this);

            if (connectionStrings != null)
                foreach (ConnectionStringSettings connectionString in connectionStrings)
                {
                    var connectionSectionName = $"ConnectionStrings{ConfigurationProvider.ConfigurationPathSeparator}{connectionString.Name}";
                    settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}Name"] = new ConfigurationValue(connectionString.Name, this);
                    settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ConnectionString"] = new ConfigurationValue(connectionString.ConnectionString, this);
                    settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ProviderName"] = new ConfigurationValue(connectionString.ProviderName, this);
                }

            return settings;
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
