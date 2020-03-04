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
using Rhetos.Logging;
using System.IO;
using System.Text;

namespace Rhetos
{
    public class RhetosProjectAssetsFileProvider
    {
        public const string ProjectAssetsFileName = "rhetos-project.assets.json";

        public string ProjectAssetsFilePath { get; set; }

        public RhetosProjectAssetsFileProvider(string projectRootFolder)
        {
            ProjectAssetsFilePath = Path.Combine(projectRootFolder, "obj", ProjectAssetsFileName);
        }

        public RhetosProjectAssets Load()
        {
            if (!File.Exists(ProjectAssetsFilePath))
                return null;

            string serialized = File.ReadAllText(ProjectAssetsFilePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<RhetosProjectAssets>(serialized, _serializerSettings);
        }

        public bool Save(RhetosProjectAssets rhetosProjectAssets)
        {
            string serialized = JsonConvert.SerializeObject(rhetosProjectAssets, _serializerSettings);
            string oldSerializedData = File.Exists(ProjectAssetsFilePath) ? File.ReadAllText(ProjectAssetsFilePath, Encoding.UTF8) : "";

            if (oldSerializedData != serialized)
            {
                File.WriteAllText(ProjectAssetsFilePath, serialized, Encoding.UTF8);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
    }
}
