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

using Autofac;
using Rhetos.DatabaseGenerator;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence.NHibernate;
using Rhetos.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DeployPackages
{
    class Program
    {
        static ILogger _logger = new NLogger("DeployPackagesInitialization");
        static ILogger _performanceLogger;
        static Action undoDataMigrationScriptsOnError = null;
        static string oldCurrentDirectory = null;

        static int Main(string[] args)
        {
            try
            {
                if (args.Contains("/StartPaused"))
                {
                    // Use for debugging (Attach to Process)
                    Console.WriteLine("Press any key to continue . . .");
                    Console.ReadKey(true);
                }

                Paths.InitializeRhetosServerRootPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));

                oldCurrentDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                
                if (!Directory.Exists(Paths.GeneratedFolder))
                    Directory.CreateDirectory(Paths.GeneratedFolder);
                foreach (var oldGeneratedFile in Directory.GetFiles(Paths.GeneratedFolder, "*", SearchOption.AllDirectories))
                    File.Delete(oldGeneratedFile);
                if (File.Exists(Paths.DomAssemblyFile))
                    File.Delete(Paths.DomAssemblyFile);

                var builder = new ContainerBuilder();
                builder.RegisterModule(new AutofacConfiguration());
                using (var container = builder.Build())
                    DeployPackages(container);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                _logger.Error(ex.ToString());

                DeploymentUtility.WriteError(ex.GetType().Name + ": " + ex.Message);
                Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in config file for even more details.");

                if (ex is ReflectionTypeLoadException)
                {
                    var loaderMessages = string.Join("\r\n", ((ReflectionTypeLoadException)ex).LoaderExceptions.Select(le => le.Message).Distinct());
                    _logger.Error(loaderMessages);
                }

                if (undoDataMigrationScriptsOnError != null)
                    undoDataMigrationScriptsOnError();

                Thread.Sleep(3000);
                return 1;
            }
            finally
            {
                if (oldCurrentDirectory != null && Directory.Exists(oldCurrentDirectory))
                    Directory.SetCurrentDirectory(oldCurrentDirectory);
            }

            return 0;
        }

        private static void DeployPackages(IContainer container)
        {
            _logger = new ConsoleLogger("DeployPackages", container.Resolve<ILogProvider>().GetLogger("DeployPackages"));
            _performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");

            Console.WriteLine("SQL connection string: " + SqlUtility.MaskPassword(SqlUtility.ConnectionString));

            Console.Write("Parsing DSL scripts ... ");
            Console.WriteLine(container.Resolve<IDslModel>().Concepts.Count() + " statements.");

            Console.Write("Compiling DOM assembly ... ");
            int generatedTypesCount = container.Resolve<IDomGenerator>().Assembly.GetTypes().Length;
            if (generatedTypesCount == 0)
            {
                string report = "WARNING: Empty assembly is generated.";
                DeploymentUtility.WriteError(report);
                _logger.Error(report);
            }
            else
                Console.WriteLine("Generated " + generatedTypesCount + " types.");

            var generators = container.Resolve<GeneratorPlugins>().GetGenerators();
            foreach (var generator in generators)
            {
                Console.Write("Executing " + generator.GetType().Name + " ... ");
                generator.Generate();
                Console.WriteLine("Done.");
            }
            if (!generators.Any())
                Console.WriteLine("No additional generators.");

            Console.Write("Preparing Rhetos database ... ");
            DeploymentUtility.PrepareRhetosDatabase(container.Resolve<ISqlExecuter>());
            Console.WriteLine("Done.");

            Console.Write("Cleaning old migration data ... ");
            var databaseCleaner = container.Resolve<DatabaseCleaner>();
            {
                string report = databaseCleaner.RemoveRedundantMigrationColumns();
                databaseCleaner.RefreshDataMigrationRows();
                Console.WriteLine(report);
            }

            Console.Write("Executing data migration scripts ... ");
            var dataMigration = container.Resolve<DataMigration>();
            var dataMigrationReport = dataMigration.ExecuteDataMigrationScripts(Paths.DataMigrationScriptsFolder);
            Console.WriteLine(dataMigrationReport);
            undoDataMigrationScriptsOnError = delegate { dataMigration.UndoDataMigrationScripts(dataMigrationReport.CreatedTags); };

            Console.Write("Upgrading database ... ");
            var updateDatabaseReport = container.Resolve<IDatabaseGenerator>().UpdateDatabaseStructure();
            Console.WriteLine(updateDatabaseReport);
            undoDataMigrationScriptsOnError = null;

            Console.Write("Deleting redundant migration data ... ");
            {
                var report = databaseCleaner.RemoveRedundantMigrationColumns();
                databaseCleaner.RefreshDataMigrationRows();
                Console.WriteLine(report);
            }

            Console.Write("Uploading DSL scripts ... ");
            int dslScriptCount = DslScriptManager.UploadDslScriptsToServer(Paths.DslScriptsFolder, container.Resolve<ISqlExecuter>());
            if (dslScriptCount == 0)
            {
                string report = "WARNING: There are no DSL scripts in source folder " + Paths.DslScriptsFolder + ".";
                _logger.Info(report);
            }
            else
                Console.WriteLine("Uploaded " + dslScriptCount + " DSL scripts to database.");

            Console.Write("Generating NHibernate mapping ... ");
            File.WriteAllText(Paths.NHibernateMappingFile, container.Resolve<INHibernateMapping>().GetMapping(), Encoding.Unicode);
            Console.WriteLine("Done.");

            {
                Console.Write("Loading generated plugins ... ");
                var stopwatch = Stopwatch.StartNew();
                PluginsUtility.DetectAndRegisterNewModulesAndPlugins(container);
                _performanceLogger.Write(stopwatch, "DeployPackages.ServerInitialization: New modules and plugins registered.");
                Console.WriteLine("Done.");

                var initializers = container.Resolve<ServerInitializationPlugins>().GetInitializers();
                foreach (var initializer in initializers)
                {
                    Console.Write("Initialization: " + initializer.GetType().Name + " ... ");
                    initializer.Initialize();
                    Console.WriteLine("Done.");
                }
                if (!initializers.Any())
                    Console.WriteLine("No server initialization plugins.");
            }

            var configFile = new FileInfo(Paths.RhetosServerWebConfigFile);
            if (configFile.Exists)
            {
                DeploymentUtility.Touch(configFile);
                Console.WriteLine("Updated Web.config modification date to restart server.");
            }
            else
                Console.WriteLine("Web.config update skipped.");
        }
    }
}
