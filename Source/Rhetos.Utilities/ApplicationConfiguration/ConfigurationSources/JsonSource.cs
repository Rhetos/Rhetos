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

        public IDictionary<string, ConfigurationValue> Load()
        {
            var reader = new JsonTextReader(new StringReader(_jsonText));
            reader.DateParseHandling = DateParseHandling.None;
            var parsedJson = JObject.Load(reader);
            var jsonOptions = new Dictionary<string, ConfigurationValue>();
            AddKeysFromObject(jsonOptions, parsedJson, "");
            return jsonOptions;
        }

        private static readonly JTokenType[] _scalarJTokenTypes = { JTokenType.Boolean, JTokenType.String, JTokenType.Integer, JTokenType.Float };

        private void AddKeysFromObject(Dictionary<string, ConfigurationValue> jsonOptions, JObject jObject, string path)
        {
            foreach (var keyValue in jObject)
                AddKeysFromToken(jsonOptions, keyValue.Value, Combine(path, keyValue.Key));
        }

        private void AddKeysFromToken(Dictionary<string, ConfigurationValue> jsonOptions, JToken jToken, string path)
        {
            if (jToken.Type == JTokenType.Object)
            {
                AddKeysFromObject(jsonOptions, (JObject)jToken, path);
            }
            else if (_scalarJTokenTypes.Contains(jToken.Type))
            {
                jsonOptions.Add(path, new ConfigurationValue(jToken.ToString(), this));
            }
            else if (jToken is JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                    AddKeysFromToken(jsonOptions, jArray[i], Combine(path, i.ToString()));
            }
            else
            {
                throw new FrameworkException($"JSON token type {jToken.Type} is not allowed at '{path}', value '{jToken}'.");
            }
        }

        public string Combine(string path, string key)
        {
            return string.IsNullOrEmpty(path) ? key : $"{path}{ConfigurationProvider.ConfigurationPathSeparator}{key}";
        }
    }
}
