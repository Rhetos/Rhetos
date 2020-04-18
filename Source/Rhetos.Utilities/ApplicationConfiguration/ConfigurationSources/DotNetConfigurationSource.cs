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

        public string BaseFolder => AppDomain.CurrentDomain.BaseDirectory;

        public IDictionary<string, IConfigurationValue> Load()
        {
            var settings = new Dictionary<string, IConfigurationValue>();

            foreach (var pair in appSettings)
                settings[pair.Key] = new VerbatimConfigurationValue(pair.Value);

            if (connectionStrings != null)
                foreach (ConnectionStringSettings connectionString in connectionStrings)
                {
                    var connectionSectionName = $"ConnectionStrings{ConfigurationProvider.ConfigurationPathSeparator}{connectionString.Name}";
                    settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}Name"] = new VerbatimConfigurationValue(connectionString.Name);
                    settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ConnectionString"] = new VerbatimConfigurationValue(connectionString.ConnectionString);
                    settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ProviderName"] = new VerbatimConfigurationValue(connectionString.ProviderName);
                }

            return settings;
        }
    }
}
