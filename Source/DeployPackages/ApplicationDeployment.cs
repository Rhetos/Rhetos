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
    public class ApplicationDeployment
    {
        private readonly ILogger logger;
        private readonly DeployArguments deployArguments;

        public ApplicationDeployment(ILogger logger, DeployArguments deployArguments)
        {
            this.logger = logger;
            this.deployArguments = deployArguments;
        }
        
        public void GenerateApplication()
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

        public void InitializeGeneratedApplication()
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
