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

        private readonly JsonSerializerSettings _jsonSerializerSettings = new()
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
        };

        private string DslSyntaxFilePath => Path.Combine(_rhetosBuildEnvironment.CacheFolder, DslSyntaxFileName);

        public void Serialize(DslSyntax dslSyntax)
        {
            using (var fileWriter = File.CreateText(DslSyntaxFilePath))
            {
                JsonSerializer serializer = JsonSerializer.Create(_jsonSerializerSettings);
                serializer.Serialize(fileWriter, dslSyntax);
            }
        }

        public DslSyntax Deserialize()
        {
            using (var fileReader = new StreamReader(File.OpenRead(DslSyntaxFilePath)))
            using (var jsonReader = new JsonTextReader(fileReader))
            {
                JsonSerializer serializer = JsonSerializer.Create(_jsonSerializerSettings);
                return serializer.Deserialize<DslSyntax>(jsonReader);
            }
        }
    }
}