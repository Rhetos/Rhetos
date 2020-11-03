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
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Rhetos.Persistence;

namespace Rhetos
{
    public class ApplicationDeployment
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ILogProvider _logProvider;

        public ApplicationDeployment(IConfiguration configuration, ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _configuration = configuration;
            _logProvider = logProvider;
        }

        //=====================================================================

        public void UpdateDatabase()
        {
            _logger.Info("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = CreateDbUpdateComponentsContainer();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance." + GetType().Name);
                performanceLogger.Write(stopwatch, "Modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Generating application", container, _logProvider);

                container.Resolve<DatabaseDeployment>().UpdateDatabase();
            }
        }

        protected RhetosContainerBuilder CreateDbUpdateComponentsContainer()
        {
            var builder = new RhetosContainerBuilder(_configuration, _logProvider, AssemblyResolver.GetRuntimeAssemblies(_configuration));
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DbUpdateModule());
            builder.AddPluginModules();
            builder.RegisterType<NullUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container should not execute the application's business features.
            return builder;
        }

        //=====================================================================

        public void InitializeGeneratedApplication(IRhetosRuntime rhetosRuntime)
        {
            // Creating a new container builder instead of using builder.Update(), because of severe performance issues with the Update method.

            _logger.Info("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            using (var container = rhetosRuntime.BuildContainer(_logProvider, _configuration, AddAppInitializationComponents))
            {
                // EfMappingViewsInitializer is manually executed before other initializers because of performance issues.
                // It is needed for most of the initializer to run, but lazy initialization of EfMappingViews on first DbContext usage
                // is not a good option because of significant hash check duration (it would not be applicable in run-time).
                _logger.Info("Initializing EfMappingViews.");
                var efMappingViewsInitializer = container.Resolve<EfMappingViewsInitializer>();
                efMappingViewsInitializer.Initialize();

                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance." + GetType().Name);
                var initializers = ApplicationInitialization.GetSortedInitializers(container);

                performanceLogger.Write(stopwatch, "New modules and plugins registered.");
                ContainerBuilderPluginRegistration.LogRegistrationStatistics("Initializing application", container, _logProvider);

                if (!initializers.Any())
                {
                    _logger.Info("No server initialization plugins.");
                }
                else
                {
                    foreach (var initializer in initializers)
                        ApplicationInitialization.ExecuteInitializer(container, initializer);
                }
            }
        }

        protected void AddAppInitializationComponents(ContainerBuilder builder)
        {
            builder.RegisterModule(new AppInitializeModule());
            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container is intended to be used in a simple process.
        }

        //=====================================================================

        /// <summary>
        /// Forces restart of the web application, to avoid using old version of application's DLLs from ASP.NET cache (Shadow Copying Assemblies).
        /// </summary>
        public void RestartWebServer(string configurationFolder)
        {
            var configFile = Path.Combine(configurationFolder, "Web.config");
            if (FilesUtility.SafeTouch(configFile))
                _logger.Info($"Updated {Path.GetFileName(configFile)} modification date to restart server.");
            else
                _logger.Info($"Missing {configFile}.");
        }
    }
}
