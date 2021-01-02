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
    public class RhetosHost : IDisposable
    {
        public static readonly string HostBuilderFactoryMethodName = "CreateRhetosHostBuilder";

        public IContainer Container { get; }

        public RhetosHost(IContainer container)
        {
            this.Container = container;
        }

        public TransactionScopeContainer CreateScope(Action<ContainerBuilder> registerScopeComponentsAction = null)
        {
            return new TransactionScopeContainer(Container, registerScopeComponentsAction);
        }

        public static IRhetosHostBuilder FindBuilder(string hostFilePath)
        {
            var hostDirectory = Path.GetDirectoryName(hostFilePath);

            var startupAssembly = ResolveStartupAssembly(hostFilePath);

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

            var rhetosHostBuilder = (IRhetosHostBuilder)method.Invoke(null, new object[] { });
            rhetosHostBuilder.UseRootFolder(hostDirectory); // use host directory as root for all RhetosHostBuilder operations
            return rhetosHostBuilder;
        }

        private static Assembly ResolveStartupAssembly(string hostFilename)
        {
            var assemblyDependencyResolver = new AssemblyDependencyResolver(hostFilename);
            var currentAcl = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
            currentAcl.Resolving += (AssemblyLoadContext acl, AssemblyName assemblyName) =>
            {
                return acl.LoadFromAssemblyPath(assemblyDependencyResolver.ResolveAssemblyToPath(assemblyName));
            };

            currentAcl.ResolvingUnmanagedDll += (Assembly assembly, string assemblyName) =>
            {
                return NativeLibrary.Load(assemblyDependencyResolver.ResolveUnmanagedDllToPath(assemblyName));
            };

            return currentAcl.LoadFromAssemblyPath(hostFilename);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Container?.Dispose();
            }
        }
    }
}
