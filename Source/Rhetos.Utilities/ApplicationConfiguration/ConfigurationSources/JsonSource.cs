/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var reader = new JsonTextReader(new StringReader(_jsonText));
            reader.DateParseHandling = DateParseHandling.None;
            var parsedJson = JObject.Load(reader);
            return GetKeysFromObject(parsedJson, "");
        }

        private static readonly JTokenType[] _allowedJTokenTypes = { JTokenType.Boolean, JTokenType.String, JTokenType.Integer, JTokenType.Float, JTokenType.Object };

        private Dictionary<string, object> GetKeysFromObject(JObject jObject, string path)
        {
            var jsonOptions = new Dictionary<string, object>();

            // Expecting dot as a standard path separator in JSON configuration.
            string convertPath(string keyPath) => keyPath;//.Replace(".", ConfigurationProvider.ConfigurationPathSeparator);

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
                    foreach (var pair in childOptions) jsonOptions.Add(convertPath(pair.Key), pair.Value);
                }
                else
                {
                    jsonOptions.Add(convertPath(fullKey), keyValue.Value.ToString());
                }
            }

            return jsonOptions;
        }

    }
}
