using Autofac;
using Rhetos;
using Rhetos.Configuration.Autofac;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployPackages
{
    public class DeployManager
    {
        private readonly ILogger logger;
        private readonly DeployArguments deployArguments;

        public DeployManager(ILogger logger, DeployArguments deployArguments)
        {
            this.logger = logger;
            this.deployArguments = deployArguments;
        }
        
        public void DeployApplication()
        {
            InitialCleanup();
            DownloadPackages();
            GenerateApplication();
            InitializeGeneratedApplication();
        }

        private void InitialCleanup()
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

            if (!deployArguments.DeployDatabaseOnly)
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

        private void DownloadPackages()
        {
            if (!deployArguments.DeployDatabaseOnly)
            {
                logger.Trace("Getting packages.");
                var config = new DeploymentConfiguration(DeploymentUtility.InitializationLogProvider);
                var packageDownloaderOptions = new PackageDownloaderOptions { IgnorePackageDependencies = deployArguments.IgnorePackageDependencies };
                var packageDownloader = new PackageDownloader(config, DeploymentUtility.InitializationLogProvider, packageDownloaderOptions);
                var packages = packageDownloader.GetPackages();

                InstalledPackages.Save(packages);
            }
            else
                logger.Info("Skipped download packages (DeployDatabaseOnly).");
        }

        private void GenerateApplication()
        {
            logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            Plugins.SetInitializationLogging(DeploymentUtility.InitializationLogProvider);
            var deployType = deployArguments.DeployDatabaseOnly ? DeployType.DeployDatabaseOnly : DeployType.DeployFull;
            var builder = new ContainerBuilder()
                .AddRhetosDeployment(deployArguments.ShortTransactions, deployType)
                .AddUserAndLoggingOverrides();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Generating application", container);

                if (deployArguments.Debug)
                    container.Resolve<DomGeneratorOptions>().Debug = true;

                container.Resolve<ApplicationGenerator>().ExecuteGenerators(deployArguments.DeployDatabaseOnly);
            }
        }

        private void InitializeGeneratedApplication()
        {
            // Creating a new container builder instead of using builder.Update, because of severe performance issues with the Update method.
            Plugins.ClearCache();

            logger.Trace("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            Plugins.SetInitializationLogging(DeploymentUtility.InitializationLogProvider);
            var builder = new ContainerBuilder()
                .AddApplicationInitialization(deployArguments)
                .AddRhetosRuntime()
                .AddUserAndLoggingOverrides();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                var initializers = ApplicationInitialization.GetSortedInitializers(container);

                performanceLogger.Write(stopwatch, "DeployPackages.Program: New modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Initializing application", container);

                if (!initializers.Any())
                {
                    logger.Trace("No server initialization plugins.");
                }
                else
                {
                    foreach (var initializer in initializers)
                        ApplicationInitialization.ExecuteInitializer(container, initializer);
                }
            }

            RestartWebServer();
        }

        private void RestartWebServer()
        {
            var configFile = Paths.RhetosServerWebConfigFile;
            if (FilesUtility.SafeTouch(configFile))
                logger.Trace($"Updated {Path.GetFileName(configFile)} modification date to restart server.");
            else
                logger.Trace($"Missing {Path.GetFileName(configFile)}.");
        }

    }
}
