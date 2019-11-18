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
using System.Linq;
using System.Text;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    /// <summary>
    /// Used internally to handle multiple scenarios of System.Configuration configuration sources
    /// </summary>
    class DotNetConfigurationSource : IConfigurationSource
    {
        private readonly IList<KeyValuePair<string, string>> appSettings;
        private readonly IList<ConnectionStringSettings> connectionStrings;

        public DotNetConfigurationSource(IList<KeyValuePair<string, string>> appSettings, IList<ConnectionStringSettings> connectionStrings)
        {
            this.appSettings = appSettings;
            this.connectionStrings = connectionStrings;
        }

        public IDictionary<string, object> Load()
        {
            var settings = new Dictionary<string, object>();

            foreach (var pair in appSettings)
                settings[pair.Key] = pair.Value;

            foreach (ConnectionStringSettings connectionString in connectionStrings)
            {
                var connectionSectionName = $"ConnectionStrings{ConfigurationProvider.ConfigurationPathSeparator}{connectionString.Name}";
                settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}Name"] = connectionString.Name;
                settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ConnectionString"] = connectionString.ConnectionString;
                settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ProviderName"] = connectionString.ProviderName;
            }

            return settings;
        }
    }
}
