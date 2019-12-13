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

using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.DatabaseGenerator
{
    public class ConceptDataMigrationExecuter : IConceptDataMigrationExecuter
    {
        private readonly ILogger _deployPackagesLogger;
        private readonly SqlTransactionBatches _sqlExecuter;
        RhetosAppOptions _rheosAppOptions;
        private readonly Lazy<GeneratedDataMigrationScripts> _scripts;

        public IEnumerable<string> Dependencies => new List<string>();

        public ConceptDataMigrationExecuter(
            ILogProvider logProvider,
            SqlTransactionBatches sqlExecuter,
            RhetosAppOptions rhetosAppOptions)
        {
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _sqlExecuter = sqlExecuter;
            _rheosAppOptions = rhetosAppOptions;
            _scripts = new Lazy<GeneratedDataMigrationScripts>(LoadScripts);
        }

        public void ExecuteBeforeDataMigrationScripts()
        {
            _deployPackagesLogger.Trace(() => $"Executing {_scripts.Value.BeforeDataMigration.Count()} before data migration scripts.");
            _sqlExecuter.Execute(_scripts.Value.BeforeDataMigration.Select(x => 
                new SqlTransactionBatches.SqlScript
                {
                    Sql = x,
                    IsBatch = true
                }));
        }

        public void ExecuteAfterDataMigrationScripts()
        {
            _deployPackagesLogger.Trace(() => $"Executing {_scripts.Value.AfterDataMigration.Count()} after data migration scripts.");
            _sqlExecuter.Execute(_scripts.Value.AfterDataMigration.Reverse().Select(x =>
                new SqlTransactionBatches.SqlScript
                {
                    Sql = x,
                    IsBatch = true
                }));
        }

        private GeneratedDataMigrationScripts LoadScripts()
        {
            var serializedConcepts = File.ReadAllText(Path.Combine(_rheosAppOptions.AssetsFolder, ConceptDataMigrationGenerator.ConceptDataMigrationScriptsFileName), Encoding.UTF8);
            return JsonConvert.DeserializeObject<GeneratedDataMigrationScripts>(serializedConcepts);
        }
    }
}