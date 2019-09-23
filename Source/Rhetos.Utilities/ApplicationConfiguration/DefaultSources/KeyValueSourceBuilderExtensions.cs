using Rhetos.Utilities.ApplicationConfiguration.DefaultSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public static class KeyValueSourceBuilderExtensions
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
    }
}
