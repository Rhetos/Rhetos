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
using System.IO;

namespace Rhetos.Utilities
{
    public static class JsonUtility
    {
        public static void SerializeToFile<T>(T data, string filePath)
        {
            SerializeToFile<T>(data, filePath, new JsonSerializerSettings());
        }

        public static void SerializeToFile<T>(T data, string filePath, Formatting formatting)
        {
            SerializeToFile<T>(data, filePath, new JsonSerializerSettings { Formatting = formatting });
        }

        public static void SerializeToFile<T>(T data, string filePath, JsonSerializerSettings jsonSerializerSettings)
        {
            using (var fileWriter = File.CreateText(filePath))
            {
                var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);
                jsonSerializer.Serialize(fileWriter, data);
            }
        }

        public static T DeserializeFromFile<T>(string filePath)
        {
            return DeserializeFromFile<T>(filePath, new JsonSerializerSettings());
        }

        public static T DeserializeFromFile<T>(string filePath, JsonSerializerSettings jsonSerializerSettings)
        {
            T data;
            using (var fileReader = new StreamReader(File.OpenRead(filePath)))
            using (var jsonReader = new JsonTextReader(fileReader))
            {
                var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);
                data = jsonSerializer.Deserialize<T>(jsonReader);
            }
            return data;
        }
    }
}
