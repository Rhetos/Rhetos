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
using System.IO;
using System.Linq;

namespace Rhetos
{
    public class ApplicationBuild
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ILogProvider _logProvider;
        private readonly IEnumerable<string> _pluginAssemblies;
        private readonly InstalledPackages _installedPackages;

        /// <param name="pluginAssemblies">List of assemblies (DLL file paths) that will be scanned for plugins.</param>
        public ApplicationBuild(IConfiguration configuration, ILogProvider logProvider, IEnumerable<string> pluginAssemblies, InstalledPackages installedPackages)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _configuration = configuration;
            _logProvider = logProvider;
            _pluginAssemblies = pluginAssemblies;
            _installedPackages = installedPackages;
        }

        /// <summary>
        /// Rhetos CLI does not support legacy Rhetos packages with libraries locates in Plugins subfolder.
        /// </summary>
        public void ReportLegacyPluginsFolders()
        {
            var legacyLibraries = _installedPackages.Packages.SelectMany(
                    package => package.ContentFiles
                        .Where(file => file.InPackagePath.StartsWith("Plugins" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        && file.InPackagePath.EndsWith(".dll", StringComparison.Ordinal))
                        .Select(file => (Package: package, File: file)));

            if (legacyLibraries.Any())
                _logger.Warning("Rhetos NuGet packages with DLLs in \"Plugins\" folder are not supported in this environment." +
                    " To update the packages, in their .nuspec files replace target=\"Plugins\" with target=\"lib\"," +
                    " to match the standard NuGet convention. Packages: " +
                    string.Join(", ", legacyLibraries.Select(library => library.Package.Id).Distinct()) + ".");
        }

        public void GenerateApplication()
        {
            _logger.Info("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = CreateBuildComponentsContainer();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance." + GetType().Name);
                performanceLogger.Write(stopwatch, "Modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Rhetos build component registrations", container, _logProvider);

                container.Resolve<ApplicationGenerator>().ExecuteGenerators();
            }
        }

        private ContainerBuilder CreateBuildComponentsContainer()
        {
            var pluginScanner = new PluginScanner(
                _pluginAssemblies,
                _configuration.GetOptions<RhetosBuildEnvironment>(),
                _logProvider,
                _configuration.GetOptions<PluginScannerOptions>());

            var builder = RhetosContainerBuilder.Create(_configuration, _logProvider, pluginScanner);
            builder.Properties.Add(nameof(ExecutionStage), new ExecutionStage { IsBuildTime = true });
            builder.Register(context => new PluginInfoCollection(pluginScanner.FindAllPlugins()));
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new CorePluginsModule());
            builder.RegisterModule(new BuildModule());
            builder.AddRhetosPluginModules();
            builder.RegisterType<NullUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container should not execute the application's business features.
            builder.RegisterInstance(_installedPackages);
            return builder;
        }
    }
}
