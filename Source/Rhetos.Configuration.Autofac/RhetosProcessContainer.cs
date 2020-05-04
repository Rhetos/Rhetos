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
using Rhetos.Extensibility;
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
    /// 
    /// RhetosProcessContainer overrides the main application's DI components to use <see cref="ProcessUserInfo"/> and <see cref="ConsoleLogProvider"/> by default.
    /// It also registers assembly resolver for runtime assemblies from <see cref="IRhetosRuntime.GetRuntimeAssemblies(ILogProvider, IConfiguration)"/>.
    /// </summary>
    public class RhetosProcessContainer : IDisposable
    {
        private readonly Lazy<Host> _host;
        private readonly Lazy<IConfiguration> _configuration;
        private readonly Lazy<IContainer> _rhetosIocContainer;
        private ResolveEventHandler _assemblyResolveEventHandler = null;

        public IConfiguration Configuration => _configuration.Value;

        /// <param name="applicationFolder">
        /// Folder where the Rhetos configuration file is located (see <see cref="RhetosAppEnvironment.ConfigurationFileName"/>),
        /// or any subfolder.
        /// If not specified, the current application's base directory is used by default.
        /// </param>
        /// <param name="logProvider">
        /// If not specified, ConsoleLogProvider is used by default.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// </param>
        public RhetosProcessContainer(string applicationFolder = null, ILogProvider logProvider = null,
            Action<IConfigurationBuilder> addCustomConfiguration = null, Action<ContainerBuilder> registerCustomComponents = null)
        {
            logProvider = logProvider ?? new ConsoleLogProvider();
            if (applicationFolder == null)
                applicationFolder = AppDomain.CurrentDomain.BaseDirectory;

            _host = new Lazy<Host>(() => Host.Find(applicationFolder, logProvider), LazyThreadSafetyMode.ExecutionAndPublication);
            _configuration = new Lazy<IConfiguration>(() => _host.Value.RhetosRuntime.BuildConfiguration(logProvider, _host.Value.ConfigurationFolder, addCustomConfiguration), LazyThreadSafetyMode.ExecutionAndPublication);
            _rhetosIocContainer = new Lazy<IContainer>(() => BuildRhetosProcessContainer(logProvider, registerCustomComponents), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private IContainer BuildRhetosProcessContainer(ILogProvider logProvider, Action<ContainerBuilder> registerCustomComponents)
        {
            // The values for rhetosRuntime and configuration are resolved before the call to Stopwatch.StartNew
            // so that the performance logging only takes into account the time needed to build the IOC container
            var sw = Stopwatch.StartNew();

            var runtimeAssemblies = _host.Value.RhetosRuntime.GetRuntimeAssemblies(logProvider, _configuration.Value);
            _assemblyResolveEventHandler = AssemblyResolver.GetResolveEventHandler(runtimeAssemblies, logProvider);
            AppDomain.CurrentDomain.AssemblyResolve += _assemblyResolveEventHandler;

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

        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// </param>
        public RhetosTransactionScopeContainer CreateTransactionScope(Action<ContainerBuilder> registerCustomComponents = null)
        {
            return new RhetosTransactionScopeContainer(_rhetosIocContainer.Value, registerCustomComponents);
        }

        private bool disposed = false; // Standard IDisposable pattern to detect redundant calls.

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (_rhetosIocContainer.IsValueCreated)
                    _rhetosIocContainer.Value.Dispose();
                if (_assemblyResolveEventHandler != null)
                    AppDomain.CurrentDomain.AssemblyResolve -= _assemblyResolveEventHandler;
            }

            disposed = true;
        }
    }
}
