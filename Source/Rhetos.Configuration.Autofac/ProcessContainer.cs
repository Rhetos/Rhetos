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

namespace Rhetos
{
    /// <summary>
    /// <see cref="ProcessContainer"/> is a helper class for accessing the generated application object model in unit tests and command-line utilities.
    /// This class is thread-safe: a single instance can be reused between threads to reduce the initialization time
    /// (Entity Framework startup and plugin discovery).
    /// For each unit of work, call <see cref="CreateScope(Action{ContainerBuilder})"/> to create
    /// a lifetime-scope for the dependency injection container.
    /// Each child container uses its own database transaction that is either committed or rolled back
    /// when the instance is disposed, making data changes atomic.
    /// <see cref="ProcessContainer"/> overrides the main application's DI components to use <see cref="ProcessUserInfo"/>
    /// and <see cref="ConsoleLogProvider"/>.
    /// </summary>
    public class ProcessContainer : IDisposable
    {
        private readonly Lazy<RhetosHost> _rhetosIocContainer;

        /// <param name="rhetosAppAssemblyPath">
        /// Path to assembly where the CreateRhetosHostBuilder method is located.
        /// </param>
        /// <param name="logProvider">
        /// If not specified, <see cref="ConsoleLogProvider"/> is used by default.
        /// The specified <paramref name="logProvider"/> will be used during initialization of configuration and dependency injection container.
        /// Note that this log provider is not registered to DI container by default;
        /// customize run-time logging with <paramref name="registerCustomComponents"/>.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// </param>
        public ProcessContainer(string rhetosAppAssemblyPath, ILogProvider logProvider = null,
            Action<IConfigurationBuilder> addCustomConfiguration = null, Action<ContainerBuilder> registerCustomComponents = null)
        {
            logProvider = logProvider ?? LoggingDefaults.DefaultLogProvider;

            _rhetosIocContainer = new Lazy<RhetosHost>(() => BuildProcessContainer(rhetosAppAssemblyPath, logProvider, addCustomConfiguration, registerCustomComponents), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private RhetosHost BuildProcessContainer(string rhetosAppAssemblyPath, ILogProvider logProvider,
            Action<IConfigurationBuilder> addCustomConfiguration, Action<ContainerBuilder> registerCustomComponents)
        {
            // The values for rhetosRuntime and configuration are resolved before the call to Stopwatch.StartNew
            // so that the performance logging only takes into account the time needed to build the IOC container
            var sw = Stopwatch.StartNew();

            var rhetosHost = RhetosHost.Find(rhetosAppAssemblyPath, rhetosHostBuilder =>
            {
                rhetosHostBuilder.ConfigureConfiguration(configuration =>
                    {
                        addCustomConfiguration?.Invoke(configuration);
                    })
                    .ConfigureContainer(builder =>
                    {
                        // Override runtime IUserInfo plugins. This container is intended to be used in unit tests or
                        // in a process that is executed directly by user, usually by developer or administrator.
                        builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                        builder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();

                        registerCustomComponents?.Invoke(builder);
                    });
            });

            logProvider.GetLogger("Performance." + GetType().Name).Write(sw, $"Built IoC container");
            return rhetosHost;
        }

        /// <summary>
        /// This method creates a thread-safe lifetime scope DI container to isolate unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="UnitOfWorkScope.CommitAndClose"/> at the end of the 'using' block.
        /// </summary>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// <para>
        /// Note that the transaction-scope component registration might not affect singleton components.
        /// Customize the behavior of singleton components in <see cref="ProcessContainer"/> constructor.
        /// </para>
        /// </param>
        public UnitOfWorkScope CreateScope(Action<ContainerBuilder> registerCustomComponents = null)
        {
#pragma warning disable CS0618 // Type or member is obsolete. It provides a derivation of UnitOfWorkScope for legacy methods.
            return _rhetosIocContainer.Value.CreateScope(registerCustomComponents);
#pragma warning restore CS0618
        }

        #region Static helper for singleton ProcessContainer. Useful optimization for LINQPad scripts that reuse the external static instance after recompiling the script.

        private static ProcessContainer _singleContainer = null;
        private static string _singleContainerAssemblyPath = null;
        private static readonly object _singleContainerLock = new object();

        /// <summary>
        /// This method creates a thread-safe lifetime scope DI container to isolate unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="UnitOfWorkScope.CommitAndClose"/> at the end of the 'using' block.
        /// </summary>
        /// <remarks>
        /// In most cases it is preferred to use a <see cref="ProcessContainer"/> instance, instead of this static method, for better control over the DI container.
        /// The static method is useful in some special cases, for example to optimize LINQPad scripts that can reuse the external static instance
        /// after recompiling the script.
        /// </remarks>
        /// <param name="rhetosAppAssemblyPath">
        /// Path to assembly where the CreateRhetosHostBuilder method is located.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// <para>
        /// Note that the transaction-scope component registration might not affect singleton components.
        /// Customize the behavior of singleton components in <see cref="ProcessContainer"/> constructor.
        /// </para>
        /// </param>
        public static UnitOfWorkScope CreateScope(string rhetosAppAssemblyPath, Action<ContainerBuilder> registerCustomComponents = null)
        {
            if (_singleContainer == null)
                lock (_singleContainerLock)
                    if (_singleContainer == null)
                    {
                        _singleContainerAssemblyPath = rhetosAppAssemblyPath;
                        _singleContainer = new ProcessContainer(rhetosAppAssemblyPath);
                    }

            if (_singleContainerAssemblyPath != rhetosAppAssemblyPath)
                throw new FrameworkException($"Static {nameof(ProcessContainer)}.{nameof(CreateScope)} cannot be used for different" +
                    $" application contexts: Provided folder 1: '{_singleContainerAssemblyPath}', folder 2: '{rhetosAppAssemblyPath}'." +
                    $" Use a {nameof(ProcessContainer)} instances instead.");

            return _singleContainer.CreateScope(registerCustomComponents);
        }

        #endregion

        #region Standard IDisposable pattern

        private bool disposed = false;

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
            }

            disposed = true;
        }

        #endregion
    }
}
