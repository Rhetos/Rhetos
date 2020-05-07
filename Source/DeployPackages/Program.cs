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

using Rhetos;
using Rhetos.Deployment;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.IO;
using System.Linq;

namespace DeployPackages
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var logProvider = new NLogProvider();
            var logger = logProvider.GetLogger("DeployPackages");
            var pauseOnError = false;

            logger.Info(() => "Logging configured.");

            try
            {
                if (!DeployPackagesArguments.ValidateArguments(args))
                    return 1;

                string rhetosAppRootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));

                Build(args, logProvider, rhetosAppRootPath, out pauseOnError);
                DbUpdate(args, logProvider);

                logger.Info("Done.");
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());

                string typeLoadReport = CsUtility.ReportTypeLoadException(e);
                if (typeLoadReport != null)
                    logger.Error(typeLoadReport);

                if (Environment.UserInteractive)
                    InteractiveExceptionInfo(e, pauseOnError);

                return 1;
            }

            return 0;
        }

        private static void Build(string[] args, NLogProvider logProvider, string rhetosAppRootPath, out bool pauseOnError)
        {
            var logger = logProvider.GetLogger("DeployPackages");

            var configurationBuilder = new ConfigurationBuilder()
                .AddOptions(new RhetosBuildEnvironment
                {
                    ProjectFolder = rhetosAppRootPath,
                    OutputAssemblyName = null,
                    CacheFolder = Path.Combine(rhetosAppRootPath, "GeneratedFilesCache"),
                    GeneratedAssetsFolder = Path.Combine(rhetosAppRootPath, "bin", "Generated"),
                    GeneratedSourceFolder = null,
                })
                .AddOptions(new LegacyPathsOptions
                {
                    BinFolder = Path.Combine(rhetosAppRootPath, "bin"),
                    PluginsFolder = Path.Combine(rhetosAppRootPath, "bin", "Plugins"),
                    ResourcesFolder = Path.Combine(rhetosAppRootPath, "Resources"),
                })
                .AddKeyValue($"{OptionsAttribute.GetConfigurationPath<BuildOptions>()}:{nameof(BuildOptions.GenerateAppSettings)}", false)
                .AddKeyValue($"{OptionsAttribute.GetConfigurationPath<BuildOptions>()}:{nameof(BuildOptions.BuildResourcesFolder)}", true)
                .AddWebConfiguration(rhetosAppRootPath)
                .AddConfigurationManagerConfiguration()
                .AddCommandLineArgumentsWithConfigurationPaths(args);

            var configuration = configurationBuilder.Build();

            var deployPackagesOptions = configuration.GetOptions<DeployPackagesOptions>();

            pauseOnError = !deployPackagesOptions.NoPause;
            if (deployPackagesOptions.StartPaused)
                StartPaused();

            if (!deployPackagesOptions.DatabaseOnly)
            {
                LegacyUtilities.Initialize(configuration);
                DeleteObsoleteFiles(rhetosAppRootPath, logProvider, logger);

                var installedPackages = DownloadPackages(deployPackagesOptions.IgnoreDependencies, logProvider, logger);

                var pluginAssemblies = Directory.GetFiles(Path.Combine(rhetosAppRootPath, "bin", "Plugins"), "*.dll", SearchOption.TopDirectoryOnly);
                var build = new ApplicationBuild(configuration, logProvider, pluginAssemblies, installedPackages);
                build.GenerateApplication();
            }
            else
            {
                logger.Info("Skipped deleting old generated files (DeployDatabaseOnly).");
                logger.Info("Skipped download packages (DeployDatabaseOnly).");
                logger.Info("Skipped code generators (DeployDatabaseOnly).");
            }
        }

        /// <summary>
        /// This feature is intended to simplify attaching debugger to the process that was run from a build script.
        /// </summary>
        private static void StartPaused()
        {
            if (!Environment.UserInteractive)
                throw new UserException("DeployPackages parameter 'StartPaused' must not be set, because the application is executed in a non-interactive environment.");

            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }

        public static InstalledPackages DownloadPackages(bool ignoreDependencies, ILogProvider logProvider, ILogger logger)
        {
            logger.Info("Getting packages.");
            var config = new DeploymentConfiguration(logProvider);
            var packageDownloaderOptions = new PackageDownloaderOptions { IgnorePackageDependencies = ignoreDependencies };
            var packageDownloader = new PackageDownloader(config, logProvider, packageDownloaderOptions);
            var installedPackages = packageDownloader.GetPackages();
            return installedPackages;
        }

        /// <summary>
        /// Deletes left-over files from old versions of Rhetos framework.
        /// Throws an exception if important data might be lost.
        /// </summary>
        private static void DeleteObsoleteFiles(string rhetosAppRootPath, ILogProvider logProvider, ILogger logger)
        {
            var filesUtility = new FilesUtility(logProvider);

            var obsoleteFolders = new string[]
            {
                Path.Combine(rhetosAppRootPath, "DslScripts"),
                Path.Combine(rhetosAppRootPath, "DataMigration"),
            };
            var obsoleteFolder = obsoleteFolders.FirstOrDefault(folder => Directory.Exists(folder));
            if (obsoleteFolder != null)
                throw new UserException($"Please backup all Rhetos server folders and delete obsolete folder '{obsoleteFolder}'. It is no longer used.");

            var deleteObsoleteFiles = new string[]
            {
                Path.Combine(rhetosAppRootPath, "bin", "ServerDom.cs"),
                Path.Combine(rhetosAppRootPath, "bin", "ServerDom.dll"),
                Path.Combine(rhetosAppRootPath, "bin", "ServerDom.pdb"),
            };

            foreach (var path in deleteObsoleteFiles)
                if (File.Exists(path))
                {
                    logger.Warning($"Deleting obsolete file '{path}'.");
                    filesUtility.SafeDeleteFile(path);
                }
        }

        private static void DbUpdate(string[] args, NLogProvider logProvider)
        {
            var host = Host.Find(AppDomain.CurrentDomain.BaseDirectory, logProvider);
            var configuration = host.RhetosRuntime
                .BuildConfiguration(logProvider, host.ConfigurationFolder, configurationBuilder => configurationBuilder
                    .AddConfigurationManagerConfiguration()
                    .AddCommandLineArgumentsWithConfigurationPaths(args));

            var deployment = new ApplicationDeployment(configuration, logProvider);
            deployment.UpdateDatabase();
            deployment.InitializeGeneratedApplication(host.RhetosRuntime);
            deployment.RestartWebServer(host.ConfigurationFolder);
        }

        private static void InteractiveExceptionInfo(Exception e, bool pauseOnError)
        {
            DeploymentUtility.PrintErrorSummary(e);

            if (pauseOnError)
            {
                Console.WriteLine("Press any key to continue . . .  (use /NoPause switch to avoid pause on error)");
                Console.ReadKey(true);
            }
        }
    }
}
