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
using Rhetos.Utilities;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Rhetos
{
    /// <summary>
    /// <see cref="RhetosHost"/> is a helper class for accessing the Rhetos application's components (DI container).
    /// This class is thread-safe: a single instance can be reused between threads to reduce the initialization time
    /// (Entity Framework startup and plugin discovery, e.g.).
    /// For each thread or unit of work, call <see cref="CreateScope(Action{ContainerBuilder})"/> to create
    /// a new lifetime scope for the dependency injection container.
    /// Note that created lifetime scope is <see cref="IDisposable"/>.
    /// Each scope uses its own database transaction that is either committed or rolled back
    /// when the instance is disposed, making data changes atomic.
    /// </summary>
    public class RhetosHost : IDisposable
    {
        public static readonly string HostBuilderFactoryMethodName = "CreateRhetosHostBuilder";

        private readonly IContainer _container;

        private bool disposed;

        public RhetosHost(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Creates a thread-safe lifetime scope for dependency injection container,
        /// in order to isolate a unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="TransactionScopeContainer.CommitChanges"/> at the end of the 'using' block.
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
        public TransactionScopeContainer CreateScope(Action<ContainerBuilder> registerScopeComponentsAction = null)
        {
            return new TransactionScopeContainer(_container, registerScopeComponentsAction);
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
        /// Finds and loads the Rhetos runtime context of the main application.
        /// </summary>
        /// <remarks>
        /// This method is intended to be called by a utility application that needs to use Rhetos runtime features
        /// of a referenced application or assembly built with Rhetos framework.
        /// <para>
        /// The referenced assembly is expected to have an entry point (typically the Program class) with a
        /// static method that creates and configures a <see cref="IRhetosHostBuilder"/> instance,
        /// see <see cref="HostBuilderFactoryMethodName"/>.
        /// The specified application should be created with Rhetos framework, or reference an assembly that
        /// was created with Rhetos framework.
        /// </para>
        /// </remarks>
        /// <param name="rhetosHostAssemblyPath">
        /// Path to assembly where the CreateRhetosHostBuilder method is located.
        /// </param>
        /// <returns>It returns a IRhetosHostBuilder that is created and configuration by the referenced main application (<paramref name="rhetosHostAssemblyPath"/>).</returns>
        public static IRhetosHostBuilder FindBuilder(string rhetosHostAssemblyPath)
        {
            // Using the full path for better error reporting. If the absolute path is not provided, assuming the caller utility's location as the base path.
            rhetosHostAssemblyPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rhetosHostAssemblyPath));
            if (!File.Exists(rhetosHostAssemblyPath))
                throw new ArgumentException($"Please specify the host application assembly file. File '{rhetosHostAssemblyPath}' does not exist.");

            // The Assembly.LoadFrom method would load the Assembly in the DefaultLoadContext.
            // In most cases this will work but when using LINQPad this can lead to unexpected behavior when comparing types because
            // LINQPad will load the reference assemblies in another context.
            // The code below is similar to Assembly.Load(AssemblyName) method: It loads the Assembly in
            // the AssemblyLoadContext.CurrentContextualReflectionContext, if it is set, or AssemblyLoadContext.Default.
            // We are using this behavior because if needed the application developer can change the AssemblyLoadContext.CurrentContextualReflectionContext
            // with AssemblyLoadContext.EnterContextualReflection.
            var startupAssembly = (AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default).LoadFromAssemblyPath(rhetosHostAssemblyPath);
            if (startupAssembly == null)
                throw new FrameworkException($"Could not resolve assembly from path '{rhetosHostAssemblyPath}'.");

            var entryPointType = startupAssembly?.EntryPoint?.DeclaringType;
            if (entryPointType == null)
                throw new FrameworkException($"Startup assembly '{startupAssembly.Location}' doesn't have an entry point.");

            var method = entryPointType.GetMethod(HostBuilderFactoryMethodName, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new FrameworkException(
                    $"Static method '{entryPointType.FullName}.{HostBuilderFactoryMethodName}' not found in entry point type in assembly {startupAssembly.Location}."
                    + $" Method is required in entry point assembly for constructing a configured instance of {nameof(RhetosHost)}.");

            if (method.ReturnType != typeof(IRhetosHostBuilder))
                throw new FrameworkException($"Static method '{entryPointType.FullName}.{HostBuilderFactoryMethodName}' has incorrect return type. Expected return type is {nameof(IRhetosHostBuilder)}.");

            var rhetosHostBuilder = (IRhetosHostBuilder)method.InvokeEx(null, Array.Empty<object>());

            // Overriding Rhetos host application's location settings, because the default values might be incorrect when the host assembly is executed
            // from another process with FindBuilder. For example, it could have different AppDomain.BaseDirectory, or the assembly copied in shadow directory.
            rhetosHostBuilder.UseRootFolder(Path.GetDirectoryName(rhetosHostAssemblyPath)); // Use host assembly directory as root for all RhetosHostBuilder operations.
            rhetosHostBuilder.ConfigureConfiguration(configurationBuilder => configurationBuilder.AddKeyValue(
                ConfigurationProvider.GetKey((RhetosAppOptions o) => o.RhetosHostFolder),
                Path.GetDirectoryName(rhetosHostAssemblyPath))); // Override the RhetosHostFolder to make sure it is set to the original host folder location, not a shadow copy (for applications such as LINQPad).
            return rhetosHostBuilder;
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
