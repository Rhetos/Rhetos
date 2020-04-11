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
using Rhetos.Configuration.Autofac.Modules;
using Rhetos.Deployment;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rhetos
{
    public class ApplicationBuild
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configurationProvider;
        private readonly ILogProvider _logProvider;
        private readonly Func<IEnumerable<string>> _pluginAssemblies;

        /// <param name="pluginAssemblies">List of assemblies (DLL file paths) that will be scanned for plugins.</param>
        public ApplicationBuild(IConfiguration configurationProvider, ILogProvider logProvider, Func<IEnumerable<string>> pluginAssemblies)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _configurationProvider = configurationProvider;
            _logProvider = logProvider;
            _pluginAssemblies = pluginAssemblies;
        }

        public InstalledPackages DownloadPackages(bool ignoreDependencies)
        {
            _logger.Info("Getting packages.");
            var config = new DeploymentConfiguration(_logProvider);
            var packageDownloaderOptions = new PackageDownloaderOptions { IgnorePackageDependencies = ignoreDependencies };
            var packageDownloader = new PackageDownloader(config, _logProvider, packageDownloaderOptions);
            var installedPackages = packageDownloader.GetPackages();
            return installedPackages;
        }

        /// <summary>
        /// Rhetos CLI does not support legacy Rhetos packages with libraries locates in Plugins subfolder.
        /// </summary>
        public void ReportLegacyPluginsFolders(InstalledPackages installedPackages)
        {
            var legacyLibraries = installedPackages.Packages.SelectMany(
                    package => package.ContentFiles
                        .Where(file => file.InPackagePath.StartsWith(@"Plugins\") && file.InPackagePath.EndsWith(".dll"))
                        .Select(file => (Package: package, File: file)));

            if (legacyLibraries.Any())
                _logger.Warning("Rhetos NuGet packages with DLLs in \"Plugins\" folder are not supported in this environment." +
                    " To update the packages, in their .nuspec files replace target=\"Plugins\" with target=\"lib\"," +
                    " to match the standard NuGet convention. Packages: " +
                    string.Join(", ", legacyLibraries.Select(library => library.Package.Id).Distinct()) + ".");
        }

        public void GenerateApplication(InstalledPackages installedPackages)
        {
            _logger.Info("Loading plugins.");
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
            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider, _pluginAssemblies);
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new CorePluginsModule());
            builder.RegisterModule(new BuildModule());
            builder.AddPluginModules();
            builder.RegisterType<NullUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container should not execute the application's business features.
            builder.RegisterInstance(installedPackages).As<IInstalledPackages>().As<InstalledPackages>();
            return builder;
        }
    }
}
