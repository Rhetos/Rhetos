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
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Rhetos.Utilities.ApplicationConfiguration;

namespace Rhetos
{
    public static class ConfigurationSourcesBuilderExtensions
    {
        public static IConfigurationBuilder AddKeyValues(this IConfigurationBuilder builder, IEnumerable<KeyValuePair<string, object>> keyValues)
        {
            builder.Add(new KeyValuesSource(keyValues));
            return builder;
        }

        public static IConfigurationBuilder AddKeyValues(this IConfigurationBuilder builder, params KeyValuePair<string, object>[] keyValues)
        {
            builder.AddKeyValues(keyValues.AsEnumerable());
            return builder;
        }
        
        public static IConfigurationBuilder AddKeyValue(this IConfigurationBuilder builder, string key, object value)
        {
            builder.AddKeyValues(new KeyValuePair<string, object>(key, value));
            return builder;
        }

        public static IConfigurationBuilder AddOptions(this IConfigurationBuilder builder, object options, string configurationPath = "")
        {
            if (string.IsNullOrEmpty(configurationPath))
                configurationPath = OptionsAttribute.GetConfigurationPath(options.GetType());

            var members = options.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(member => (member.Name, Value: member.GetValue(options)))
                .Concat(options.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).Select(member => (member.Name, Value: member.GetValue(options))));

            string keyPrefix = !string.IsNullOrEmpty(configurationPath) ? configurationPath + ConfigurationProvider.ConfigurationPathSeparator : "";
            var settings = members
                .Select(member => new KeyValuePair<string, object>(keyPrefix + member.Name, member.Value))
                .ToList();

            return builder.AddKeyValues(settings);
        }

        public static IConfigurationBuilder AddCommandLineArguments(this IConfigurationBuilder builder, string[] args, string argumentPrefix, string configurationPath = "")
        {
            builder.Add(new CommandLineArgumentsSource(args, argumentPrefix, configurationPath));
            return builder;
        }

        /// <summary>
        /// Configures RhetosAppEnvironment and loads configuration from Rhetos run-time configuration files.
        /// </summary>
        public static IConfigurationBuilder AddRhetosAppEnvironment(this IConfigurationBuilder builder, string configurationFolder)
        {
            // Normalize path for better error handling and more robust configuration.
            configurationFolder = Path.GetFullPath(Path.Combine(configurationFolder, "."));

            builder.AddKeyValue($"{OptionsAttribute.GetConfigurationPath<RhetosAppEnvironment>()}:{nameof(RhetosAppEnvironment.ApplicationRootFolder)}", configurationFolder);

            // Main run-time configuration file.
            builder.AddJsonFile(Path.Combine(configurationFolder, RhetosAppEnvironment.ConfigurationFileName), optional: true);

            // User-specific run-time configuration file.
            builder.AddJsonFile(Path.Combine(configurationFolder, RhetosAppEnvironment.LocalConfigurationFileName), optional: true);

            return builder;
        }

        /// <summary>
        /// Adds current application's configuration (App.config or Web.config: appSettings and connectionStrings).
        /// Note that the "current application" in this context can be main Rhetos application,
        /// or a custom command-line utility that references the main application and uses it's runtime components.
        /// </summary>
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

        public static IConfigurationBuilder AddJsonFile(this IConfigurationBuilder builder, string jsonFilePath, bool optional = false)
        {
            builder.Add(new JsonFileSource(jsonFilePath, optional));
            return builder;
        }
    }
}
