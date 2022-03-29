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

using Microsoft.Extensions.Hosting;
using Rhetos.Utilities;
using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Rhetos
{
    /// <summary>
    /// Wrapper around <see cref="HostFactoryResolver"/>.
    /// </summary>
    public static class HostResolver
    {
        /// <summary>
        /// Finds a standard IHostBuilder in the given Rhetos application's host assembly (the CreateHostBuilder method in the Program class).
        /// See <see href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0">.NET Generic Host</see> for more info.
        /// </summary>
        public static IHost ResolveHost(string rhetosHostAssemblyPath, string[] args, Action<IHostBuilder> configureHostBuilder)
        {
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

            var entryPointType = startupAssembly.EntryPoint?.DeclaringType;
            if (entryPointType == null)
                throw new FrameworkException($"Startup assembly '{startupAssembly.Location}' doesn't have an entry point.");

            var hostBuilderFactory = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(startupAssembly);
            if (hostBuilderFactory != null)
            {
                var hostBuilder = hostBuilderFactory(args);
                configureHostBuilder(hostBuilder);
                return hostBuilder.Build();
            }

            Action<object> configureHostBuilderCast = hostBuilder => configureHostBuilder((IHostBuilder)hostBuilder);
            var hostFactory = HostFactoryResolver.ResolveHostFactory(startupAssembly, configureHostBuilder: configureHostBuilderCast);
            if (hostFactory != null)
            {
                var host = CsUtility.Cast<IHost>(hostFactory(args), "IHost factory.");
                return host;
            }

            throw new FrameworkException(
                $"Cannot resolve {nameof(IHost)} from '{startupAssembly.Location}'." +
                $" Make sure that the assembly's entry point contains a static method '{entryPointType.FullName}.{HostFactoryResolver.CreateHostBuilder}'" +
                $" returning '{typeof(IHostBuilder)}', or that it uses \"minimal hosting model\" for ASP.NET 6.");
        }
    }
}
