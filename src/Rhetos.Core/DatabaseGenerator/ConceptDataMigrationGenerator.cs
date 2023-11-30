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

using Newtonsoft.Json;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Generates data migration script from provided DSL concept implementation plugins.
    /// </summary>
    public class ConceptDataMigrationGenerator : IGenerator
    {
        public const string ConceptDataMigrationScriptsFileName = "ConceptDataMigrationScripts.json";

        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly IDslModel _dslModel;
        private readonly IPluginsContainer<IConceptDataMigration> _plugins;
        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;

        public IEnumerable<string> Dependencies => new List<string>();

        public ConceptDataMigrationGenerator(
            ILogProvider logProvider,
            IDslModel dslModel,
            RhetosBuildEnvironment rhetosBuildEnvironment,
            IPluginsContainer<IConceptDataMigration> plugins)
        {
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _logger = logProvider.GetLogger(GetType().Name);
            _dslModel = dslModel;
            _plugins = plugins;
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
        }

        public void Generate()
        {
            var stopwatch = Stopwatch.StartNew();

            var codeBuilder = new DataMigrationScriptBuilder();

            var conceptImplementations = _dslModel.GetTypes()
                .ToDictionary(conceptType => conceptType, conceptType => _plugins.GetImplementations(conceptType).ToList());

            foreach (var conceptInfo in _dslModel.Concepts)
                foreach (var plugin in conceptImplementations[conceptInfo.GetType()])
                {
                    try
                    {
                        plugin.GenerateCode(conceptInfo, codeBuilder);
                    }
                    catch (Exception ex)
                    {
                        _logger.Info("Part of the source code that was generated before the exception was thrown is written in the trace log.");
                        _logger.Trace(() => codeBuilder.GenerateCode());
                        throw new FrameworkException($"Error while generating data-migration script for '{conceptInfo.GetUserDescription()}'.", ex);
                    }
                }

            _performanceLogger.Write(stopwatch, "Scripts generated.");

            string serializedConcepts = JsonConvert.SerializeObject(codeBuilder.GetDataMigrationScripts(), Formatting.Indented);
            File.WriteAllText(Path.Combine(_rhetosBuildEnvironment.GeneratedAssetsFolder, ConceptDataMigrationScriptsFileName), serializedConcepts, Encoding.UTF8);
        }
    }
}