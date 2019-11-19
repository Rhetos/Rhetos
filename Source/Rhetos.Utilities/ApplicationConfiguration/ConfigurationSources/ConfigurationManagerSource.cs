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
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    public class ConfigurationManagerSource : IConfigurationSource
    {
        public IDictionary<string, object> Load()
        {
            var appSettings = new List<KeyValuePair<string, string>>();
            if (ConfigurationManager.AppSettings != null)
            {
                foreach (var key in ConfigurationManager.AppSettings.AllKeys)
                    appSettings.Add(new KeyValuePair<string, string>(key, ConfigurationManager.AppSettings[key]));
            }

            var connectionStrings = new List<ConnectionStringSettings>();
            if (ConfigurationManager.ConnectionStrings != null)
            {
                foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
                    connectionStrings.Add(connectionString);
            }

            return new DotNetConfigurationSource(appSettings, connectionStrings)
                .Load();
        }
    }
}
