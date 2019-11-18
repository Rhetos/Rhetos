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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Rhetos.Dom;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rhetos.Compiler
{
    public class AssemblyGenerator : IAssemblyGenerator
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly int _errorReportLimit;
        private readonly GeneratedFilesCache _filesCache;
        private readonly DomGeneratorOptions _domGeneratorOptions;

        public AssemblyGenerator(ILogProvider logProvider, IConfigurationProvider configurationProvider,
            GeneratedFilesCache filesCache, DomGeneratorOptions domGeneratorOptions)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("AssemblyGenerator");
            _errorReportLimit = configurationProvider.GetValue("AssemblyGenerator.ErrorReportLimit", 5);
            _filesCache = filesCache;
            _domGeneratorOptions = domGeneratorOptions;
        }

        public Assembly Generate(IAssemblySource assemblySource, string outputAssemblyPath, IEnumerable<ManifestResource> manifestResources = null)
        {
            var stopwatch = Stopwatch.StartNew();

            manifestResources = manifestResources ?? Array.Empty<ManifestResource>();

            string dllName = Path.GetFileName(outputAssemblyPath);

            // Save source file and it's hash value:
            string sourceCode = // The compiler parameters are included in the source, in order to invalidate the assembly cache when the parameters are changed.
                string.Concat(assemblySource.RegisteredReferences.Select(reference => $"// Reference: {PathAndVersion(reference)}\r\n"))
                + string.Concat(manifestResources.Select(resource => $"// Resource: \"{resource.Name}\", {PathAndVersion(resource.Path)}\r\n"))
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

                FailOnTypeLoadErrors(generatedAssembly, outputAssemblyPath, assemblySource.RegisteredReferences);
                _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
            }
            else
            {
                _logger.Trace(() => "Compiling assembly: " + dllName + ".");

                var references = assemblySource.RegisteredReferences
                    .Select(reference => MetadataReference.CreateFromFile(reference)).ToList();

                var assemblyName = GetAssemblyName(dllName);
                var assemblyCSharpCompilationOptions = new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                    moduleName: assemblyName,
                    optimizationLevel: _domGeneratorOptions.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release);

                var encoding = new UTF8Encoding(true); // This encoding is used when saving the source file.
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, null, sourcePath, encoding);
                var compilation = CSharpCompilation.Create(assemblyName, new[] { syntaxTree }, references, assemblyCSharpCompilationOptions);

                using (var dllStream = new MemoryStream())
                using (var pdbStream = new MemoryStream())
                {
                    var pdbPath = Path.ChangeExtension(outputAssemblyPath, ".pdb");

                    var resources = manifestResources.Select(x => new ResourceDescription(x.Name, () => File.OpenRead(x.Path), x.IsPublic));
                    var options = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: pdbPath);
                    var emitResult = compilation.Emit(dllStream, pdbStream, manifestResources: resources, options: options);
                    _performanceLogger.Write(stopwatch, $"AssemblyGenerator: CSharpCompilation.Create ({dllName}).");

                    ReportWarnings(emitResult, outputAssemblyPath);
                    FailOnCompilerErrors(emitResult, sourceCode, sourcePath, outputAssemblyPath);

                    SaveGeneratedFile(dllStream, outputAssemblyPath);
                    SaveGeneratedFile(pdbStream, pdbPath);

                    generatedAssembly = Assembly.LoadFrom(outputAssemblyPath);

                    FailOnTypeLoadErrors(generatedAssembly, outputAssemblyPath, assemblySource.RegisteredReferences);
                    _performanceLogger.Write(stopwatch, $"AssemblyGenerator: Report errors ({dllName}).");
                }
            }

            return generatedAssembly;
        }

        private string PathAndVersion(string path)
        {
            var file = new FileInfo(path);
            if (file.Exists)
                return $"{path}, {file.LastWriteTime.ToString("o")}";
            else
                return path;
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

        private void FailOnCompilerErrors(EmitResult emitResult, string sourceCode, string sourcePath, string outputAssemblyPath)
        {
            if (emitResult.Success)
                return;

            var errors = emitResult.Diagnostics
                .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (!errors.Any())
                return;

            var report = new StringBuilder();
            report.Append($"{errors.Count} error(s) while compiling {Path.GetFileName(outputAssemblyPath)}");

            if (errors.Count > _errorReportLimit)
                report.AppendLine($". The first {_errorReportLimit} errors:");
            else
                report.AppendLine(":");

            report.Append(string.Join("\r\n",
                errors.Take(_errorReportLimit).Select(error => error.ToString() + ReportContext(error, sourceCode, sourcePath))));

            if (errors.Count > _errorReportLimit)
            {
                report.AppendLine();
                report.AppendLine("...");
            }

            throw new FrameworkException(report.ToString());
        }

        private string ReportContext(Diagnostic error, string sourceCode, string sourcePath)
        {
            if (error.Location.IsInSource && error.Location.SourceTree.FilePath == sourcePath)
            {
                var span = error.Location.SourceSpan;
                return "\r\n" + ScriptPositionReporting.ReportPreviousAndFollowingText(sourceCode, span.Start);
            }
            else
                return "";
        }

        private void FailOnTypeLoadErrors(Assembly assembly, string outputAssemblyPath, IEnumerable<string> referencedAssembliesPaths)
        {
            try
            {
                assembly.GetTypes();
            }
            catch (Exception ex)
            {
                string contextInfo = $"Error while compiling {Path.GetFileName(outputAssemblyPath)}.";
                string typeLoadReport = CsUtility.ReportTypeLoadException(ex, contextInfo, referencedAssembliesPaths);
                if (typeLoadReport != null)
                    throw new FrameworkException(typeLoadReport, ex);
                else
                    ExceptionsUtility.Rethrow(ex);
            }
        }

        private void ReportWarnings(EmitResult emitResult, string outputAssemblyPath)
        {
            List<Diagnostic> warnings = emitResult.Diagnostics
                .Where(x => x.Severity == DiagnosticSeverity.Warning)
                .ToList();

            if (!warnings.Any())
                return;

            string warningDetails = string.Join("\r\n", warnings
                .GroupBy(warning => warning.Id + DescriptionIfObsolete(warning))
                .Select(warningGroup => $"{warningGroup.First()}{MultipleWarningsInfo(warningGroup.Count())}"));

            _logger.Info($"{warnings.Count} warning(s) while compiling {Path.GetFileName(outputAssemblyPath)}:\r\n{warningDetails}");
        }

        private string MultipleWarningsInfo(int count)
        {
            if (count == 1)
                return "";
            else
                return $" ({count} warnings)";
        }

        private static string DescriptionIfObsolete(Diagnostic warning)
        {
            if (warning.Id == "CS0612") // Grouping obsolete warnings by description.
                return warning.GetMessage();
            else if (warning.Id == "CS0618") // Grouping obsolete warnings with custom message by custom message, to avoid spamming the log for each generated class.
            {
                const string obsoleteInfoTag = "is obsolete: ";
                var warningMessage = warning.GetMessage();
                int obsoleteInfoStart = warningMessage.IndexOf(obsoleteInfoTag);
                if (obsoleteInfoStart != -1)
                    return " " + warningMessage.Substring(obsoleteInfoStart + obsoleteInfoTag.Length);
            }
            return "";
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