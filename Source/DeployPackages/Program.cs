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
using Rhetos;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DeployPackages
{
    public class Program
    {
        public static int Main(string[] args)
        {
            ILogger logger = new ConsoleLogger("DeployPackages"); // Using the simplest logger outside of try-catch block.
            string oldCurrentDirectory = null;
            DeployArguments arguments = null;

            try
            {
                logger = DeploymentUtility.InitializationLogProvider.GetLogger("DeployPackages"); // Setting the final log provider inside the try-catch block, so that the simple ConsoleLogger can be used (see above) in case of an initialization error.

                arguments = new DeployArguments(args);
                if (arguments.Help)
                    return 1;

                if (arguments.StartPaused)
                {
                    if (!Environment.UserInteractive)
                        throw new Rhetos.UserException("DeployPackages parameter 'StartPaused' must not be set, because the application is executed in a non-interactive environment.");

                    // Use for debugging (Attach to Process)
                    Console.WriteLine("Press any key to continue . . .");
                    Console.ReadKey(true);
                }

                Paths.InitializeRhetosServerRootPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));

                oldCurrentDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                InitialCleanup(logger, arguments);
                DownloadPackages(logger, arguments);
                GenerateApplication(logger, arguments);
                InitializeGeneratedApplication(logger, arguments);
                logger.Trace("Done.");
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());

                if (ex is ReflectionTypeLoadException)
                    logger.Error(CsUtility.ReportTypeLoadException((ReflectionTypeLoadException)ex));

                if (Environment.UserInteractive)
                {
                    PrintSummary(ex);
                    if (arguments != null && !arguments.NoPauseOnError)
                    {
                        Console.WriteLine("Press any key to continue . . .  (use /NoPause switch to avoid pause on error)");
                        Console.ReadKey(true);
                    }
                }

                return 1;
            }
            finally
            {
                if (oldCurrentDirectory != null && Directory.Exists(oldCurrentDirectory))
                    Directory.SetCurrentDirectory(oldCurrentDirectory);
            }

            return 0;
        }

        private static void InitialCleanup(ILogger logger, DeployArguments arguments)
        {
            // Warning to backup obsolete folders:

            var obsoleteFolders = new string[]
            {
                Path.Combine(Paths.RhetosServerRootPath, "DslScripts"),
                Path.Combine(Paths.RhetosServerRootPath, "DataMigration")
            };
            var obsoleteFolder = obsoleteFolders.FirstOrDefault(folder => Directory.Exists(folder));
            if (obsoleteFolder != null)
                throw new UserException("Please backup all Rhetos server folders and delete obsolete folder '" + obsoleteFolder + "'. It is no longer used.");

            // Delete obsolete generated files:

            var deleteObsoleteFiles = new string[]
            {
                Path.Combine(Paths.BinFolder, "ServerDom.cs"),
                Path.Combine(Paths.BinFolder, "ServerDom.dll"),
                Path.Combine(Paths.BinFolder, "ServerDom.pdb")
            };
            var filesUtility = new FilesUtility(DeploymentUtility.InitializationLogProvider);
            foreach (var path in deleteObsoleteFiles)
                if (File.Exists(path))
                {
                    logger.Info($"Deleting obsolete file '{path}'.");
                    filesUtility.SafeDeleteFile(path);
                }

            // Backup and delete generated files:

            if (!arguments.DeployDatabaseOnly)
            {
                logger.Trace("Moving old generated files to cache.");
                new GeneratedFilesCache(DeploymentUtility.InitializationLogProvider).MoveGeneratedFilesToCache();
                filesUtility.SafeCreateDirectory(Paths.GeneratedFolder);
            }
            else
            {
                var missingFile = Paths.DomAssemblyFiles.FirstOrDefault(f => !File.Exists(f));
                if (missingFile != null)
                    throw new UserException($"'/DatabaseOnly' switch cannot be used if the server have not been deployed successfully before. Run a regular deployment instead. Missing '{missingFile}'.");

                logger.Info("Skipped deleting old generated files (DeployDatabaseOnly).");
            }
        }

        private static void DownloadPackages(ILogger logger, DeployArguments arguments)
        {
            if (!arguments.DeployDatabaseOnly)
            {
                logger.Trace("Getting packages.");
                var config = new DeploymentConfiguration(DeploymentUtility.InitializationLogProvider);
                var packageDownloaderOptions = new PackageDownloaderOptions { IgnorePackageDependencies = arguments.IgnorePackageDependencies };
                var packageDownloader = new PackageDownloader(config, DeploymentUtility.InitializationLogProvider, packageDownloaderOptions);
                var packages = packageDownloader.GetPackages();

                InstalledPackages.Save(packages);
            }
            else
                logger.Info("Skipped download packages (DeployDatabaseOnly).");
        }

        private static void GenerateApplication(ILogger logger, DeployArguments arguments)
        {
            logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new AutofacModuleConfiguration(
                deploymentTime: true,
                configurationArguments: arguments));

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Generating application", container);

                if (arguments.Debug)
                    container.Resolve<DomGeneratorOptions>().Debug = true;

                container.Resolve<ApplicationGenerator>().ExecuteGenerators(arguments.DeployDatabaseOnly);
            }
        }

        private static void InitializeGeneratedApplication(ILogger logger, DeployArguments arguments)
        {
            // Creating a new container builder instead of using builder.Update, because of severe performance issues with the Update method.
            Plugins.ClearCache();

            logger.Trace("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new AutofacModuleConfiguration(
                deploymentTime: false,
                configurationArguments: arguments));
            
            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                var initializers = ApplicationInitialization.GetSortedInitializers(container);

                performanceLogger.Write(stopwatch, "DeployPackages.Program: New modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Initializing application", container);

                if (!initializers.Any())
                {
                    logger.Trace("No server initialization plugins.");
                } else
                {
                    foreach (var initializer in initializers)
                        ApplicationInitialization.ExecuteInitializer(container, initializer);
                }
            }

            RestartWebServer(logger);
        }

        private static void RestartWebServer(ILogger logger)
        {
            var configFile = Paths.RhetosServerWebConfigFile;
            if (FilesUtility.SafeTouch(configFile))
                logger.Trace($"Updated {Path.GetFileName(configFile)} modification date to restart server.");
            else
                logger.Trace($"Missing {Path.GetFileName(configFile)}.");
        }

        private static void PrintSummary(Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("=== ERROR SUMMARY ===");
            DeploymentUtility.WriteError(ex.GetType().Name + ": " + ExceptionsUtility.SafeFormatUserMessage(ex));
            Console.WriteLine();
            Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in DeployPackages.exe.config for even more details.");
        }
    }
}
