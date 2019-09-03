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
using System.Diagnostics;
using System.Linq;

namespace Rhetos.DatabaseGenerator
{
    public class ConceptDataMigrationExecuter : IConceptDataMigrationExecuter
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly SqlTransactionBatches _sqlExecuter;
        private readonly IDslModel _dslModel;
        private readonly IPluginsContainer<IConceptDataMigration> _plugins;
        private readonly Lazy<GeneratedDataMigrationScripts> _scripts;

        public ConceptDataMigrationExecuter(
            ILogProvider logProvider,
            SqlTransactionBatches sqlExecuter,
            IDslModel dslModel,
            IPluginsContainer<IConceptDataMigration> plugins)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger(GetType().Name);
            _sqlExecuter = sqlExecuter;
            _dslModel = dslModel;
            _plugins = plugins;
            _scripts = new Lazy<GeneratedDataMigrationScripts>(ExecutePlugins);
        }

        public void ExecuteBeforeDataMigrationScripts()
        {
            _sqlExecuter.Execute(_scripts.Value.BeforeDataMigration.Select(x => 
                new SqlTransactionBatches.SqlScript
                {
                    Sql = x,
                    IsBatch = true
                }));
        }

        public void ExecuteAfterDataMigrationScripts()
        {
            _sqlExecuter.Execute(_scripts.Value.AfterDataMigration.Reverse().Select(x =>
                new SqlTransactionBatches.SqlScript
                {
                    Sql = x,
                    IsBatch = true
                }));
        }

        private GeneratedDataMigrationScripts ExecutePlugins()
        {
            var stopwatch = Stopwatch.StartNew();

            var codeBuilder = new DataMigrationScriptBuilder();

            foreach (var conceptInfo in _dslModel.Concepts)
                foreach (var plugin in _plugins.GetImplementations(conceptInfo.GetType()))
                {
                    try
                    {
                        plugin.GenerateCode(conceptInfo, codeBuilder);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Part of the source code that was generated before the exception was thrown is written in the trace log.");
                        _logger.Trace(codeBuilder.GeneratedCode);
                        throw new FrameworkException($"Error while generating data-migration script for '{conceptInfo.GetUserDescription()}'.", ex);
                    }
                }

            _logger.Trace(codeBuilder.GeneratedCode);

            _performanceLogger.Write(stopwatch, "DataMigrationScriptGenerator: Scripts generated.");

            return codeBuilder.GetDataMigrationScripts();
        }
    }
}