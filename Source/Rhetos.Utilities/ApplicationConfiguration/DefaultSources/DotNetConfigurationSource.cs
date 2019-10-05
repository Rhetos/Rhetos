using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration.DefaultSources
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

        public Dictionary<string, object> Load()
        {
            var settings = new Dictionary<string, object>();

            foreach (var pair in appSettings)
                settings[NormalizeConfigurationKey(pair.Key)] = pair.Value;

            foreach (ConnectionStringSettings connectionString in connectionStrings)
            {
                var connectionSectionName = $"ConnectionStrings{ConfigurationProvider.ConfigurationPathSeparator}{connectionString.Name}";
                settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}Name"] = connectionString.Name;
                settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ConnectionString"] = connectionString.ConnectionString;
                settings[$"{connectionSectionName}{ConfigurationProvider.ConfigurationPathSeparator}ProviderName"] = connectionString.ProviderName;
            }

            return settings;
        }

        private static string NormalizeConfigurationKey(string key)
        {
            return key
                .Replace(".", ConfigurationProvider.ConfigurationPathSeparator)
                .Replace("__", ConfigurationProvider.ConfigurationPathSeparator);
       }
    }
}
