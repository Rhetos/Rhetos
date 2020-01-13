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
using Autofac.Core;
using Rhetos.Configuration.Autofac.Modules;
using Rhetos.Deployment;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rhetos
{
    public class ApplicationDeployment
    {
        private readonly ILogger _logger;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILogProvider _logProvider;
        private readonly Func<IEnumerable<string>> _findAssemblies;
        private readonly FilesUtility _filesUtility;

        public ApplicationDeployment(IConfigurationProvider configurationProvider, ILogProvider logProvider, Func<IEnumerable<string>> findAssemblies)
        {
            _logger = logProvider.GetLogger("DeployPackages");
            _configurationProvider = configurationProvider;
            _logProvider = logProvider;
            _findAssemblies = findAssemblies;
            _filesUtility = new FilesUtility(logProvider);
            LegacyUtilities.Initialize(configurationProvider);
        }

        public InstalledPackages DownloadPackages(bool ignoreDependencies)
        {
            _logger.Trace("Getting packages.");
            var config = new DeploymentConfiguration(_logProvider);
            var packageDownloaderOptions = new PackageDownloaderOptions { IgnorePackageDependencies = ignoreDependencies };
            var packageDownloader = new PackageDownloader(config, _logProvider, packageDownloaderOptions);
            var installedPackages = packageDownloader.GetPackages();
            return installedPackages;
        }

        //=====================================================================

        public void GenerateApplication(InstalledPackages installedPackages)
        {
            _filesUtility.EmptyDirectory(_configurationProvider.GetOptions<AssetsOptions>().AssetsFolder);
            _filesUtility.EmptyDirectory(_configurationProvider.GetOptions<BuildOptions>().GeneratedSourceFolder);
            _filesUtility.SafeCreateDirectory(_configurationProvider.GetOptions<BuildOptions>().CacheFolder); // Cache should not be deleted between builds.

            _logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = CreateBuildComponentsContainer(installedPackages);

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Generating application", container, _logProvider);

                container.Resolve<ApplicationGenerator>().ExecuteGenerators();
            }
        }

        internal RhetosContainerBuilder CreateBuildComponentsContainer(InstalledPackages installedPackages)
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider, _findAssemblies);
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new CorePluginsModule());
            builder.RegisterModule(new BuildModule());
            builder.GetPluginRegistration().FindAndRegisterPluginModules();
            builder.RegisterType<NullUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container should not execute the application's business features.
            builder.RegisterInstance(installedPackages).As<IInstalledPackages>().As<InstalledPackages>();
            return builder;
        }

        //=====================================================================

        public void UpdateDatabase()
        {
            _logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = CreateDbUpdateComponentsContainer();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Generating application", container, _logProvider);

                container.Resolve<DatabaseDeployment>().UpdateDatabase();
            }
        }

        internal RhetosContainerBuilder CreateDbUpdateComponentsContainer()
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider, _findAssemblies);
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DbUpdateModule());
            builder.GetPluginRegistration().FindAndRegisterPluginModules();
            builder.RegisterType<NullUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container should not execute the application's business features.
            return builder;
        }

        //=====================================================================

        public void InitializeGeneratedApplication()
        {
            // Creating a new container builder instead of using builder.Update(), because of severe performance issues with the Update method.

            _logger.Trace("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = CreateAppInitializationComponentsContainer();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                var initializers = ApplicationInitialization.GetSortedInitializers(container);

                performanceLogger.Write(stopwatch, "DeployPackages.Program: New modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Initializing application", container, _logProvider);

                if (!initializers.Any())
                {
                    _logger.Trace("No server initialization plugins.");
                }
                else
                {
                    foreach (var initializer in initializers)
                        ApplicationInitialization.ExecuteInitializer(container, initializer);
                }
            }
        }

        internal RhetosContainerBuilder CreateAppInitializationComponentsContainer()
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider, _findAssemblies);
            builder.AddRhetosRuntime();
            builder.RegisterModule(new AppInitializeModule());
            builder.GetPluginRegistration().FindAndRegisterPluginModules();
            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container is intended to be used in a simple process.
            return builder;
        }

        //=====================================================================

        public static void PrintErrorSummary(Exception ex)
        {
            while (ex is DependencyResolutionException && ex.InnerException != null)
                ex = ex.InnerException;

            Console.WriteLine();
            Console.WriteLine("=============== ERROR SUMMARY ===============");
            Console.WriteLine(ex.GetType().Name + ": " + ExceptionsUtility.SafeFormatUserMessage(ex));
            Console.WriteLine("=============================================");
            Console.WriteLine();
            Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in DeployPackages.exe.config for even more details.");
        }

        public void RestartWebServer()
        {
            var configFile = Path.Combine(Paths.RhetosServerRootPath, "Web.config");
            if (FilesUtility.SafeTouch(configFile))
                _logger.Trace($"Updated {Path.GetFileName(configFile)} modification date to restart server.");
            else
                _logger.Trace($"Missing {configFile}.");
        }
    }
}
