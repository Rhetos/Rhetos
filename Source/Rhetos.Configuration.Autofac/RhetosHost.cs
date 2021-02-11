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

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Autofac;

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
        /// The application is expected to have an entry point (typically the Program class) with a
        /// static method that creates and configures a <see cref="IRhetosHostBuilder"/> instance,
        /// see <see cref="HostBuilderFactoryMethodName"/>.
        /// The specified application should be created with Rhetos framework, or reference an assembly that
        /// was created with Rhetos framework.
        /// </summary>
        /// <param name="hostFilePath">Path of the application's assembly file (.dll or .exe).</param>
        public static IRhetosHostBuilder FindBuilder(string hostFilePath)
        {
            if (!File.Exists(hostFilePath))
                throw new ArgumentException($"Please specify the host application assembly file. File '{hostFilePath}' does not exist.");

            // The Assembly.LoadFrom method would load the Assembly in the DefaultLoadContext.
            // In most cases this will work but when using LINQPad this can lead to unexpected behavior when comparing types because
            // LINQPad will load the reference assemblies in another context.
            // The code below is similar to Assembly.Load(AssemblyName) method: It loads the Assembly in
            // the AssemblyLoadContext.CurrentContextualReflectionContext, if it is set, or AssemblyLoadContext.Default.
            // We are using this behavior because if needed the application developer can change the AssemblyLoadContext.CurrentContextualReflectionContext
            // with AssemblyLoadContext.EnterContextualReflection.
            var startupAssembly = (AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default).LoadFromAssemblyPath(hostFilePath);
            if (startupAssembly == null)
                throw new FrameworkException($"Could not resolve assembly from path '{hostFilePath}'.");

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

            var rhetosHostBuilder = (IRhetosHostBuilder)method.Invoke(null, Array.Empty<object>());
            rhetosHostBuilder.UseRootFolder(Path.GetDirectoryName(hostFilePath)); // use host directory as root for all RhetosHostBuilder operations
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
