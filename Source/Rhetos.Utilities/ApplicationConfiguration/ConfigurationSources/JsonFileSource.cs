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
            if (!File.Exists(_filePath))
                throw new FrameworkException($"Specified Json configuration file '{_filePath}' doesn't exist.");

            string json;
            try
            {
                json = File.ReadAllText(_filePath);
            }
            catch (Exception e)
            {
                throw new FrameworkException($"Error reading Json configuration file '{_filePath}'.", e);
            }

            try
            {
                var jsonSource = new JsonSource(json);
                return jsonSource.Load();
            }
            catch (Exception e)
            {
                throw new FrameworkException($"Error parsing Json contents from '{_filePath}'.", e);
            }
        }
    }
}
