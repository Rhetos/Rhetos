using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration.DefaultSources
{
    public class ConfigurationFileSource : IConfigurationSource
    {
        private readonly System.Configuration.Configuration configuration;

        public ConfigurationFileSource(System.Configuration.Configuration configuration)
        {
            this.configuration = configuration;
        }
        
        public IDictionary<string, object> Load()
        {
            var appSettings = new List<KeyValuePair<string, string>>();
            if (configuration.AppSettings?.Settings != null)
            {
                foreach (var key in configuration.AppSettings.Settings.AllKeys)
                    appSettings.Add(new KeyValuePair<string, string>(key, configuration.AppSettings.Settings[key].Value));
            }

            var connectionStrings = new List<ConnectionStringSettings>();
            if (configuration.ConnectionStrings?.ConnectionStrings != null)
            {
                foreach (ConnectionStringSettings connectionString in configuration.ConnectionStrings.ConnectionStrings)
                    connectionStrings.Add(connectionString);
            }

            return new DotNetConfigurationSource(appSettings, connectionStrings)
                .Load();
        }
    }
}
