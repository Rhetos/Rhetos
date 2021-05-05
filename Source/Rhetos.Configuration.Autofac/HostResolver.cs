using Microsoft.Extensions.Hosting;
using Rhetos.Utilities;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Rhetos
{
    public class HostResolver
    {
        public static readonly string HostBuilderFactoryMethodName = "CreateHostBuilder";

        public static IHostBuilder FindBuilder(string rhetosHostAssemblyPath)
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

            if (method.ReturnType != typeof(IHostBuilder))
                throw new FrameworkException($"Static method '{entryPointType.FullName}.{HostBuilderFactoryMethodName}' has incorrect return type. Expected return type is {nameof(IRhetosHostBuilder)}.");

            return (IHostBuilder)method.InvokeEx(null, new object[] { Array.Empty<string>() });
        }
    }
}
