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
using Rhetos.Logging;
using Rhetos.Persistence;
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
        private readonly Func<Action<IRhetosHostBuilder>, RhetosHost> _rhetosHostFactory;
        private readonly ILogger _logger;
        private readonly ILogProvider _logProvider;

        /// <param name="rhetosHostFactory">
        /// Full <see cref="IRhetosHostBuilder"/> is needed for <see cref="InitializeGeneratedApplication"/>,
        /// while <see cref="UpdateDatabase"/> should work without runtime components registration
        /// (only runtime configuration settings are required, for database connection and similar).
        /// Since both sub-commands are now executed together, this is simplified to a single argument.
        /// </param>
        public ApplicationDeployment(Func<Action<IRhetosHostBuilder>, RhetosHost> rhetosHostFactory, ILogProvider logProvider)
        {
            _rhetosHostFactory = rhetosHostFactory;
            _logProvider = logProvider;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        //=====================================================================

        public void UpdateDatabase()
        {
            _logger.Info("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            Action<IRhetosHostBuilder> configureRhetosHost = builder =>
            {
                builder.UseBuilderLogProvider(_logProvider)
                    .OverrideContainerConfiguration(SetDbUpdateComponents);
            };

            using (var rhetosHost = _rhetosHostFactory(configureRhetosHost))
            using (var scope = rhetosHost.CreateScope())
            {
                var performanceLogger = scope.Resolve<ILogProvider>().GetLogger("Performance." + GetType().Name);
                performanceLogger.Write(stopwatch, "Modules and plugins registered.");
                ((UnitOfWorkScope)scope).LogRegistrationStatistics("UpdateDatabase component registrations", _logProvider);
                scope.Resolve<DatabaseDeployment>().UpdateDatabase();
                // DbUpdate scope does not contain IPersistenceTransaction, so there is no need to call scope.CommitOnDispose() here. It would throw an exception.
            }
        }

        private void SetDbUpdateComponents(IConfiguration configuration, ContainerBuilder builder, List<Action<ContainerBuilder>> configureActions)
        {
            // DbUpdate overrides default runtime components with OverrideContainerConfiguration,
            // because it does not require full runtime context.
            // Instead it registers only basic CoreModule and part of the runtime from DbUpdateModule.
            // AddPluginModules allows custom override of DbUpdate components if needed.

            // Custom configuration from "configureActions" parameter is intentionally ignored.
            // It is intended for application runtime and AppInitialization container.
            // DbUpdate container can be customized by standard plugin classes with Export attribute
            // or in a Autofac.Module plugin implementation if more control is needed
            // (also with Export attribute on the Module).

            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DbUpdateModule());
            builder.AddRhetosPluginModules();
            builder.RegisterType<NullUserInfo>().As<IUserInfo>(); // Override any runtime IUserInfo plugins. This container should not execute the application's business features, so IUserInfo is not expected to be used.
        }

        //=====================================================================

        public void InitializeGeneratedApplication()
        {
            // Creating a new container builder instead of using builder.Update(), because of severe performance issues with the Update method.

            _logger.Info("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            Action<IRhetosHostBuilder> configureRhetosHost = builder =>
            {
                builder.UseBuilderLogProvider(_logProvider)
                    .ConfigureContainer(AddAppInitializationComponents);
            };

            using (var rhetosHost = _rhetosHostFactory(configureRhetosHost))
            {
                Type[] initializers;
                using (var scope = rhetosHost.CreateScope())
                {
                    // EfMappingViewsInitializer is manually executed before other initializers because of performance issues.
                    // It is needed for most of the initializer to run, but lazy initialization of EfMappingViews on first DbContext usage
                    // is not a good option because of significant hash check duration (it would not be applicable in run-time).
                    _logger.Info("Initializing EfMappingViews.");
                    var efMappingViewsInitializer = scope.Resolve<EfMappingViewsInitializer>();
                    efMappingViewsInitializer.Initialize();

                    var performanceLogger = scope.Resolve<ILogProvider>().GetLogger("Performance." + GetType().Name);
                    initializers = ApplicationInitialization.GetSortedInitializers(scope);

                    performanceLogger.Write(stopwatch, "New modules and plugins registered.");
                    ((UnitOfWorkScope)scope).LogRegistrationStatistics("InitializeApplication component registrations", _logProvider);
                    scope.CommitAndClose();
                }

                if (!initializers.Any())
                {
                    _logger.Info("No server initialization plugins.");
                }
                else
                {
                    foreach (Type initializerType in initializers)
                        ApplicationInitialization.ExecuteInitializer(rhetosHost, initializerType, _logProvider);
                }
            }
        }

        private void AddAppInitializationComponents(ContainerBuilder builder)
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
