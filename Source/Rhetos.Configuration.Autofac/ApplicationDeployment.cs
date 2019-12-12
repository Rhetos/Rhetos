﻿/*
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
using Rhetos.Deployment;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
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
        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly BuildOptions _buildOptions;
        private readonly FilesUtility _filesUtility;
        private readonly DeployOptions _deployOptions;
        private readonly string _rhetosRootPath;

        public ApplicationDeployment(IConfigurationProvider configurationProvider, ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("DeployPackages");
            _configurationProvider = configurationProvider;
            _logProvider = logProvider;
            _filesUtility = new FilesUtility(logProvider);
            _buildOptions = _configurationProvider.GetOptions<BuildOptions>();
            _rhetosAppOptions = configurationProvider.GetOptions<RhetosAppOptions>();
            _deployOptions = configurationProvider.GetOptions<DeployOptions>();
            _rhetosRootPath = configurationProvider.GetValue<string>("RootPath");
            LegacyUtilities.Initialize(configurationProvider);
        }

        /// <summary>
        /// Backup and delete generated files.
        /// </summary>
        public void InitialCleanup()
        {
            DeleteObsoleteFiles();
            _logger.Trace("Moving old generated files to cache.");
            new GeneratedFilesCache(_logProvider).MoveGeneratedFilesToCache();
            _filesUtility.SafeCreateDirectory(Paths.GeneratedFolder);
        }

        public void DownloadPackages(bool ignoreDependencies)
        {
            _logger.Trace("Getting packages.");
            var config = new DeploymentConfiguration(Paths.RhetosServerRootPath, _logProvider);
            var packageDownloaderOptions = new PackageDownloaderOptions {
                IgnorePackageDependencies = ignoreDependencies,
                RootFolder = Paths.RhetosServerRootPath,
                ResourcesFolder = Paths.ResourcesFolder,
                PackagesCacheFolder = Paths.PackagesCacheFolder,
                PluginsFolder = Paths.PluginsFolder,
            };
            var packageDownloader = new PackageDownloader(config, _logProvider, packageDownloaderOptions);
            var packages = packageDownloader.GetPackages();

            InstalledPackages.Save(packages);
        }

        public void GenerateApplication()
        {
            _logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider)
                .AddRhetosDeployment()
                .AddProcessUserOverride();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Generating application", container, _logProvider);

                container.Resolve<ApplicationGenerator>().ExecuteGenerators();
            }
        }

        public void UpdateDatabase()
        {
            // TODO: Remove this after refactoring UpdateDatabase to use generated database model file.
            var missingFile = Paths.DomAssemblyFiles.FirstOrDefault(f => !File.Exists(f));
            if (missingFile != null)
                throw new UserException($"'/DatabaseOnly' switch cannot be used if the server have not been deployed successfully before. Run a regular deployment instead. Missing '{missingFile}'.");

            _logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider)
                .AddRhetosDeployment()
                .AddProcessUserOverride();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Generating application", container, _logProvider);

                container.Resolve<ApplicationGenerator>().UpdateDatabase();
            }
        }

        public void InitializeGeneratedApplication()
        {
            // Creating a new container builder instead of using builder.Update(), because of severe performance issues with the Update method.

            _logger.Trace("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider)
                .AddApplicationInitialization()
                .AddRhetosRuntime()
                .AddProcessUserOverride();

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

        /// <summary>
        /// Deletes left-over files from old versions of Rhetos framework.
        /// Throws an exception if important data might be lost.
        /// </summary>
        private void DeleteObsoleteFiles()
        {
            var obsoleteFolders = new string[]
            {
                Path.Combine(_rhetosRootPath, "DslScripts"),
                Path.Combine(_rhetosRootPath, "DataMigration")
            };
            var obsoleteFolder = obsoleteFolders.FirstOrDefault(folder => Directory.Exists(folder));
            if (obsoleteFolder != null)
                throw new UserException("Please backup all Rhetos server folders and delete obsolete folder '" + obsoleteFolder + "'. It is no longer used.");

            var deleteObsoleteFiles = new string[]
            {
                Path.Combine(_rhetosAppOptions.BinFolder, "ServerDom.cs"),
                Path.Combine(_rhetosAppOptions.BinFolder, "ServerDom.dll"),
                Path.Combine(_rhetosAppOptions.BinFolder, "ServerDom.pdb")
            };

            foreach (var path in deleteObsoleteFiles)
                if (File.Exists(path))
                {
                    _logger.Info($"Deleting obsolete file '{path}'.");
                    _filesUtility.SafeDeleteFile(path);
                }
        }

        public static void PrintErrorSummary(Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("=============== ERROR SUMMARY ===============");
            Console.WriteLine(ex.GetType().Name + ": " + ExceptionsUtility.SafeFormatUserMessage(ex));
            Console.WriteLine("=============================================");
            Console.WriteLine();
            Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in DeployPackages.exe.config for even more details.");
        }

        public void RestartWebServer()
        {
            var configFile = Path.Combine(_rhetosRootPath, "Web.config");
            if (FilesUtility.SafeTouch(configFile))
                _logger.Trace($"Updated {Path.GetFileName(configFile)} modification date to restart server.");
            else
                _logger.Trace($"Missing {configFile}.");
        }
    }
}
