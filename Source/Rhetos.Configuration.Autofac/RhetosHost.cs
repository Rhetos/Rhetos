using System;
using System.IO;
using System.Reflection;
using Autofac;
using Rhetos.Extensibility;
using Rhetos.Utilities;

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

        public TransactionScopeContainer CreateScope()
        {
            return new TransactionScopeContainer(Container);
        }

        public static IRhetosHostBuilder FindBuilder(string hostFilename, params string[] assemblyProbingDirectories)
        {
            var directory = Path.GetDirectoryName(hostFilename);
            if (!string.IsNullOrEmpty(directory))
                throw new FrameworkException($"Host filename '{hostFilename}' shouldn't contain directory/path.");

            var startupAssembly = ResolveStartupAssembly(hostFilename, assemblyProbingDirectories);

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
            rhetosHostBuilder.AddAssemblyProbingDirectories(assemblyProbingDirectories); // preserve probing directories which were used to locate this builder
            return rhetosHostBuilder;
        }

        private static Assembly ResolveStartupAssembly(string hostFilename, params string[] assemblyProbingDirectories)
        {
            ResolveEventHandler resolveEventHandler = null;
            if (assemblyProbingDirectories.Length > 0)
            {
                var assemblies = AssemblyResolver.GetRuntimeAssemblies(assemblyProbingDirectories);
                resolveEventHandler = AssemblyResolver.GetResolveEventHandler(assemblies, new ConsoleLogProvider(), true);
                AppDomain.CurrentDomain.AssemblyResolve += resolveEventHandler;
            }

            try
            {
                var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(hostFilename));
                var startupAssembly = Assembly.Load(assemblyName);
                if (startupAssembly == null)
                    throw new InvalidOperationException($"Assembly load for '{assemblyName.Name}' failed.");
                return startupAssembly;
            }
            catch (Exception e)
            {
                throw new FrameworkException($"Error loading startup assembly '{hostFilename}': {e.Message}", e);
            }
            finally
            {
                if (resolveEventHandler != null)
                    AppDomain.CurrentDomain.AssemblyResolve -= resolveEventHandler;
            }
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
