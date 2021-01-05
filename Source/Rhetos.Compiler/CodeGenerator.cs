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

using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _logger = logProvider.GetLogger("CodeGenerator");
            _dslModel = dslModel;
        }

        public string ExecutePlugins<TPlugin>(IPluginsContainer<TPlugin> plugins, string tagOpen, string tagClose, IConceptCodeGenerator initialCodeGenerator)
            where TPlugin : IConceptCodeGenerator
        {
            var codeBuilder = BuildCode(plugins, tagOpen, tagClose, initialCodeGenerator);
            return codeBuilder.GenerateCode();
        }

        public IDictionary<string, string> ExecutePluginsToFiles<TPlugin>(IPluginsContainer<TPlugin> plugins, string tagOpen, string tagClose, IConceptCodeGenerator initialCodeGenerator)
            where TPlugin : IConceptCodeGenerator
        {
            var codeBuilder = BuildCode(plugins, tagOpen, tagClose, initialCodeGenerator);
            return codeBuilder.GeneratedCodeByFile;
        }

        private CodeBuilder BuildCode<TPlugin>(IPluginsContainer<TPlugin> plugins, string tagOpen, string tagClose, IConceptCodeGenerator initialCodeGenerator) where TPlugin : IConceptCodeGenerator
        {
            var stopwatch = Stopwatch.StartNew();

            var codeBuilder = new CodeBuilder(tagOpen, tagClose);

            if (initialCodeGenerator != null)
                initialCodeGenerator.GenerateCode(null, codeBuilder);

            var conceptImplementations = _dslModel.GetTypes()
                .ToDictionary(conceptType => conceptType, conceptType => plugins.GetImplementations(conceptType).ToList());
            _performanceLogger.Write(stopwatch, $"Get implementations for {typeof(TPlugin).FullName}.");

            var implementationStopwatches = conceptImplementations.SelectMany(ci => ci.Value)
                .Select(plugin => plugin.GetType())
                .Distinct()
                .ToDictionary(pluginType => pluginType, pluginType => new Stopwatch());

            foreach (var conceptInfo in _dslModel.Concepts)
                foreach (var plugin in conceptImplementations[conceptInfo.GetType()])
                {
                    try
                    {
                        var implementationStopwatch = implementationStopwatches[plugin.GetType()];
                        implementationStopwatch.Start();
                        plugin.GenerateCode(conceptInfo, codeBuilder);
                        implementationStopwatch.Stop();
                    }
                    catch (Exception e)
                    {
                        _logger.Info("Part of the source code that was generated before the exception was thrown is written in the trace log.");
                        _logger.Trace(() => codeBuilder.GenerateCode());
                        ExceptionsUtility.Rethrow(e);
                    }
                }

            foreach (var imp in implementationStopwatches.OrderByDescending(i => i.Value.Elapsed.TotalSeconds).Take(3))
                _performanceLogger.Write(imp.Value, () => $"{typeof(TPlugin).Name} total time for {imp.Key}.");

            _performanceLogger.Write(stopwatch, $"Code generated for {typeof(TPlugin).FullName}.");
            return codeBuilder;
        }
    }
}