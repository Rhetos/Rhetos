using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration.DefaultSources
{
    public class SystemConfigurationSource : IConfigurationSource
    {
        public Dictionary<string, object> Load()
        {
            var settings = new Dictionary<string, object>();

            if (ConfigurationManager.AppSettings != null)
            {
                foreach (var key in ConfigurationManager.AppSettings.AllKeys)
                {
                    var normalizedKey = key
                        .Replace(".", "__")
                        .Replace(":", "__");
                    settings[normalizedKey] = ConfigurationManager.AppSettings[key];
                }
            }


            if (ConfigurationManager.ConnectionStrings != null)
            {
                foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
                {
                    var connectionSectionName = $"ConnectionStrings__{connectionString.Name}";
                    settings[$"{connectionSectionName}__Name"] = connectionString.Name;
                    settings[$"{connectionSectionName}__ConnectionString"] = connectionString.ConnectionString;
                    settings[$"{connectionSectionName}__ProviderName"] = connectionString.ProviderName;
                }
            }

            return settings;
        }
    }
}
