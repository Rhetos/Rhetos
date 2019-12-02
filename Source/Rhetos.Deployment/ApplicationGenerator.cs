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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.Deployment
{
    public class ApplicationGenerator
    {
        private readonly ILogger _deployPackagesLogger;
        private readonly ILogger _performanceLogger;
        private readonly ISqlExecuter _sqlExecuter;
        private readonly IDslModel _dslModel;
        private readonly IDomainObjectModel _domGenerator;
        private readonly IPluginsContainer<IGenerator> _generatorsContainer;
        private readonly DatabaseCleaner _databaseCleaner;
        private readonly DataMigrationScripts _dataMigration;
        private readonly IDatabaseGenerator _databaseGenerator;
        private readonly IDslScriptsProvider _dslScriptsLoader;
        private readonly IConceptDataMigrationExecuter _dataMigrationFromCodeExecuter;
        private readonly DeployOptions _deployOptions;

        public ApplicationGenerator(
            ILogProvider logProvider,
            ISqlExecuter sqlExecuter,
            IDslModel dslModel,
            IDomainObjectModel domGenerator,
            IPluginsContainer<IGenerator> generatorsContainer,
            DatabaseCleaner databaseCleaner,
            DataMigrationScripts dataMigration,
            IDatabaseGenerator databaseGenerator,
            IDslScriptsProvider dslScriptsLoader,
            IConceptDataMigrationExecuter dataMigrationFromCodeExecuter,
            DeployOptions deployOptions)
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
            _dataMigrationFromCodeExecuter = dataMigrationFromCodeExecuter;
            _deployOptions = deployOptions;
        }

        public void ExecuteGenerators()
        {
            CheckDslModelErrors();

            _deployPackagesLogger.Trace("Compiling DOM assembly.");
            int generatedTypesCount = _domGenerator.GetTypes().Count();
            if (generatedTypesCount == 0)
            {
                _deployPackagesLogger.Info("Warning: Empty assembly is generated.");
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
        }

        /// <summary>
        /// Creating the DSL model instance *before* executing code generators, to proved better error reporting
        /// and make it clear that a code generator did not cause a parser error.
        /// </summary>
        public void CheckDslModelErrors()
        {
            _deployPackagesLogger.Trace("Parsing DSL scripts.");
            int dslModelConceptsCount = _dslModel.Concepts.Count();
            _deployPackagesLogger.Trace("Application model has " + dslModelConceptsCount + " statements.");
        }

        public void UpdateDatabase()
        {
            _deployPackagesLogger.Trace("SQL connection: " + SqlUtility.SqlConnectionInfo(SqlUtility.ConnectionString));
            ValidateDbConnection();

            _deployPackagesLogger.Trace("Preparing Rhetos database.");
            PrepareRhetosDatabase();

            _deployPackagesLogger.Trace("Cleaning old migration data.");
            _databaseCleaner.RemoveRedundantMigrationColumns();
            _databaseCleaner.RefreshDataMigrationRows();

            _dataMigrationFromCodeExecuter.ExecuteBeforeDataMigrationScripts();

            _deployPackagesLogger.Trace("Executing data migration scripts.");
            var dataMigrationReport = _dataMigration.Execute();

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
                    _dataMigration.Undo(dataMigrationReport.CreatedTags);
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

            var sqlScripts = SqlUtility.SplitBatches(sql);
            _sqlExecuter.ExecuteSql(sqlScripts);
        }

        private IList<IGenerator> GetSortedGenerators()
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var generators = _generatorsContainer.GetPlugins().ToArray();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var generatorNames = generators.Select(GetGeneratorName).ToList();
            var dependencies = generators.Where(gen => gen.Dependencies != null)
                .SelectMany(gen => gen.Dependencies.Select(dependsOn => Tuple.Create(dependsOn, GetGeneratorName(gen))))
                .ToList();
            Graph.TopologicalSort(generatorNames, dependencies);

            foreach (var missingDependency in dependencies.Where(dep => !generatorNames.Contains(dep.Item1)))
                _deployPackagesLogger.Info($"Missing dependency '{missingDependency.Item1}' for application generator '{missingDependency.Item2}'.");

            Graph.SortByGivenOrder(generators, generatorNames, GetGeneratorName);
            return generators;
        }

        private static string GetGeneratorName(IGenerator gen)
        {
            return gen.GetType().FullName;
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
