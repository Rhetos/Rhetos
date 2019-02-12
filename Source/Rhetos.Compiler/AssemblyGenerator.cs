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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Logging;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;
using Rhetos.Dom;

namespace Rhetos.Compiler
{
    public class AssemblyGenerator : IAssemblyGenerator
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly Lazy<int> _errorReportLimit;
        private readonly GeneratedFilesCache _filesCache;
        private readonly DomGeneratorOptions _domGeneratorOptions;

        public AssemblyGenerator(ILogProvider logProvider, IConfiguration configuration,
            GeneratedFilesCache filesCache, DomGeneratorOptions domGeneratorOptions)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("AssemblyGenerator");
            _errorReportLimit = configuration.GetInt("AssemblyGenerator.ErrorReportLimit", 5);
            _filesCache = filesCache;
            _domGeneratorOptions = domGeneratorOptions;
        }

        public Assembly Generate(IAssemblySource assemblySource, string outputAssembly, List<ManifestResource> manifestResources = null)
        {
            var stopwatch = Stopwatch.StartNew();

            string dllName = Path.GetFileName(outputAssembly);

            // Save source file and it's hash value:
            string sourceCode = // The compiler parameters are included in the source, in order to invalidate the assembly cache when the parameters are changed.
                string.Concat(assemblySource.RegisteredReferences.Select(reference => $"// Reference: {reference}\r\n"))
                + $"// DomGeneratorOptions.Debug = \"{_domGeneratorOptions.Debug}\"\r\n\r\n"
                + assemblySource.GeneratedCode;

            string sourceFile = Path.GetFullPath(Path.ChangeExtension(outputAssembly, ".cs"));
            var sourceHash = _filesCache.SaveSourceAndHash(sourceFile, sourceCode);
            _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Save source and hash ({dllName}).");

            // Compile assembly or get from cache:
            Assembly generatedAssembly;

            var filesFromCache = _filesCache.RestoreCachedFiles(sourceFile, sourceHash, Path.GetDirectoryName(outputAssembly), new[] { ".dll", ".pdb" });
            if (filesFromCache != null)
            {
                _logger.Trace(() => "Restoring assembly from cache: " + dllName + ".");
                if (!File.Exists(outputAssembly))
                    throw new FrameworkException($"AssemblyGenerator: RestoreCachedFiles failed to create the assembly file ({dllName}).");

                generatedAssembly = Assembly.LoadFrom(outputAssembly);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Assembly from cache ({dllName}).");

                FailOnTypeLoadErrors(generatedAssembly, outputAssembly);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
            }
            else
            {
                _logger.Trace(() => "Compiling assembly: " + dllName + ".");

                var references = assemblySource.RegisteredReferences.Select(reference =>
                                    MetadataReference.CreateFromFile(reference)).ToList();

                var assemblyName = dllName.Replace(".dll", "");
                var assemblyCSharpCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                         assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                         moduleName: assemblyName,
                         optimizationLevel: _domGeneratorOptions.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release);

                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName, new[] { syntaxTree }, references, assemblyCSharpCompilationOptions);

                using (var dllStream = new MemoryStream())
                using (var pdbStream = new MemoryStream())
                {
                    var emitResult = compilation.Emit(dllStream, pdbStream, manifestResources:
                            manifestResources?.Select(x => new ResourceDescription(x.Name, () => File.OpenRead(x.Path), x.IsPublic)));
                    _performanceLogger.Write(stopwatch, $"AssemblyGenerator: CSharpCompilation.Create ({dllName}).");

                    FailOnCompilerErrors(emitResult, outputAssembly);

                    SaveGeneratedFile(dllStream, outputAssembly);
                    SaveGeneratedFile(pdbStream, Path.ChangeExtension(outputAssembly, ".pdb"));

                    generatedAssembly = Assembly.LoadFrom(outputAssembly);

                    FailOnTypeLoadErrors(generatedAssembly, outputAssembly);
                    ReportWarnings(emitResult, outputAssembly);
                    _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
                }

                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
            }

            return generatedAssembly;
        }

        private void SaveGeneratedFile(MemoryStream inputStream, string filePath)
        {
            inputStream.Seek(0, SeekOrigin.Begin);
            using (var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            {
                inputStream.WriteTo(fs);
            }
        }

        private void FailOnCompilerErrors(EmitResult emitResult, string sourcePath)
        {
            if (emitResult.Success)
                return;

            List<Diagnostic> errors = emitResult.Diagnostics.Where(x =>
                            x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error).ToList();

            if (!errors.Any())
                return;

            var report = new StringBuilder();
            var errorsCount = errors.Count();
            report.Append($"{errorsCount} error(s) while compiling '{Path.GetFileName(sourcePath)}'");

            if (errorsCount > _errorReportLimit.Value)
                report.AppendLine($". The first {_errorReportLimit.Value} errors:");
            else
                report.AppendLine(":");

            report.Append(string.Join("\n", errors.Take(_errorReportLimit.Value).Select(error => error.ToString())));

            if (errorsCount > _errorReportLimit.Value)
            {
                report.AppendLine();
                report.AppendLine("...");
            }

            throw new FrameworkException(report.ToString().Trim());
        }

        private void FailOnTypeLoadErrors(Assembly assembly, string assemblyPath)
        {
            try
            {
                assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new FrameworkException(CsUtility.ReportTypeLoadException(ex, "Error while compiling " + assemblyPath + "."), ex);
            }
        }

        private void ReportWarnings(EmitResult emitResult, string sourcePath)
        {
            List<Diagnostic> warnings = emitResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning).ToList();
            if (!warnings.Any())
                return;

            const string obsoleteInfo = "is obsolete: ";
            var warningGroups = warnings.GroupBy(warning =>
            {
                string groupKey = warning.Id;
                if (groupKey == "CS0618")
                {
                    var warningMessage = warning.GetMessage();
                    int obsoleteInfoStart = warningMessage.IndexOf(obsoleteInfo);
                    if (obsoleteInfoStart != -1)
                        groupKey += " " + warningMessage.Substring(obsoleteInfoStart + obsoleteInfo.Length);
                }
                return groupKey;
            });

            var sourcePathCs = Path.ChangeExtension(sourcePath, ".cs");
            foreach (var warningGroup in warningGroups)
            {
                var warning = warningGroup.First();
                var report = new StringBuilder();

                if (warningGroup.Count() > 1)
                    report.Append($"{warningGroup.Count()} warnings {warning.Id}: ");

                report.Append($"{warning.ToString()}, file: {sourcePathCs}");

                _logger.Info(report.ToString());
            }
        }
    }
}