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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.Dom
{
    public class DomGenerator : IGenerator
    {
        private readonly IPluginsContainer<IConceptCodeGenerator> _pluginRepository;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IAssemblyGenerator _assemblyGenerator;
        private readonly BuildOptions _buildOptions;
        private readonly ISourceWriter _sourceWriter;

        /// <summary>
        /// If assemblyName is not null, the assembly will be saved on disk.
        /// If assemblyName is null, the assembly will be generated in memory.
        /// </summary>
        public DomGenerator(
            IPluginsContainer<IConceptCodeGenerator> plugins,
            ICodeGenerator codeGenerator,
            IAssemblyGenerator assemblyGenerator,
            BuildOptions buildOptions,
            ISourceWriter sourceWriter)
        {
            _pluginRepository = plugins;
            _codeGenerator = codeGenerator;
            _assemblyGenerator = assemblyGenerator;
            _buildOptions = buildOptions;
            _sourceWriter = sourceWriter;
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();

        public void Generate()
        {
            var sourceFiles = _codeGenerator.ExecutePluginsToFiles(_pluginRepository, "/*", "*/", null);

            if (!string.IsNullOrEmpty(_buildOptions.GeneratedSourceFolder))
            {
                foreach (var sourceFile in sourceFiles)
                    _sourceWriter.Add(sourceFile.Key + ".cs", sourceFile.Value.GeneratedCode);
            }
            else
            {
                var targetAssemblies = sourceFiles
                    .GroupBy(sourceFile => sourceFile.Key.Split('\\').First())
                    .Select(sourceFileGroup => new
                    {
                        Name = sourceFileGroup.Key,
                        AssemblySource = new AssemblySource
                        {
                            GeneratedCode = string.Join("\r\n", sourceFileGroup.OrderBy(sourceFile => sourceFile.Key).Select(sourceFile => sourceFile.Value.GeneratedCode)),
                            RegisteredReferences = sourceFileGroup.SelectMany(sourceFile => sourceFile.Value.RegisteredReferences).Distinct().OrderBy(name => name)
                        },
                        AssemblyFile = Paths.GetDomAssemblyFile((DomAssemblies)Enum.Parse(typeof(DomAssemblies), sourceFileGroup.Key)),
                    })
                    .ToList();

                Graph.SortByGivenOrder(targetAssemblies,
                    new string[] { DomAssemblies.Model.ToString(), DomAssemblies.Orm.ToString(), DomAssemblies.Repositories.ToString() },
                    targetAssembly => targetAssembly.Name);
                AddReferences(targetAssemblies[1].AssemblySource, new[] { targetAssemblies[0].AssemblyFile });
                AddReferences(targetAssemblies[2].AssemblySource, new[] { targetAssemblies[0].AssemblyFile, targetAssemblies[1].AssemblyFile });

                foreach (var targetAssembly in targetAssemblies)
                    _assemblyGenerator.Generate(targetAssembly.AssemblySource, targetAssembly.AssemblyFile);
            }
        }

        private void AddReferences(AssemblySource targetAssembly, string[] additionalReferences)
        {
            targetAssembly.RegisteredReferences = targetAssembly.RegisteredReferences.Concat(additionalReferences).ToList();
        }
    }
}
