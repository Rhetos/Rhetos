using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    public class JsonFileSource : IConfigurationSource
    {
        private readonly string _filePath;

        public JsonFileSource(string filePath)
        {
            _filePath = Path.GetFullPath(filePath);
        }

        public IDictionary<string, object> Load()
        {
            var jsonText = File.ReadAllText(_filePath);

            try
            {
                var jsonSource = new JsonSource(jsonText);
                return jsonSource.Load();
            }
            catch (Exception e)
            {
                throw new FrameworkException($"Error parsing Json contents from '{_filePath}'.", e);
            }
        }
    }
}
