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
using Rhetos.Utilities;
using System.IO;

namespace Rhetos.Dsl
{
    public class DslDocumentationFile
    {
        public static readonly string DslDocumentationFileName = "DslDocumentation.json";

        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;

        public DslDocumentationFile(RhetosBuildEnvironment rhetosBuildEnvironment)
        {
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
        }

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        private string DslDocumentationFilePath => Path.Combine(_rhetosBuildEnvironment.CacheFolder, DslDocumentationFileName);

        /// <summary>
        /// Save method will not update the existing file if the content is same.
        /// This is useful for optimization of external DSL analysis, such as DSL IntelliSense plugin.
        /// </summary>
        public void Save(DslDocumentation dslDocumentation)
        {
            string newContent = JsonConvert.SerializeObject(dslDocumentation, _jsonSettings);

            string oldContent = File.Exists(DslDocumentationFilePath)
                ? File.ReadAllText(DslDocumentationFilePath)
                : null;

            if (newContent != oldContent)
                File.WriteAllText(DslDocumentationFilePath, newContent);
        }

        public DslDocumentation Load()
        {
            using (var fileReader = new StreamReader(File.OpenRead(DslDocumentationFilePath)))
            using (var jsonReader = new JsonTextReader(fileReader))
            {
                JsonSerializer serializer = JsonSerializer.Create(_jsonSettings);
                return serializer.Deserialize<DslDocumentation>(jsonReader);
            }
        }
    }
}