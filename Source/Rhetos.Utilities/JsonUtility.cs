using Newtonsoft.Json;
using System.IO;

namespace Rhetos.Utilities
{
    public class JsonUtility
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
            using (StreamWriter file = File.CreateText(filePath))
            {
                JsonSerializer serializer = JsonSerializer.Create(jsonSerializerSettings);
                serializer.Serialize(file, data);
            }
        }

        public static T DeserializeFromFile<T>(string filePath)
        {
            return DeserializeFromFile<T>(filePath, new JsonSerializerSettings());
        }

        public static T DeserializeFromFile<T>(string filePath, JsonSerializerSettings jsonSerializerSettings)
        {
            T data;
            using (StreamReader sr = new StreamReader(File.OpenRead(filePath)))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = JsonSerializer.Create(jsonSerializerSettings);
                data = serializer.Deserialize<T>(reader);
            }
            return data;
        }
    }
}
