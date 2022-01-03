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
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dom
{
    public class DomGenerator : IGenerator
    {
        private readonly IPluginsContainer<IConceptCodeGenerator> _pluginRepository;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ISourceWriter _sourceWriter;
        private readonly InitialDomCodeGenerator _initialDomCodeGenerator;
        private readonly ILogger _logger;

        public DomGenerator(
            IPluginsContainer<IConceptCodeGenerator> plugins,
            ICodeGenerator codeGenerator,
            ISourceWriter sourceWriter,
            InitialDomCodeGenerator initialDomCodeGenerator,
            ILogProvider logProvider)
        {
            _pluginRepository = plugins;
            _codeGenerator = codeGenerator;
            _sourceWriter = sourceWriter;
            _initialDomCodeGenerator = initialDomCodeGenerator;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        public void Generate()
        {
            var sourceFiles = _codeGenerator.ExecutePluginsToFiles(_pluginRepository, "/*", "*/", _initialDomCodeGenerator);

            var summary = Enum.GetValues<SourceWriterResult>().ToDictionary(r => r, r => 0);
            foreach (var sourceFile in sourceFiles)
            {
                var result = _sourceWriter.Add(sourceFile.Key + ".cs", sourceFile.Value);
                summary[result]++;
            }

            _logger.Info(() => $"{summary[SourceWriterResult.Creating]} files created, {summary[SourceWriterResult.Updating]} files updated.");
        }
    }
}
