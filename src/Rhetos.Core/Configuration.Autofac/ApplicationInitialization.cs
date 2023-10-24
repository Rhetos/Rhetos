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
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Deployment
{
    public class ApplicationInitialization
    {
        private readonly Func<Action<IRhetosHostBuilder>, RhetosHost> _rhetosHostFactory;
        private readonly ILogger _logger;
        private readonly ILogProvider _logProvider;

        public ApplicationInitialization(Func<Action<IRhetosHostBuilder>, RhetosHost> rhetosHostFactory, ILogProvider logProvider)
        {
            _rhetosHostFactory = rhetosHostFactory;
            _logProvider = logProvider;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public void InitializeGeneratedApplication()
        {
            // Creating a new container builder instead of using builder.Update(), because of severe performance issues with the Update method.

            _logger.Info("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            using (var rhetosHost = _rhetosHostFactory(ConfigureRhetosHost))
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
                    initializers = GetSortedInitializers(scope);

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
                        ExecuteInitializer(rhetosHost, initializerType, _logProvider);
                }
            }
        }

        private void ConfigureRhetosHost(IRhetosHostBuilder hostBuilder)
        {
            hostBuilder.UseBuilderLogProvider(_logProvider);
            hostBuilder.ConfigureContainer(containerBuilder =>
            {
                containerBuilder.RegisterModule(new AppInitializeModule());
                containerBuilder.RegisterType<ProcessUserInfo>().As<IUserInfo>(); // Override runtime IUserInfo plugins. This container is intended to be used in a simple process.
            });
        }

        public static Type[] GetSortedInitializers(IUnitOfWorkScope scope)
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var initializers = scope.Resolve<IPluginsContainer<IServerInitializer>>().GetPlugins();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var initNames = initializers.Select(init => init.GetType().FullName).ToList();
            var initDependencies = initializers.SelectMany(init => (init.Dependencies ?? Array.Empty<string>()).Select(x => Tuple.Create(x, init.GetType().FullName)));
            Graph.TopologicalSort(initNames, initDependencies);

            var sortedInitializers = initializers.ToArray();
            Graph.SortByGivenOrder(sortedInitializers, initNames.ToArray(), init => init.GetType().FullName);
            return sortedInitializers.Select(initializer => initializer.GetType()).ToArray();
        }

        /// <summary>
        /// Note:
        /// This method does not conform to the standard IoC design pattern.
        /// It uses IoC container directly because it needs to handle a special scope control (separate database connections) and error handling.
        /// </summary>
        public static void ExecuteInitializer(RhetosHost container, Type initializerType, ILogProvider logProvider)
        {
            var logger = logProvider.GetLogger(nameof(ApplicationInitialization));
            
            using (var scope = container.CreateScope())
            {
                logger.Info($"Initialization {initializerType.Name}.");
                var initializers = scope.Resolve<IPluginsContainer<IServerInitializer>>().GetPlugins();
                IServerInitializer initializer = initializers.Single(i => i.GetType() == initializerType);
                initializer.Initialize();
                scope.CommitAndClose();
            }
        }
    }
}
