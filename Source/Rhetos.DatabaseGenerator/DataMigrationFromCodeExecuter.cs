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
    public class DataMigrationFromCodeExecuter : IDataMigrationFromCodeExecuter
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly SqlTransactionBatches _sqlExecuter;
        private readonly IDslModel _dslModel;
        private readonly IPluginsContainer<IDataMigrationScript> _plugins;

        DataMigrationScriptBuilder _codeBuilder;

        public DataMigrationFromCodeExecuter(
            ILogProvider logProvider,
            SqlTransactionBatches sqlExecuter,
            IDslModel dslModel,
            IPluginsContainer<IDataMigrationScript> plugins)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DataMigrationScriptGenerator");
            _sqlExecuter = sqlExecuter;
            _dslModel = dslModel;
            _plugins = plugins;
        }

        public void ExecuteBeforeDataMigrationScripts()
        {
            Initialize();
            _sqlExecuter.Execute(_codeBuilder.GetBeforeDataMigartionScript().Select(x => 
                new SqlTransactionBatches.SqlScript {
                    Sql = x, IsBatch = true
                }));
        }

        public void ExecuteAfterDataMigrationScripts()
        {
            Initialize();
            _sqlExecuter.Execute(_codeBuilder.GetAfterDataMigartionScript().Reverse().Select(x =>
                new SqlTransactionBatches.SqlScript
                {
                    Sql = x,
                    IsBatch = true
                }));
        }

        private void Initialize()
        {
            if (_codeBuilder == null)
                _codeBuilder = ExecutePlugins();
        }

        private DataMigrationScriptBuilder ExecutePlugins()
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
                        _logger.Error(ex.ToString());
                        _logger.Error("Part of the source code that was generated before the exception was thrown is written in the trace log.");
                        _logger.Trace(codeBuilder.GeneratedCode);
                        throw;
                    }
                }

            _performanceLogger.Write(stopwatch, "DataMigrationScriptGenerator: Scripts generated.");

            return codeBuilder;
        }
    }
}