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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Rhetos.Utilities;
using System;
using System.IO;

namespace Rhetos
{
    /// <summary>
    /// <see cref="RhetosHost"/> encapsulates the Rhetos application's components (DI container).
    /// This class is thread-safe: a single instance can be reused between threads to reduce the initialization time
    /// (Entity Framework startup and plugin discovery, e.g.).
    /// For each thread or unit of work, call <see cref="CreateScope(Action{ContainerBuilder})"/> to create
    /// a new lifetime scope for the dependency injection container.
    /// Note that created lifetime scope is <see cref="IDisposable"/>.
    /// Each scope uses its own database transaction that is either committed or rolled back
    /// when the instance is disposed, making data changes atomic.
    /// </summary>
    /// <remarks>
    /// When accessing Rhetos components from host application's services, use <see cref="IRhetosComponent{T}"/>
    /// to get a Rhetos component in a constructor parameter, instead of using <see cref="RhetosHost"/> directly.
    /// </remarks>
    public class RhetosHost : IDisposable
    {
        private readonly IContainer _container;

        private bool disposed;

        public RhetosHost(IContainer container)
        {
            _container = container;
            _container.ResolveOptional<UnitOfWorkFactory>()?.Initialize(this);
        }

        /// <summary>
        /// Creates a thread-safe lifetime scope for dependency injection container,
        /// in order to isolate a unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="IUnitOfWork.CommitAndClose"/> at the end of the 'using' block.
        /// Note that created lifetime scope is <see cref="IDisposable"/>.
        /// Transaction will be committed or rolled back when scope is disposed.
        /// </summary>
        /// <param name="registerScopeComponentsAction">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// <para>
        /// Note that the transaction-scope component registration might not affect singleton components.
        /// Customize the behavior of singleton components with <see cref="IRhetosHostBuilder"/> methods,
        /// before building the <see cref="RhetosHost"/> instance with <see cref="IRhetosHostBuilder.Build"/>.
        /// </para>
        /// </param>
        public IUnitOfWorkScope CreateScope(Action<ContainerBuilder> registerScopeComponentsAction = null)
        {
            return new UnitOfWorkScope(_container, registerScopeComponentsAction);
        }

        /// <summary>
        /// Provides direct access to the internal DI container.
        /// </summary>
        /// <remarks>
        /// In most cases this method should not be called directly.
        /// Instead create a scope (unit of work) with <see cref="CreateScope"/>,
        /// and resolve components from it.
        /// </remarks>
        public IContainer GetRootContainer() => _container;

        /// <summary>
        /// Creates the Rhetos runtime context for the given application, using the application's setup and configuration.
        /// </summary>
        /// <remarks>
        /// This method is intended to be called by a utility application that needs to use Rhetos runtime features
        /// of a referenced application or assembly built with Rhetos framework.
        /// <para>
        /// This method resolves <see cref="RhetosHost"/> from the referenced assembly's <see cref="IHostBuilder"/>.
        /// The referenced assembly is expected to have an entry point (typically the Program class) with standard
        /// CreateHostBuilder method (see <see href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0">.NET Generic Host</see>),
        /// or ASP.NET 6 minimal hosting model (<see href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0">Overview</see>, <see href="https://docs.microsoft.com/en-us/aspnet/core/migration/50-to-60-samples?view=aspnetcore-6.0">Samples</see>).
        /// The specified application should be created with Rhetos framework, or reference an assembly that
        /// was created with Rhetos framework.
        /// </para>
        /// </remarks>
        /// <returns>
        /// It returns a <see cref="RhetosHost"/> that is created and configuration by the referenced application.
        /// </returns>
        /// <param name="rhetosHostAssemblyPath">
        /// Path to assembly where the CreateHostBuilder method is located.
        /// </param>
        /// <param name="configureRhetosHost">
        /// Configures Rhetos dependency injection components and configuration.
        /// </param>
        /// <param name="configureServices">
        /// Configures host application's dependency injection components and configuration.
        /// </param>
        public static RhetosHost CreateFrom(
            string rhetosHostAssemblyPath,
            Action<IRhetosHostBuilder> configureRhetosHost = null,
            Action<HostBuilderContext, IServiceCollection> configureServices = null)
        {
            var services = GetHostServices(rhetosHostAssemblyPath, configureRhetosHost, configureServices);
            return services.GetService<RhetosHost>();
        }

        /// <summary>
        /// Provides services from the referenced host application.
        /// Use the <see cref="CreateFrom"/> method instead, it you only need Rhetos context and components.
        /// </summary>
        public static IServiceProvider GetHostServices(
            string rhetosHostAssemblyPath,
            Action<IRhetosHostBuilder> configureRhetosHost = null,
            Action<HostBuilderContext, IServiceCollection> configureServices = null)
        {
            // Using the full path for better error reporting.
            rhetosHostAssemblyPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rhetosHostAssemblyPath));
            if (!File.Exists(rhetosHostAssemblyPath))
                throw new ArgumentException($"Please specify the host application assembly file. File '{rhetosHostAssemblyPath}' does not exist.");

            void ConfigureHostBuilder(IHostBuilder hostBuilder)
            {
                hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    services.AddRhetosHost((serviceProvider, rhetosHostBuilder) =>
                    {
                        // Overriding Rhetos host application's location settings, because the default values might be incorrect when the host assembly is executed
                        // from another process with FindBuilder. For example, it could have different AppDomain.BaseDirectory, or the assembly copied in shadow directory.
                        rhetosHostBuilder.UseRootFolder(Path.GetDirectoryName(rhetosHostAssemblyPath)); // Use host assembly directory as root for all RhetosHostBuilder operations.
                        rhetosHostBuilder.ConfigureConfiguration(configurationBuilder => configurationBuilder.AddKeyValue(
                            ConfigurationProvider.GetKey((RhetosAppOptions o) => o.RhetosHostFolder),
                            Path.GetDirectoryName(rhetosHostAssemblyPath))); // Override the RhetosHostFolder to make sure it is set to the original host folder location, not a shadow copy (for applications such as LINQPad).                

                        configureRhetosHost?.Invoke(rhetosHostBuilder);
                    });
                });

                if (configureServices != null)
                    hostBuilder.ConfigureServices(configureServices);
            }

            IHost host = HostResolver.ResolveHost(rhetosHostAssemblyPath, Array.Empty<string>(), ConfigureHostBuilder);
            return host.Services;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _container?.Dispose();
                }

                disposed = true;
            }
        }
    }
}
