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
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Diagnostics;
using System.Threading;

namespace Rhetos.Configuration.Autofac
{
    /// <summary>
    /// It encapsulates a Dependency Injection container (see 
    /// <see cref="Host.CreateRhetosContainer(string, ILogProvider, Action{IConfigurationBuilder}, Action{ContainerBuilder})"/>)
    /// for creating the lifetime-scope child containers with <see cref="CreateTransactionScope"/>.
    /// Use the child containers to isolate units of work into separate atomic transactions.
    /// 
    /// RhetosProcessContainer is thread-safe: the main RhetosProcessContainer instance can be reused between threads
    /// to reduce the initialization time, such as plugin discovery and Entity Framework startup.
    /// Each thread should use <see cref="CreateTransactionScope"/> to create its own lifetime-scope child container.
    /// </summary>
    public class RhetosProcessContainer
    {
        private readonly Lazy<IContainer> _rhetosIocContainer;
        private readonly Lazy<Host> _host;
        private readonly Lazy<IConfiguration> _configuration;

        public IConfiguration Configuration => _configuration.Value;

        /// <param name="applicationFolder">
        /// Folder where the Rhetos configuration file is located (see <see cref="RhetosAppEnvironment.ConfigurationFileName"/>),
        /// or any subfolder.
        /// If not specified, the current application's base directory is used by default.
        /// </param>
        /// <param name="logProvider">
        /// If not specified, ConsoleLogProvider is used by default.
        /// </param>
        public RhetosProcessContainer(Func<string> applicationFolder = null, ILogProvider logProvider = null,
            Action<IConfigurationBuilder> addCustomConfiguration = null, Action<ContainerBuilder> registerCustomComponents = null)
        {
            logProvider = logProvider ?? new ConsoleLogProvider();
            if (applicationFolder == null)
                applicationFolder = () => AppDomain.CurrentDomain.BaseDirectory;

            _host = new Lazy<Host>(() => Host.Find(applicationFolder(), logProvider), LazyThreadSafetyMode.ExecutionAndPublication);
            _configuration = new Lazy<IConfiguration>(() => _host.Value.RhetosRuntime.BuildConfiguration(logProvider, _host.Value.ConfigurationFolder, addCustomConfiguration), LazyThreadSafetyMode.ExecutionAndPublication);
            _rhetosIocContainer = new Lazy<IContainer>(() => BuildRhetosContainer(logProvider, registerCustomComponents), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private IContainer BuildRhetosContainer(ILogProvider logProvider, Action<ContainerBuilder> registerCustomComponents)
        {
            //The values for rhetosRuntime and configuration are resolved before the call to Stopwatch.StartNew
            //so that the performance logging only takes into account the time needed to build the IOC container
            var sw = Stopwatch.StartNew();
            var iocContainer = _host.Value.RhetosRuntime.BuildContainer(logProvider, _configuration.Value, builder =>
            {
                // Override runtime IUserInfo plugins. This container is intended to be used in a simple process or unit tests.
                builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                builder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();
                registerCustomComponents?.Invoke(builder);
            });
            logProvider.GetLogger("Performance").Write(sw, $"{nameof(RhetosTransactionScopeContainer)}: Built IoC container");
            return iocContainer;
        }

        public T Resolve<T>() => _rhetosIocContainer.Value.Resolve<T>();

        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked. By default it is set to true.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Used to customize the transaction scope Dependency Injection container. By default it is set to null.
        /// </param>/// 
        public RhetosTransactionScopeContainer CreateTransactionScope(bool commitChanges, Action<ContainerBuilder> registerCustomComponents = null) => new RhetosTransactionScopeContainer(_rhetosIocContainer, commitChanges, registerCustomComponents);
    }
}
