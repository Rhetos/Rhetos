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
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Rhetos.Dsl
{
    public class DslSyntaxFileGenerator : IGenerator
    {
        private readonly IDslSyntax _dslSyntax;
        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;
        private readonly ILogger _performanceLogger;
        public static readonly string DslSyntaxFileName = "DslSyntax.json";

        public DslSyntaxFileGenerator(IDslSyntax dslSyntax, RhetosBuildEnvironment rhetosBuildEnvironment, ILogProvider logProvider)
        {
            _dslSyntax = dslSyntax;
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
        }

        public void Generate()
        {
            var sw = Stopwatch.StartNew();

            var serializerSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
            };

            JsonUtility.SerializeToFile(_dslSyntax, Path.Combine(_rhetosBuildEnvironment.CacheFolder, DslSyntaxFileName), serializerSettings);
            _performanceLogger.Write(sw, nameof(Generate));
        }

        public IEnumerable<string> Dependencies => null;
    }
}