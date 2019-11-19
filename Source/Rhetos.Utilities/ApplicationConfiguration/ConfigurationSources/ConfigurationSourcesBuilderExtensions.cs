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

using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Configuration;

namespace Rhetos
{
    public static class ConfigurationSourcesBuilderExtensions
    {
        public static IConfigurationBuilder AddKeyValues(this IConfigurationBuilder builder, params KeyValuePair<string, object>[] keyValues)
        {
            builder.Add(new KeyValuesSource(keyValues));
            return builder;
        }

        public static IConfigurationBuilder AddKeyValue(this IConfigurationBuilder builder, string key, object value)
        {
            builder.Add(new KeyValuesSource(new [] { new KeyValuePair<string, object>(key, value) }));
            return builder;
        }

        public static IConfigurationBuilder AddCommandLineArguments(this IConfigurationBuilder builder, string[] args, string argumentPrefix, string configurationPath = "")
        {
            builder.Add(new CommandLineArgumentsSource(args, argumentPrefix, configurationPath));
            return builder;
        }

        public static IConfigurationBuilder AddConfigurationManagerConfiguration(this IConfigurationBuilder builder)
        {
            builder.Add(new ConfigurationManagerSource());
            return builder;
        }

        public static IConfigurationBuilder AddConfigurationFile(this IConfigurationBuilder builder, string filePath)
        {
            filePath = Path.GetFullPath(filePath);
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap { ExeConfigFilename = filePath };
            System.Configuration.Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            builder.Add(new ConfigurationFileSource(configuration));
            return builder;
        }

        /// <summary>
        /// Sets RhetosAppRootPath to specified path and loads default configuration for the app at that path.
        /// Currently, web.config is expected to exist at the path and configuration will be loaded from it.
        /// This is planned for phasing out in favor of separate config file used only for Rhetos app.
        /// </summary>
        public static IConfigurationBuilder AddRhetosAppConfiguration(this IConfigurationBuilder builder, string rhetosAppRootPath)
        {
            rhetosAppRootPath = Path.GetFullPath(rhetosAppRootPath);
            builder.AddKeyValue(nameof(RhetosAppOptions.RootPath), rhetosAppRootPath);
            builder.AddWebConfiguration(rhetosAppRootPath);
            return builder;
        }

        private static IConfigurationBuilder AddWebConfiguration(this IConfigurationBuilder builder, string webRootPath)
        {
            webRootPath = Path.GetFullPath(webRootPath);
            VirtualDirectoryMapping vdm = new VirtualDirectoryMapping(webRootPath, true);
            WebConfigurationFileMap wcfm = new WebConfigurationFileMap();
            wcfm.VirtualDirectories.Add("/", vdm);
            System.Configuration.Configuration configuration = WebConfigurationManager.OpenMappedWebConfiguration(wcfm, "/");
            builder.Add(new ConfigurationFileSource(configuration));
            return builder;
        }
    }
}
