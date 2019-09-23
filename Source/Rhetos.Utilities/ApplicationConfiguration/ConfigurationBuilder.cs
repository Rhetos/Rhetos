using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public class ConfigurationBuilder : IConfigurationBuilder
    {
        private readonly List<IConfigurationSource> configurationSources = new List<IConfigurationSource>();

        public void Add(IConfigurationSource source)
        {
            configurationSources.Add(source);
        }

        public IConfigurationProvider Build()
        {
            var configurationValues = new Dictionary<string, object>();

            foreach (var configurationSource in configurationSources)
            {
                var sourceValues = configurationSource.Load();
                foreach (var sourceValue in sourceValues) 
                    configurationValues[sourceValue.Key] = sourceValue.Value;
            }
            return new ConfigurationProvider(configurationValues);
        }
    }
}
