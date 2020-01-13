﻿/*
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

using System;
using System.Linq;
using Rhetos.Compiler;
using System.Reflection;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using Rhetos.Logging;
using ICodeGenerator = Rhetos.Compiler.ICodeGenerator;
using System.Collections.Generic;

namespace Rhetos.Dom
{
    public class DomGenerator : IGenerator
    {
        private readonly IPluginsContainer<IConceptCodeGenerator> _pluginRepository;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IAssemblyGenerator _assemblyGenerator;

        /// <summary>
        /// If assemblyName is not null, the assembly will be saved on disk.
        /// If assemblyName is null, the assembly will be generated in memory.
        /// </summary>
        public DomGenerator(
            IPluginsContainer<IConceptCodeGenerator> plugins,
            ICodeGenerator codeGenerator,
            IAssemblyGenerator assemblyGenerator)
        {
            _pluginRepository = plugins;
            _codeGenerator = codeGenerator;
            _assemblyGenerator = assemblyGenerator;
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        public void Generate()
        {
            IAssemblySource assemblySource = _codeGenerator.ExecutePlugins(_pluginRepository, "/*", "*/", null);

            foreach (var sourcePart in SplitAssemblySource(assemblySource))
                _assemblyGenerator.Generate(sourcePart.AssemblySource, sourcePart.AssemblyFileName);
        }

        private IEnumerable<SourcePart> SplitAssemblySource(IAssemblySource assemblySource)
        {
            string source = assemblySource.GeneratedCode;
            List<string> references = assemblySource.RegisteredReferences.ToList();

            string partName = "";
            int partStart = 0;

            while (true)
            {
                int partEnd = source.IndexOf(DomGeneratorOptions.FileSplitterPrefix, partStart);
                if (partEnd == -1)
                    partEnd = source.Length;

                string partSource = source.Substring(partStart, partEnd - partStart).Trim();
                if (!string.IsNullOrEmpty(partSource))
                {
                    //TODO: Refactor tis so that it does not use the Paths class
                    string assemblyFile = Paths.GetDomAssemblyFile((DomAssemblies)Enum.Parse(typeof(DomAssemblies), partName));
                    yield return new SourcePart
                    {
                        AssemblyFileName = assemblyFile,
                        AssemblySource = new SimpleAssemplySource
                        {
                            GeneratedCode = partSource,
                            RegisteredReferences = references
                        }
                    };
                    references = references.ToList(); // Create a new list to avoid changing the one provided to the assembly above.
                    references.Add(assemblyFile);
                }

                if (partEnd == source.Length)
                    break;

                int tagStart = partEnd + DomGeneratorOptions.FileSplitterPrefix.Length;
                int tagEnd = source.IndexOf(DomGeneratorOptions.FileSplitterSuffix, tagStart);
                if (tagEnd == -1)
                    throw new FrameworkException($"Unexpected file splitter tag format. Cannot find tag end mark ({DomGeneratorOptions.FileSplitterSuffix})"
                        + $" after position {tagStart - partEnd} in source:\r\n"
                        + source.Substring(partEnd, 200));

                partName = source.Substring(tagStart, tagEnd - tagStart);
                partStart = tagEnd + DomGeneratorOptions.FileSplitterSuffix.Length;
            }
        }

        private class SourcePart
        {
            public string AssemblyFileName;
            public IAssemblySource AssemblySource;
        }

        private class SimpleAssemplySource : IAssemblySource
        {
            public string GeneratedCode { get; set; }
            public IEnumerable<string> RegisteredReferences { get; set; }
        }
    }
}
