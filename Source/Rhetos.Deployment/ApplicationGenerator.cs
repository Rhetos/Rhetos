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
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence.NHibernate;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Deployment
{
    public class ApplicationGenerator
    {
        private readonly ILogger _deployPackagesLogger;
        private readonly ILogger _performanceLogger;
        private readonly ISqlExecuter _sqlExecuter;
        private readonly IDslModel _dslModel;
        private readonly IDomGenerator _domGenerator;
        private readonly IPluginsContainer<IGenerator> _generatorsContainer;
        private readonly DatabaseCleaner _databaseCleaner;
        private readonly DataMigration _dataMigration;
        private readonly IDatabaseGenerator _databaseGenerator;
        private readonly IDslScriptsProvider _dslScriptsLoader;
        private readonly INHibernateMapping _nHibernateMapping;

        public ApplicationGenerator(
            ILogProvider logProvider,
            ISqlExecuter sqlExecuter,
            IDslModel dslModel,
            IDomGenerator domGenerator,
            IPluginsContainer<IGenerator> generatorsContainer,
            DatabaseCleaner databaseCleaner,
            DataMigration dataMigration,
            IDatabaseGenerator databaseGenerator,
            IDslScriptsProvider dslScriptsLoader,
            INHibernateMapping nHibernateMapping)
        {
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _performanceLogger = logProvider.GetLogger("Performance");
            _sqlExecuter = sqlExecuter;
            _dslModel = dslModel;
            _domGenerator = domGenerator;
            _generatorsContainer = generatorsContainer;
            _databaseCleaner = databaseCleaner;
            _dataMigration = dataMigration;
            _databaseGenerator = databaseGenerator;
            _dslScriptsLoader = dslScriptsLoader;
            _nHibernateMapping = nHibernateMapping;
        }

        public void ExecuteGenerators()
        {
            _deployPackagesLogger.Trace("SQL connection string: " + SqlUtility.MaskPassword(SqlUtility.ConnectionString));
            ValidateDbConnection();

            _deployPackagesLogger.Trace("Preparing Rhetos database.");
            PrepareRhetosDatabase();

            _deployPackagesLogger.Trace("Parsing DSL scripts.");
            int dslModelConceptsCount = _dslModel.Concepts.Count();
            _deployPackagesLogger.Trace("Application model has " + dslModelConceptsCount + " statements.");

            _deployPackagesLogger.Trace("Compiling DOM assembly.");
            int generatedTypesCount = _domGenerator.Assembly.GetTypes().Length;
            if (generatedTypesCount == 0)
            {
                _deployPackagesLogger.Error("WARNING: Empty assembly is generated.");
            }
            else
                _deployPackagesLogger.Trace("Generated " + generatedTypesCount + " types.");

            var generators = GetSortedGenerators();
            foreach (var generator in generators)
            {
                _deployPackagesLogger.Trace("Executing " + generator.GetType().Name + ".");
                generator.Generate();
            }
            if (!generators.Any())
                _deployPackagesLogger.Trace("No additional generators.");

            _deployPackagesLogger.Trace("Cleaning old migration data.");
            _databaseCleaner.RemoveRedundantMigrationColumns();
            _databaseCleaner.RefreshDataMigrationRows();

            _deployPackagesLogger.Trace("Executing data migration scripts.");
            var dataMigrationReport = _dataMigration.ExecuteDataMigrationScripts();

            _deployPackagesLogger.Trace("Upgrading database.");
            try
            {
                _databaseGenerator.UpdateDatabaseStructure();
            }
            catch (Exception ex)
            {
                try
                {
                    _dataMigration.UndoDataMigrationScripts(dataMigrationReport.CreatedTags);
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

            _deployPackagesLogger.Trace("Uploading DSL scripts.");
            UploadDslScriptsToServer();

            _deployPackagesLogger.Trace("Generating NHibernate mapping.");
            File.WriteAllText(Paths.NHibernateMappingFile, _nHibernateMapping.GetMapping(), Encoding.Unicode);
        }

        private void ValidateDbConnection()
        {
            var connectionReport = new ConnectionStringReport(_sqlExecuter);
            if (!connectionReport.connectivity)
                throw (connectionReport.exceptionRaised);
            else if (!connectionReport.isDbo)
                throw (new FrameworkException("Current user does not have db_owner role for the database."));
        }

        private void PrepareRhetosDatabase()
        {
            string rhetosDatabaseScriptResourceName = "Rhetos.Deployment.RhetosDatabase." + SqlUtility.DatabaseLanguage + ".sql";
            var resourceStream = GetType().Assembly.GetManifestResourceStream(rhetosDatabaseScriptResourceName);
            if (resourceStream == null)
                throw new FrameworkException("Cannot find resource '" + rhetosDatabaseScriptResourceName + "'.");
            var sql = new StreamReader(resourceStream).ReadToEnd();

            var sqlScripts = sql.Split(new[] { "\r\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries).Where(s => !String.IsNullOrWhiteSpace(s));
            _sqlExecuter.ExecuteSql(sqlScripts);
        }

        private IList<IGenerator> GetSortedGenerators()
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var generators = _generatorsContainer.GetPlugins();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var genNames = generators.Select(gen => gen.GetType().FullName).ToList();
            var genDependencies = generators.SelectMany(gen => (gen.Dependencies ?? new string[0]).Select(x => Tuple.Create(x, gen.GetType().FullName)));
            Rhetos.Utilities.Graph.TopologicalSort(genNames, genDependencies);

            var sortedGenerators = generators.ToArray();
            Graph.SortByGivenOrder(sortedGenerators, genNames.ToArray(), gen => gen.GetType().FullName);
            return sortedGenerators;
        }

        private void UploadDslScriptsToServer()
        {
            List<string> sql = new List<string>();

            sql.Add(Sql.Get("DslScriptManager_Delete"));

            sql.AddRange(_dslScriptsLoader.DslScripts.Select(dslScript => Sql.Format(
                "DslScriptManager_Insert",
                SqlUtility.QuoteText(dslScript.Name),
                SqlUtility.QuoteText(dslScript.Script))));

            _sqlExecuter.ExecuteSql(sql);

            _deployPackagesLogger.Trace("Uploaded " + _dslScriptsLoader.DslScripts.Count() + " DSL scripts to database.");
        }
    }
}
