using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.TestCommon
{
    public class MockConfiguration : Dictionary<string, object>, IConfiguration
    {
        public Lazy<bool> GetBool(string key, bool defaultValue) => Get(key, defaultValue);

        public Lazy<int> GetInt(string key, int defaultValue) => Get(key, defaultValue);

        public Lazy<string> GetString(string key, string defaultValue) => Get(key, defaultValue);

        private Lazy<T> Get<T>(string key, T defaultValue)
        {
            object value;
            if (!this.TryGetValue(key, out value))
                value = defaultValue;
            return new Lazy<T>(() => (T)value);
        }
    }
}
