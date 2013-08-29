﻿/*
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
using System.Diagnostics;
using Rhetos.Dsl;
using System.Diagnostics.Contracts;
using Rhetos.Extensibility;
using Rhetos.Logging;

namespace Rhetos.Compiler
{
    public class CodeGenerator : ICodeGenerator
    {
        private readonly ILogger _performanceLogger;
        private readonly IDslModel _dslModel;
        private readonly ILogger _logger;

        public CodeGenerator(
            ILogProvider logProvider,
            IDslModel dslModel)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("CodeGenerator");
            _dslModel = dslModel;
        }


        public IAssemblySource ExecutePlugins<TPlugin>(IPluginsContainer<TPlugin> plugins, string tagOpen, string tagClose, IConceptCodeGenerator initialCodeGenerator)
            where TPlugin : IConceptCodeGenerator
        {
            var stopwatch = Stopwatch.StartNew();

            _performanceLogger.Write(stopwatch, "CodeGenerator: Plugins registered.");

            var codeBuilder = new CodeBuilder(tagOpen, tagClose);

            if (initialCodeGenerator != null)
                initialCodeGenerator.GenerateCode(null, codeBuilder);

            foreach (var conceptInfo in _dslModel.Concepts)
                foreach (var plugin in plugins.GetImplementations(conceptInfo.GetType()))
                {
                    try
                    {
                        plugin.GenerateCode(conceptInfo, codeBuilder);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.ToString());
                        _logger.Error("Part of the source code that was generated before the exception was thrown is written in the trace log.");
                        _logger.Trace(codeBuilder.GeneratedCode);
                        throw;
                    }
                }

            _performanceLogger.Write(stopwatch, "CodeGenerator: Code generated.");

            return codeBuilder;
        }
    }
}