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

using Rhetos.Extensibility;
using Rhetos.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rhetos.Dsl
{
    public class DslSyntaxFileGenerator : IGenerator
    {
        private readonly DslSyntax _dslSyntax;
        private readonly DslSyntaxFile _dslSyntaxFile;
        private readonly ILogger _performanceLogger;
        public static readonly string DslSyntaxFileName = "DslSyntax.json";

        public DslSyntaxFileGenerator(DslSyntax dslSyntax, DslSyntaxFile dslSyntaxFile, ILogProvider logProvider)
        {
            _dslSyntax = dslSyntax;
            _dslSyntaxFile = dslSyntaxFile;
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
        }

        public void Generate()
        {
            var sw = Stopwatch.StartNew();
            _dslSyntaxFile.Save(_dslSyntax);
            _performanceLogger.Write(sw, nameof(Generate));
        }

        public IEnumerable<string> Dependencies => null;
    }
}