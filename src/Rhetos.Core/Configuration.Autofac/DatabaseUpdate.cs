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
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Diagnostics;

namespace Rhetos.Deployment
{
    public class DatabaseUpdate
    {
        private readonly Func<Action<IRhetosHostBuilder>, RhetosHost> _rhetosHostFactory;
        private readonly ILogger _logger;
        private readonly ILogProvider _logProvider;

        public DatabaseUpdate(Func<Action<IRhetosHostBuilder>, RhetosHost> rhetosHostFactory, ILogProvider logProvider)
        {
            // UpdateDatabase does not need full IRhetosHostBuilder, see OverrideContainerConfiguration below.
            // It should work without runtime components registration, only runtime configuration settings are required, for database connection and similar.
            _rhetosHostFactory = rhetosHostFactory;
            _logProvider = logProvider;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public void UpdateDatabase()
        {
            _logger.Info("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            using (var rhetosHost = _rhetosHostFactory(ConfigureRhetosHost))
            using (var scope = rhetosHost.CreateScope())
            {
                var performanceLogger = scope.Resolve<ILogProvider>().GetLogger("Performance." + GetType().Name);
                performanceLogger.Write(stopwatch, "Modules and plugins registered.");
                ((UnitOfWorkScope)scope).LogRegistrationStatistics("UpdateDatabase component registrations", _logProvider);
                scope.Resolve<DatabaseDeployment>().UpdateDatabase();
                // No need to call scope.CommitAndClose() here, since DbUpdate has its own transaction management.
                // See UseDatabaseTransaction set to false in configuration above.
            }
        }

        private void ConfigureRhetosHost(IRhetosHostBuilder builder)
        {
            builder.ConfigureConfiguration(configurationBuilder =>
            {
                // Database update components manage transactions manually.
                // A single overarching transaction would cause performance and memory issues with large databases,
                // and also make impossible for some advanced operations that need to be executed without database transaction
                // (for example, creating a full-text search index).
                configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey<PersistenceTransactionOptions>(o => o.UseDatabaseTransaction), false);
            });
            builder.UseBuilderLogProvider(_logProvider);

            // DbUpdate overrides default runtime components with OverrideContainerConfiguration,
            // because it does not require full runtime context.
            // Instead it registers only basic CoreModule and part of the runtime from DbUpdateModule.
            // AddPluginModules allows custom override of DbUpdate components if needed.
            builder.OverrideContainerConfiguration((configuration, containerBuilder, configureActions) =>
            {
                // Custom configuration from "configureActions" parameter is intentionally ignored.
                // It is intended for application runtime and AppInitialization container.
                // DbUpdate container can be customized by standard plugin classes with Export attribute
                // or in a Autofac.Module plugin implementation if more control is needed
                // (also with Export attribute on the Module).

                containerBuilder.Properties[nameof(ExecutionStage)] = new ExecutionStage { IsDatabaseUpdate = true }; // Overrides the default 'Runtime' setting from RhetosHostBuilder.
                containerBuilder.RegisterModule(new CoreModule());
                containerBuilder.RegisterModule(new DbUpdateModule());
                containerBuilder.AddRhetosPluginModules();
                containerBuilder.RegisterType<NullUserInfo>().As<IUserInfo>(); // Override any runtime IUserInfo plugins. This container should not execute the application's business features, so IUserInfo is not expected to be used.
            });
        }
    }
}
