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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.IO;
using System.Linq;

namespace Rhetos.DatabaseGenerator
{
    public class ConceptDataMigrationExecuter : IConceptDataMigrationExecuter
    {
        private readonly ILogger _logger;
        private readonly SqlTransactionBatches _sqlExecuter;
        private readonly Lazy<GeneratedDataMigrationScripts> _scripts;
        private readonly RhetosAppOptions _rhetosAppOptions;

        public ConceptDataMigrationExecuter(
            ILogProvider logProvider,
            SqlTransactionBatches sqlExecuter,
            RhetosAppOptions rhetosAppOptions)
        {
            _logger = logProvider.GetLogger("ConceptDataMigration");
            _sqlExecuter = sqlExecuter;
            _scripts = new Lazy<GeneratedDataMigrationScripts>(LoadScripts);
            _rhetosAppOptions = rhetosAppOptions;
        }

        public void ExecuteBeforeDataMigrationScripts()
        {
            _logger.Info(() => $"Executing {_scripts.Value.BeforeDataMigration.Count()} before-data-migration scripts.");
            _sqlExecuter.Execute(_scripts.Value.BeforeDataMigration.Select(x => 
                new SqlTransactionBatches.SqlScript
                {
                    Sql = x,
                    IsBatch = true
                }));
        }

        public void ExecuteAfterDataMigrationScripts()
        {
            _logger.Info(() => $"Executing {_scripts.Value.AfterDataMigration.Count()} after-data-migration scripts.");
            _sqlExecuter.Execute(_scripts.Value.AfterDataMigration.Reverse().Select(x =>
                new SqlTransactionBatches.SqlScript
                {
                    Sql = x,
                    IsBatch = true
                }));
        }

        private GeneratedDataMigrationScripts LoadScripts()
        {
            return JsonUtility.DeserializeFromFile<GeneratedDataMigrationScripts>(Path.Combine(_rhetosAppOptions.AssetsFolder, ConceptDataMigrationGenerator.ConceptDataMigrationScriptsFileName));
        }
    }
}