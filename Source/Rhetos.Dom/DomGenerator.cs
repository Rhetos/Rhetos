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

using Rhetos.Compiler;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;

namespace Rhetos.Dom
{
    public class DomGenerator : IGenerator
    {
        private readonly IPluginsContainer<IConceptCodeGenerator> _pluginRepository;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ISourceWriter _sourceWriter;

        public DomGenerator(
            IPluginsContainer<IConceptCodeGenerator> plugins,
            ICodeGenerator codeGenerator,
            ISourceWriter sourceWriter)
        {
            _pluginRepository = plugins;
            _codeGenerator = codeGenerator;
            _sourceWriter = sourceWriter;
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        public void Generate()
        {
            var sourceFiles = _codeGenerator.ExecutePluginsToFiles(_pluginRepository, "/*", "*/", null);

            foreach (var sourceFile in sourceFiles)
                _sourceWriter.Add(sourceFile.Key + ".cs", sourceFile.Value);
        }
    }
}
