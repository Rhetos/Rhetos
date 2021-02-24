using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Rhetos
{
    public static class RhetosConfigurationBuilderExtensions
    {
        public static void MapNetCoreConfiguration(this IConfigurationBuilder builder, IConfiguration configurationToMap)
        {
            if (configurationToMap == null) return;
            foreach (var configurationItem in configurationToMap.AsEnumerable().Where(a => a.Value != null))
            {
                var key = configurationItem.Key;
                if (configurationToMap is IConfigurationSection configurationSection && !string.IsNullOrEmpty(configurationSection.Path))
                {
                    var regex = new Regex($"^{configurationSection.Path}:");
                    key = regex.Replace(key, "");
                }
                builder.AddKeyValue(key, configurationItem.Value);
            }
        }
    }
}
