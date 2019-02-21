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
using Microsoft.CodeAnalysis.Text;
using System.CodeDom.Compiler;
using System.Collections.Specialized;

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

        public Assembly Generate(IAssemblySource assemblySource, string outputAssemblyPath, IEnumerable<ManifestResource> manifestResources = null)
        {
            var stopwatch = Stopwatch.StartNew();

            string dllName = Path.GetFileName(outputAssemblyPath);

            // Save source file and it's hash value:
            string sourceCode = // The compiler parameters are included in the source, in order to invalidate the assembly cache when the parameters are changed.
                string.Concat(assemblySource.RegisteredReferences.Select(reference => $"// Reference: {reference}\r\n"))
                + $"// DomGeneratorOptions.Debug = \"{_domGeneratorOptions.Debug}\"\r\n\r\n"
                + assemblySource.GeneratedCode;

            string sourcePath = Path.GetFullPath(Path.ChangeExtension(outputAssemblyPath, ".cs"));
            var sourceHash = _filesCache.SaveSourceAndHash(sourcePath, sourceCode);
            _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Save source and hash ({dllName}).");

            // Compile assembly or get from cache:
            Assembly generatedAssembly;

            var filesFromCache = _filesCache.RestoreCachedFiles(sourcePath, sourceHash, Path.GetDirectoryName(outputAssemblyPath), new[] { ".dll", ".pdb" });
            if (filesFromCache != null)
            {
                _logger.Trace(() => "Restoring assembly from cache: " + dllName + ".");
                if (!File.Exists(outputAssemblyPath))
                    throw new FrameworkException($"AssemblyGenerator: RestoreCachedFiles failed to create the assembly file ({dllName}).");

                generatedAssembly = Assembly.LoadFrom(outputAssemblyPath);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Assembly from cache ({dllName}).");

                FailOnTypeLoadErrors(generatedAssembly, outputAssemblyPath);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
            }
            else
            {
                _logger.Trace(() => "Compiling assembly: " + dllName + ".");

                var references = assemblySource.RegisteredReferences.Select(reference =>
                                    MetadataReference.CreateFromFile(reference)).ToList();

                var assemblyName = GetAssemblyName(dllName);
                var assemblyCSharpCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                         assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                         moduleName: assemblyName,
                         // platform: Platform.AnyCpu, // compiler error: You must add a reference to assembly 'mscorlib, Version=2.0.5.0 ...' (we are referencing mscorlib, Version=4.0.0.0)
                         optimizationLevel: _domGeneratorOptions.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release);

                var encoding = new UTF8Encoding(true); // Encoding.UTF8;
                var buffer = encoding.GetBytes(sourceCode);
                var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    //sourceCode, null, sourcePath, encoding);
                    sourceText, new CSharpParseOptions(), sourcePath); // TODO: Ako ne treba sourceText, možda mogu koristiti stari sourceCode.
                var syntaxRootNode = syntaxTree.GetRoot() as CSharpSyntaxNode;
                var encodedsyntaxTree = CSharpSyntaxTree.Create(syntaxRootNode, null, sourcePath, encoding); // TODO: Jel ovo riješava problem

                var compilation = CSharpCompilation.Create(
                    assemblyName, new[] { encodedsyntaxTree }, references, assemblyCSharpCompilationOptions);

                using (var dllStream = new MemoryStream())
                using (var pdbStream = new MemoryStream())
                {
                    var pdbPath = Path.ChangeExtension(outputAssemblyPath, ".pdb");

                    var emitResult = compilation.Emit(
                        dllStream,
                        pdbStream,
                        manifestResources: manifestResources?.Select(x => new ResourceDescription(x.Name, () => File.OpenRead(x.Path), x.IsPublic)),

                        // TODO: Provjeriti jel embeddedTexts potreban.
                        /////embeddedTexts: new List<EmbeddedText> { EmbeddedText.FromSource(sourcePath, sourceText) },

                        options: new EmitOptions(
                            debugInformationFormat: DebugInformationFormat.Pdb, // TODO: Probati vratiti PortablePdb
                            pdbFilePath: pdbPath));
                    _performanceLogger.Write(stopwatch, $"AssemblyGenerator: CSharpCompilation.Create ({dllName}).");

                    FailOnCompilerErrors(emitResult, outputAssemblyPath);

                    SaveGeneratedFile(dllStream, outputAssemblyPath);
                    SaveGeneratedFile(pdbStream, pdbPath);

                    generatedAssembly = Assembly.LoadFrom(outputAssemblyPath);

                    FailOnTypeLoadErrors(generatedAssembly, outputAssemblyPath);
                    ReportWarnings(emitResult, outputAssemblyPath);
                    _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
                }
            }

            return generatedAssembly;
        }

        private static string GetAssemblyName(string dllName)
        {
            const string extension = ".dll";
            if (!dllName.EndsWith(extension))
                throw new FrameworkException($"DLL name '{dllName}' does not end with an extension '{extension}'.");
            return dllName.Substring(0, dllName.Length - extension.Length);
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
            report.Append($"{errors.Count} error(s) while compiling '{Path.GetFileName(sourcePath)}'");

            if (errors.Count > _errorReportLimit.Value)
                report.AppendLine($". The first {_errorReportLimit.Value} errors:");
            else
                report.AppendLine(":");

            report.Append(string.Join("\n", errors.Take(_errorReportLimit.Value).Select(error => error.ToString())));

            if (errors.Count > _errorReportLimit.Value)
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

        [Obsolete("See the description in IAssemblyGenerator.")]
        public Assembly Generate(IAssemblySource assemblySource, CompilerParameters compilerParameters)
        {
            var resources = compilerParameters.EmbeddedResources.Cast<string>()
                .Select(path => new ManifestResource { Name = Path.GetFileName(path), Path = path, IsPublic = true })
                .ToList();
            return Generate(assemblySource, compilerParameters.OutputAssembly, resources);
        }
    }
}