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
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Rhetos.Utilities;
using Rhetos.Logging;
using System.Text.RegularExpressions;

namespace Rhetos.Compiler
{
    public class AssemblyGenerator : IAssemblyGenerator
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly Lazy<int> _errorReportLimit;

        public AssemblyGenerator(ILogProvider logProvider, IConfiguration configuration)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("AssemblyGenerator");
            _errorReportLimit = configuration.GetInt("AssemblyGenerator.ErrorReportLimit", 5);
        }

        public Assembly Generate(IAssemblySource assemblySource, CompilerParameters compilerParameters)
        {
            var stopwatch = Stopwatch.StartNew();

            compilerParameters.ReferencedAssemblies.AddRange(assemblySource.RegisteredReferences.ToArray());
            if (compilerParameters.WarningLevel == -1)
                compilerParameters.WarningLevel = 4;

            string sourceFile = null;
            CompilerResults results;
            if (compilerParameters.GenerateInMemory)
            {
                using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
                    results = codeProvider.CompileAssemblyFromSource(compilerParameters, assemblySource.GeneratedCode);
            }
            else
            {
                sourceFile = Path.GetFullPath(Path.ChangeExtension(compilerParameters.OutputAssembly, ".cs"));
                File.WriteAllText(sourceFile, assemblySource.GeneratedCode, Encoding.UTF8);
                using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
                    results = codeProvider.CompileAssemblyFromFile(compilerParameters, sourceFile);
            }

            _performanceLogger.Write(stopwatch, "CSharpCodeProvider.CompileAssemblyFromSource");

            if (results.Errors.HasErrors)
                throw new FrameworkException(ReportErrors(results, assemblySource.GeneratedCode, sourceFile));

            try
            {
                results.CompiledAssembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new FrameworkException(CsUtility.ReportTypeLoadException(ex, "Error while compiling " + compilerParameters.OutputAssembly + "."), ex);
            }

            ReportWarnings(results, sourceFile);

            return results.CompiledAssembly;
        }

        private string ReportErrors(CompilerResults results, string generatedCode, string filePath)
        {
            var errors = (from System.CodeDom.Compiler.CompilerError error in results.Errors
                          where !error.IsWarning
                          select error).ToList();

            var report = new StringBuilder();
            report.Append(errors.Count + " errors while compiling '" + Path.GetFileName(filePath) + "'");

            if (errors.Count > _errorReportLimit.Value)
                report.AppendLine(". The first " + _errorReportLimit.Value + " errors:");
            else
                report.AppendLine(":");

            foreach (var error in errors.Take(_errorReportLimit.Value))
            {
                report.AppendLine();
                report.AppendLine(error.ErrorText);
                if (error.Line > 0 || error.Column > 0)
                    report.AppendLine(ScriptPositionReporting.ReportPosition(generatedCode, error.Line, error.Column, filePath));
            }

            if (errors.Count() > _errorReportLimit.Value)
            {
                report.AppendLine();
                report.AppendLine("...");
            }

            return report.ToString().Trim();
        }

        private void ReportWarnings(CompilerResults results, string filePath)
        {
            var warnings = (from System.CodeDom.Compiler.CompilerError error in results.Errors
                            where error.IsWarning
                            select error).ToList();

            var warningGroups = warnings.GroupBy(warning =>
                {
                    string groupKey = warning.ErrorNumber;
                    if (groupKey == "CS0618")
                    {
                        const string obsoleteInfo = "is obsolete: ";
                        int obsoleteInfoStart = warning.ErrorText.IndexOf(obsoleteInfo);
                        if (obsoleteInfoStart != -1)
                            groupKey += " " + warning.ErrorText.Substring(obsoleteInfoStart + obsoleteInfo.Length);
                    }
                    return groupKey;
                });

            foreach (var warningGroup in warningGroups)
            {
                var warning = warningGroup.First();
                var report = new StringBuilder();

                if (warningGroup.Count() > 1)
                    report.AppendFormat("{0} warnings", warningGroup.Count());
                else
                    report.Append("Warning");

                report.AppendFormat(" {0}: {1}.", warning.ErrorNumber, warning.ErrorText);

                if (!string.IsNullOrEmpty(warning.FileName))
                    report.AppendFormat(" At line {0}, column {1}, file '{2}'.", warning.Line, warning.Column, warning.FileName);
                
                _logger.Info(report.ToString());
            }
        }
    }
}