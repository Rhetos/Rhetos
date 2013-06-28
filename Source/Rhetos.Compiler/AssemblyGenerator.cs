/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Compiler
{
    public class AssemblyGenerator : IAssemblyGenerator
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;

        public AssemblyGenerator(ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("AssemblyGenerator");
        }

        public Assembly Generate(IAssemblySource assemblySource, CompilerParameters compilerParameters)
        {
            compilerParameters.ReferencedAssemblies.AddRange(assemblySource.RegisteredReferences.ToArray());
            var stopwatch = Stopwatch.StartNew();
            CompilerResults results;

            string sourceFile = null;
            if (compilerParameters.GenerateInMemory)
            {
                using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
                    results = codeProvider.CompileAssemblyFromSource(compilerParameters, assemblySource.GeneratedCode);
            }
            else
            {
                sourceFile = Path.GetFullPath(Path.ChangeExtension(compilerParameters.OutputAssembly, ".cs"));
                File.WriteAllText(sourceFile, assemblySource.GeneratedCode);
                using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
                    results = codeProvider.CompileAssemblyFromFile(compilerParameters, sourceFile);
            }

            _performanceLogger.Write(stopwatch, "CSharpCodeProvider.CompileAssemblyFromSource");

            if (results.Errors.HasErrors)
                throw new FrameworkException(GetErrorDescription(results, assemblySource.GeneratedCode, sourceFile));
            try
            {
                results.CompiledAssembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (ex.LoaderExceptions.Any())
                    LogAndRethrowLoaderExceptions(ex);
                else
                    throw;
            }

            return results.CompiledAssembly;
        }

        private void LogAndRethrowLoaderExceptions(ReflectionTypeLoadException ex)
        {
            // Log all loader exceptions
            _logger.Error(ex.ToString());
            foreach (string le in ex.LoaderExceptions.Select(le => le.ToString()).Distinct())
                _logger.Error(le);

            // Rethrows exception in simplified format so that enough basic information can be seen from client application
            string errors = string.Join("\r\n", ex.LoaderExceptions.Select(le => le.Message).Distinct());
            if (errors.Length > 1500)
                errors = errors.Substring(0, 1500) + "...";
            throw new FrameworkException("Unable to resolve one or more types from assembly. Reason:\r\n" + errors,
                new FrameworkException(string.Format("{0}\r\n\r\nFirst of {1} loader exceptions:\r\n{2}",
                    ex.Message,
                    ex.LoaderExceptions.Count(),
                    ex.LoaderExceptions[0])));
        }

        private static string GetErrorDescription(CompilerResults results, string generatedCode, string filePath)
        {
            StringBuilder sb = new StringBuilder("Error while generating assembly.");
            sb.AppendLine();
            var errors = (from System.CodeDom.Compiler.CompilerError err in results.Errors
                          where !err.IsWarning
                          select err);
            foreach (var err in errors.Take(5))
            {
                sb.AppendLine();
                sb.AppendLine(err.ErrorText);
                if (err.Line > 0 || err.Column > 0)
                    sb.AppendLine(ScriptPositionReporting.ReportPosition(generatedCode, err.Line, err.Column, filePath));
            }
            if (errors.Count() > 5)
                sb.AppendLine("...");
            string msg = sb.ToString();
            return msg;
        }

    }
}