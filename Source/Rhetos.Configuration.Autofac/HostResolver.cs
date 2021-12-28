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
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Rhetos
{
    public static class HostResolver
    {
        public static readonly string HostBuilderFactoryMethodName = "CreateHostBuilder";

        /// <summary>
        /// Finds a standard IHostBuilder in the given Rhetos application's host assembly (the CreateHostBuilder method in the Program class).
        /// See <see href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0">.NET Generic Host</see> for more info.
        /// </summary>
        public static IHostBuilder FindBuilder(string rhetosHostAssemblyPath)
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

            var method = entryPointType.GetMethod(HostBuilderFactoryMethodName, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new FrameworkException(
                    $"Static method '{entryPointType.FullName}.{HostBuilderFactoryMethodName}' not found in entry point type in assembly {startupAssembly.Location}."
                    + $" Method is required in entry point assembly for constructing a configured instance of {nameof(RhetosHost)}.");

            if (method.ReturnType != typeof(IHostBuilder))
                throw new FrameworkException($"Static method '{entryPointType.FullName}.{HostBuilderFactoryMethodName}' has incorrect return type. Expected return type is {nameof(IRhetosHostBuilder)}.");

            return (IHostBuilder)method.InvokeEx(null, new object[] { Array.Empty<string>() });
        }
    }
}
