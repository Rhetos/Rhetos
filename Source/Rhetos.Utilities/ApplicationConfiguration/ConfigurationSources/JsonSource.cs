using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    public class JsonSource : IConfigurationSource
    {
        private readonly string _jsonText;

        public JsonSource(string jsonText)
        {
            _jsonText = jsonText;
        }

        public IDictionary<string, object> Load()
        {
            var parsedJson = JObject.Parse(_jsonText);
            return GetKeysFromObject(parsedJson, "");
        }

        private static readonly JTokenType[] _allowedJTokenTypes = { JTokenType.Boolean, JTokenType.String, JTokenType.Integer, JTokenType.Float, JTokenType.Object };
        private Dictionary<string, object> GetKeysFromObject(JObject jObject, string path)
        {
            var jsonOptions = new Dictionary<string, object>();

            foreach (var keyValue in jObject)
            {
                var fullKey = string.IsNullOrEmpty(path)
                    ? keyValue.Key
                    : $"{path}{ConfigurationProvider.ConfigurationPathSeparator}{keyValue.Key}";

                if (!_allowedJTokenTypes.Contains(keyValue.Value.Type))
                    throw new FrameworkException($"Json token type {keyValue.Value.Type} is not allowed at '{fullKey}', value '{keyValue.Value}'.");

                if (keyValue.Value.Type == JTokenType.Object)
                {
                    var childOptions = GetKeysFromObject(keyValue.Value as JObject, fullKey);
                    foreach (var pair in childOptions) jsonOptions.Add(pair.Key, pair.Value);
                }
                else
                {
                    jsonOptions.Add(fullKey, keyValue.Value.ToString());
                }
            }

            return jsonOptions;
        }

    }
}
