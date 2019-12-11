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

using Rhetos.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Compiler
{
    public class SimpleAssemblyGenerator : IAssemblyGenerator
    {
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public SimpleAssemblyGenerator(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(nameof(SimpleAssemblyGenerator));
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public Assembly Generate(IAssemblySource assemblySource, string outputAssemblyPath, IEnumerable<ManifestResource> manifestResources = null)
        {
            var stopwatch = Stopwatch.StartNew();

            manifestResources = manifestResources ?? Array.Empty<ManifestResource>();

            // Save source file and it's hash value:
            string sourceCode = // The compiler parameters are included in the source, in order to invalidate the assembly cache when the parameters are changed.
                string.Concat(assemblySource.RegisteredReferences.Select(reference => $"// Reference: {PathAndVersion(reference)}\r\n"))
                + string.Concat(manifestResources.Select(resource => $"// Resource: \"{resource.Name}\", {PathAndVersion(resource.Path)}\r\n"))
                + assemblySource.GeneratedCode;

            string sourcePath = Path.GetFullPath(Path.ChangeExtension(outputAssemblyPath, ".cs"));
            File.WriteAllText(sourcePath, sourceCode);
            _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Save source ({sourcePath}).");

            _logger.Info("Skipped generating assembly.");

            return null;
        }

        [Obsolete("See the description in IAssemblyGenerator.")]
        public Assembly Generate(IAssemblySource assemblySource, CompilerParameters compilerParameters)
        {
            var resources = compilerParameters.EmbeddedResources.Cast<string>()
                .Select(path => new ManifestResource { Name = Path.GetFileName(path), Path = path, IsPublic = true })
                .ToList();
            return Generate(assemblySource, compilerParameters.OutputAssembly, resources);
        }

        private string PathAndVersion(string path)
        {
            var file = new FileInfo(path);
            if (file.Exists)
                return $"{path}, {file.LastWriteTime.ToString("o")}";
            else
                return path;
        }
    }
}