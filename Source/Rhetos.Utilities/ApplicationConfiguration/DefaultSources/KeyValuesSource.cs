using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration.DefaultSources
{
    public class KeyValuesSource : IConfigurationSource
    {
        private readonly IList<KeyValuePair<string, object>> keyValuePairs;

        public KeyValuesSource(IList<KeyValuePair<string, object>> keyValuePairs)
        {
            this.keyValuePairs = keyValuePairs;
        }

        public Dictionary<string, object> Load()
        {
            return keyValuePairs.ToDictionary(a => a.Key, a => a.Value);
        }
    }
}
