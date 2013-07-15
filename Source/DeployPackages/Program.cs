/*
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
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Linq;
using Autofac;
using Rhetos;
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Factory;
using Rhetos.Logging;
using Rhetos.Persistence.NHibernate;
using Rhetos.RestGenerator;
using Rhetos.Security;
using Rhetos.Dsl;
using System.Collections.Generic;

namespace DeployPackages
{
    class Paremeters
    {
        public readonly bool CleanPreviousDeploymentData;
        public readonly bool GeneratePermissionClaims;

        public Paremeters(string[] args)
        {
            var readArgs = args.ToList();

            CleanPreviousDeploymentData = ReadSwitch(readArgs, "clean");
            GeneratePermissionClaims = !ReadSwitch(readArgs, "skipclaims"); // TODO: Remove this switch when security claims from Common package are separated from Framework. Currently the server cannot be deployed without common package because of the dependency.

            if (readArgs.Count() != 0)
                throw new ApplicationException(@"DeployPackages.exe command-line arguments:
/clean        Old data-migration tables will be deleted before the upgrade. Do not use this option when recovering from previous unwanted or failed deployment.
/skipclaims  Skips generating permission claims. Will be removed in future versions.");
        }

        private static bool ReadSwitch(List<string> readArgs, string switchName)
        {
            if (readArgs.Any(arg => arg.Equals("/" + switchName, StringComparison.OrdinalIgnoreCase)))
            {
                readArgs.RemoveAll(arg => arg.Equals("/" + switchName, StringComparison.OrdinalIgnoreCase));
                return true;
            }
            return false;
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            ILogger logger = null;
            Action undoDataMigrationScriptsOnError = null;

            try
            {
                var parameters = new Paremeters(args);

                var builder = new ContainerBuilder();
                string connectionString = SqlUtility.ConnectionString;
                builder.RegisterModule(new AutofacConfiguration(connectionString));
                var container = builder.Build();
                logger = container.Resolve<ILogProvider>().GetLogger("DeployPackages");

                Console.WriteLine("SQL connection string: " + SqlUtility.MaskPassword(connectionString));

                Console.Write("Parsing DSL scripts ... ");
                Console.WriteLine(container.Resolve<IDslModel>().Concepts.Count() + " features.");

                Console.Write("Compiling DOM assembly ... ");
                int generatedTypesCount = container.Resolve<IDomGenerator>().ObjectModel.GetTypes().Length;
                if (generatedTypesCount == 0)
                {
                    string report = "WARNING: Empty assembly is generated.";
                    DeploymentUtility.WriteError(report);
                    logger.Error(report);
                }
                else
                    Console.WriteLine("Generated " + generatedTypesCount + " types.");

                Console.Write("Generating Domain Service ... ");
                container.Resolve<IRestGenerator>().Generate("DomainService");
                Console.WriteLine("Done.");

                Console.Write("Executing custom generators... ");
                container.Resolve<Rhetos.Generator.GeneratorProcessor>().ProcessGenerators();
                Console.WriteLine("Done.");

                Console.Write("Preparing Rhetos database ... ");
                DeploymentUtility.PrepareRhetosDatabase(container.Resolve<ISqlExecuter>());
                Console.WriteLine("Done.");

                if (parameters.CleanPreviousDeploymentData)
                {
                    Console.Write("Clean option is on, deleting old migration data ... ");
                    var report = container.Resolve<DatabaseCleaner>().CleanupOldData();
                    Console.WriteLine(report);
                }

                Console.Write("Executing data migration scripts ... ");
                var dataMigration = container.Resolve<DataMigration>();
                var dataMigrationReport = dataMigration.ExecuteDataMigrationScripts(AutofacConfiguration.DataMigrationScriptsFolder);
                Console.WriteLine(dataMigrationReport);
                undoDataMigrationScriptsOnError = delegate { dataMigration.UndoDataMigrationScripts(dataMigrationReport.CreatedTags); };

                Console.Write("Upgrading database ... ");
                var updateDatabaseReport = container.Resolve<IDatabaseGenerator>().UpdateDatabaseStructure();
                Console.WriteLine(updateDatabaseReport);
                undoDataMigrationScriptsOnError = null;

                Console.Write("Deleting redundant migration data ... ");
                var databaseCleanerReport = container.Resolve<DatabaseCleaner>().CleanupRedundantOldData();
                Console.WriteLine(databaseCleanerReport);

                Console.Write("Uploading DSL scripts ... ");
                int dslScriptCount = DslScriptManager.UploadDslScriptsToServer(AutofacConfiguration.DslScriptsFolder, container.Resolve<ISqlExecuter>());
                if (dslScriptCount == 0)
                {
                    string report = "WARNING: There are no DSL scripts in source folder " + AutofacConfiguration.DslScriptsFolder + ".";
                    DeploymentUtility.WriteError(report);
                    logger.Error(report);
                }
                else
                    Console.WriteLine("Uploaded " + dslScriptCount + " DSL scripts to database.");

                Console.Write("Generating NHibernate mapping ... ");
                File.WriteAllText(AutofacConfiguration.NHibernateMappingFile, container.Resolve<INHibernateMapping>().GetMapping(), Encoding.Unicode);
                Console.WriteLine("Done.");

                if (parameters.GeneratePermissionClaims)
                {
                    Console.Write("Generating claims ... ");
                    container.Resolve<IClaimGenerator>().GenerateClaims();
                    Console.WriteLine("Done.");
                }
                else
                    Console.WriteLine("Generating claims skipped. ");

                var configFile = new FileInfo(AutofacConfiguration.RhetosServerWebConfigPath);
                if (configFile.Exists)
                {
                    DeploymentUtility.Touch(configFile);
                    Console.WriteLine("Updated Web.config modification date to restart server.");
                }
                else
                    Console.WriteLine("Web.config update skipped.");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex);
                DeploymentUtility.WriteError(ex.GetType().Name + ": " + ex.Message);
                Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in config file for even more details.");

                if (logger != null)
                    logger.Error(ex.ToString());

                if (undoDataMigrationScriptsOnError != null)
                    undoDataMigrationScriptsOnError();

                Thread.Sleep(3000);
                return 1;
            }
        }
    }
}
