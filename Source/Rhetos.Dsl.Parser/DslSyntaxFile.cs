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
    public class DslSyntaxFile
    {
        public static readonly string DslSyntaxFileName = "DslSyntax.json";

        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;

        public DslSyntaxFile(RhetosBuildEnvironment rhetosBuildEnvironment)
        {
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
        }

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Formatting = Formatting.Indented,
        };

        private string DslSyntaxFilePath => Path.Combine(_rhetosBuildEnvironment.CacheFolder, DslSyntaxFileName);

        /// <summary>
        /// Save method will not update the existing file if the content is same.
        /// This is useful for optimization of external DSL analysis, such as DSL IntelliSense plugin.
        /// </summary>
        public void Save(DslSyntax dslSyntax)
        {
            string newContent = JsonConvert.SerializeObject(dslSyntax, _jsonSettings);

            string oldContent = File.Exists(DslSyntaxFilePath)
                ? File.ReadAllText(DslSyntaxFilePath)
                : null;

            if (newContent != oldContent)
                File.WriteAllText(DslSyntaxFilePath, newContent);
        }

        public DslSyntax Load()
        {
            using (var fileReader = new StreamReader(File.OpenRead(DslSyntaxFilePath)))
            using (var jsonReader = new JsonTextReader(fileReader))
            {
                JsonSerializer serializer = JsonSerializer.Create(_jsonSettings);
                return serializer.Deserialize<DslSyntax>(jsonReader);
            }
        }
    }
}