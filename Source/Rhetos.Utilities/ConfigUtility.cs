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
using System.Web.Configuration;

namespace Rhetos.Utilities
{
    public static class ConfigUtility
    {
        public static string GetAppSetting(string key)
        {
            string settingValue = ConfigurationManager.AppSettings[key];

            if (settingValue == null && !Paths.IsRhetosServer)
            {
                var setting = RhetosWebConfig.Value.AppSettings.Settings[key];
                if (setting != null)
                    settingValue = setting.Value;
            }

            return settingValue;
        }

        private const string ServerConnectionStringName = "ServerConnectionString";

        public static ConnectionStringSettings GetConnectionString()
        {
            ConnectionStringSettings connectionStringConfiguration = ConfigurationManager.ConnectionStrings[ServerConnectionStringName];

            if (connectionStringConfiguration == null && !Paths.IsRhetosServer)
                connectionStringConfiguration = RhetosWebConfig.Value.ConnectionStrings.ConnectionStrings[ServerConnectionStringName];

            if (connectionStringConfiguration == null)
                throw new FrameworkException("Missing '" + ServerConnectionStringName + "' connection string in the Rhetos server's configuration.");

            return connectionStringConfiguration;
        }

        private static Lazy<Configuration> RhetosWebConfig = new Lazy<Configuration>(InitializeWebConfiguration);

        private static Configuration InitializeWebConfiguration()
        {
            VirtualDirectoryMapping vdm = new VirtualDirectoryMapping(Paths.RhetosServerRootPath, true);
            WebConfigurationFileMap wcfm = new WebConfigurationFileMap();
            wcfm.VirtualDirectories.Add("/", vdm);
            return WebConfigurationManager.OpenMappedWebConfiguration(wcfm, "/");
        }
    }
}
