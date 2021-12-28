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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;

namespace Rhetos
{
    /// <summary>
    /// <see cref="LinqPadRhetosHost"/> is a helper class for accessing the generated application object model in LINQPad.
    /// This class is thread-safe: a single instance can be reused between threads to reduce the initialization time
    /// (Entity Framework startup and plugin discovery).
    /// For each unit of work, call <see cref="CreateScope"/> to create
    /// a lifetime-scope for the dependency injection container.
    /// Each child container uses its own database transaction that is either committed or rolled back
    /// when the instance is disposed, making data changes atomic.
    /// <see cref="LinqPadRhetosHost"/> overrides the main application's Rhetos DI components to use <see cref="ProcessUserInfo"/>
    /// and <see cref="ConsoleLogProvider"/>. It also overrides host builder to use <see cref="Microsoft.Extensions.Logging.Console.ConsoleLogger"/>.
    /// </summary>
    public static class LinqPadRhetosHost
    {
        
        private static RhetosHost _singleRhetosHost = null;
        private static string _singleRhetosHostAssemblyPath = null;
        private static readonly object _singleContainerLock = new object();

        /// <summary>
        /// This method creates a thread-safe lifetime scope DI container to isolate unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="UnitOfWorkScope.CommitAndClose"/> at the end of the 'using' block.
        /// </summary>
        /// <remarks>
        /// In most cases it is preferred to use a <see cref="RhetosHost"/> instance, instead of this static method, for better control over the DI container.
        /// The static method is useful in some special cases, for example to optimize LINQPad scripts that can reuse the external static instance
        /// after recompiling the script.
        /// </remarks>
        /// <param name="rhetosAppAssemblyPath">
        /// Path to assembly where the CreateHostBuilder method is located.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override application's services and plugins.
        /// This is commonly used by utilities and tests that need to override host application's Rhetos components or register additional plugins.
        /// <para>
        /// Note that the transaction-scope component registration will not affect singleton components.
        /// To customize the behavior of singleton components use <see cref="RhetosHost"/> directly.
        /// </para>
        /// </param>
        /// /// <param name="configureServices">
        /// Configures host application's dependency injection components and configuration.
        /// </param>
        public static UnitOfWorkScope CreateScope(
            string rhetosAppAssemblyPath,
            Action<ContainerBuilder> registerCustomComponents = null,
            Action<HostBuilderContext, IServiceCollection> configureServices = null)
        {
            if (_singleRhetosHost == null)
                lock (_singleContainerLock)
#pragma warning disable CA1508 // Avoid dead conditional code. This code uses standard double-checked locking, see https://en.wikipedia.org/wiki/Double-checked_locking#Usage_in_C#
                    if (_singleRhetosHost == null)
#pragma warning restore CA1508 // Avoid dead conditional code
                    {
                        _singleRhetosHostAssemblyPath = rhetosAppAssemblyPath;
                        _singleRhetosHost = RhetosHost.CreateFrom(rhetosAppAssemblyPath, ConfigureRhetosHost, OverrideHostLogging + configureServices);
                    }

            if (_singleRhetosHostAssemblyPath != rhetosAppAssemblyPath)
                throw new FrameworkException($"Static {nameof(LinqPadRhetosHost)}.{nameof(CreateScope)} cannot be used for different" +
                    $" application contexts: Provided folder 1: '{_singleRhetosHostAssemblyPath}', folder 2: '{rhetosAppAssemblyPath}'." +
                    $" Use a {nameof(RhetosHost)} instances instead.");

            return _singleRhetosHost.CreateScope(registerCustomComponents);
        }

        private static void ConfigureRhetosHost(IRhetosHostBuilder rhetosHostBuilder)
        {
            rhetosHostBuilder
                .UseBuilderLogProvider(new ConsoleLogProvider())
                .ConfigureContainer(builder =>
                {
                    // Override runtime IUserInfo plugins. This container is intended to be used
                    // in a process that is executed directly by user, usually by developer or administrator.
                    builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                    builder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();
                });
        }

        private static void OverrideHostLogging(HostBuilderContext context, IServiceCollection services)
        {
            // Without overriding default host logging, using Rhetos application from external utility can sometimes
            // result with PlatformNotSupportedException "EventLog access is not supported on this platform.",
            // when the default logger is internally resolved in Microsoft.Extensions.Hosting.HostBuilder.Build().
            // For example, when running the default Rhetos LINQPad script on CommonConcept.Test project.
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
        }
    }
}
