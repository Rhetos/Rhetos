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

using Rhetos.DatabaseGenerator;
using Rhetos.Dsl;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.Deployment
{
    public class DatabaseDeployment
    {
        private readonly ILogger _deployPackagesLogger;
        private readonly ISqlExecuter _sqlExecuter;
        private readonly DatabaseCleaner _databaseCleaner;
        private readonly DataMigrationScriptsExecuter _dataMigrationScriptsExecuter;
        private readonly IDatabaseGenerator _databaseGenerator;
        private readonly IConceptDataMigrationExecuter _dataMigrationFromCodeExecuter;

        public DatabaseDeployment(
            ILogProvider logProvider,
            ISqlExecuter sqlExecuter,
            DatabaseCleaner databaseCleaner,
            DataMigrationScriptsExecuter dataMigrationScriptsExecuter,
            IDatabaseGenerator databaseGenerator,
            IConceptDataMigrationExecuter dataMigrationFromCodeExecuter)
        {
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _sqlExecuter = sqlExecuter;
            _databaseCleaner = databaseCleaner;
            _dataMigrationScriptsExecuter = dataMigrationScriptsExecuter;
            _databaseGenerator = databaseGenerator;
            _dataMigrationFromCodeExecuter = dataMigrationFromCodeExecuter;
        }

        public void UpdateDatabase()
        {
            _deployPackagesLogger.Trace("SQL connection: " + SqlUtility.SqlConnectionInfo(SqlUtility.ConnectionString));
            ConnectionStringReport.ValidateDbConnection(_sqlExecuter);

            _deployPackagesLogger.Trace("Preparing Rhetos database.");
            PrepareRhetosDatabase();

            _deployPackagesLogger.Trace("Cleaning old migration data.");
            _databaseCleaner.RemoveRedundantMigrationColumns();
            _databaseCleaner.RefreshDataMigrationRows();

            _dataMigrationFromCodeExecuter.ExecuteBeforeDataMigrationScripts();

            _deployPackagesLogger.Trace("Executing data migration scripts.");
            var dataMigrationReport = _dataMigrationScriptsExecuter.Execute();

            _dataMigrationFromCodeExecuter.ExecuteAfterDataMigrationScripts();

            _deployPackagesLogger.Trace("Upgrading database.");
            try
            {
                _databaseGenerator.UpdateDatabaseStructure();
            }
            catch (Exception ex)
            {
                try
                {
                    _dataMigrationScriptsExecuter.Undo(dataMigrationReport.CreatedTags);
                }
                catch (Exception undoException)
                {
                    _deployPackagesLogger.Error(undoException.ToString());
                }
                ExceptionsUtility.Rethrow(ex);
            }

            _deployPackagesLogger.Trace("Deleting redundant migration data.");
            _databaseCleaner.RemoveRedundantMigrationColumns();
            _databaseCleaner.RefreshDataMigrationRows();
        }

        private void PrepareRhetosDatabase()
        {
            string rhetosDatabaseScriptResourceName = "Rhetos.Deployment.RhetosDatabase." + SqlUtility.DatabaseLanguage + ".sql";
            var resourceStream = GetType().Assembly.GetManifestResourceStream(rhetosDatabaseScriptResourceName);
            if (resourceStream == null)
                throw new FrameworkException("Cannot find resource '" + rhetosDatabaseScriptResourceName + "'.");
            var sql = new StreamReader(resourceStream).ReadToEnd();

            var sqlScripts = SqlUtility.SplitBatches(sql);
            _sqlExecuter.ExecuteSql(sqlScripts);
        }
    }
}
