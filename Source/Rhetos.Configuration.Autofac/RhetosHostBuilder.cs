using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Newtonsoft.Json;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos
{
    public class RhetosHostBuilder : IRhetosHostBuilder
    {
        private ILogProvider builderLogProvider = new ConsoleLogProvider();
        private readonly List<Action<ContainerBuilder>> configureContainerActions = new List<Action<ContainerBuilder>>();
        private readonly List<Action<IConfigurationBuilder>> configureConfigurationActions = new List<Action<IConfigurationBuilder>>();
        private Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> customContainerConfigurationAction = null;
        private readonly List<string> probingDirectories = new List<string>();
        private ILogger buildLogger;

        public RhetosHostBuilder()
        {
        }

        public IRhetosHostBuilder UseBuilderLogProvider(ILogProvider logProvider)
        {
            builderLogProvider = logProvider;
            return this;
        }

        public IRhetosHostBuilder ConfigureConfiguration(Action<IConfigurationBuilder> configureAction)
        {
            configureConfigurationActions.Add(configureAction);
            return this;
        }

        public IRhetosHostBuilder ConfigureContainer(Action<ContainerBuilder> configureAction)
        {
            configureContainerActions.Add(configureAction);
            return this;
        }

        public IRhetosHostBuilder UseCustomContainerConfiguration(Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> containerConfigurationAction)
        {
            customContainerConfigurationAction = containerConfigurationAction;
            return this;
        }

        public IRhetosHostBuilder AddAssemblyProbingDirectories(params string[] assemblyProbingDirectories)
        {
            probingDirectories.AddRange(assemblyProbingDirectories);
            return this;
        }

        public RhetosHost Build()
        {
            ResolveEventHandler resolveEventHandler = null;
            
            if (probingDirectories.Count > 0)
            {
                var assemblyFiles = AssemblyResolver.GetRuntimeAssemblies(probingDirectories.ToArray());
                resolveEventHandler = AssemblyResolver.GetResolveEventHandler(assemblyFiles, builderLogProvider, true);
                AppDomain.CurrentDomain.AssemblyResolve += resolveEventHandler;
            }

            buildLogger = builderLogProvider.GetLogger(nameof(RhetosHost));
            try
            {
                var configuration = CreateConfiguration();
                var rhetosContainer = BuildContainer(configuration);
                return new RhetosHost(rhetosContainer);
            }
            catch (Exception e)
            {
                buildLogger.Error(() => $"Error during {nameof(RhetosHostBuilder)}.{nameof(Build)}: {e}");
                throw;
            }
            finally
            {
                if (resolveEventHandler != null)
                    AppDomain.CurrentDomain.AssemblyResolve -= resolveEventHandler;
            }
        }

        private IConfiguration CreateConfiguration()
        {
            var rhetosAppSettingsFilePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RhetosAppEnvironment.ConfigurationFileName));
            if (!File.Exists(rhetosAppSettingsFilePath))
                throw new FrameworkException($"Unable to initialize RhetosHost. Rhetos app settings file '{rhetosAppSettingsFilePath}' not found.");

            buildLogger.Info(() => $"Initializing Rhetos app from '{rhetosAppSettingsFilePath}'.");
            
            var configurationBuilder = new ConfigurationBuilder(builderLogProvider)
                .AddJsonFile(rhetosAppSettingsFilePath);

            InvokeAll(configurationBuilder, configureConfigurationActions);

            return configurationBuilder.Build();
        }

        private IContainer BuildContainer(IConfiguration configuration)
        {
            var pluginAssemblies = AssemblyResolver.GetRuntimeAssemblies(configuration);
            var builder = new RhetosContainerBuilder(configuration, builderLogProvider, pluginAssemblies);

            var configurationAction = customContainerConfigurationAction ?? DefaultContainerConfiguration;
            configurationAction(configuration, builder, configureContainerActions);

            return builder.Build();
        }

        private void DefaultContainerConfiguration(IConfiguration configuration, ContainerBuilder builder, List<Action<ContainerBuilder>> customActions)
        {
            builder.AddRhetosRuntime();
            builder.AddPluginModules();

            InvokeAll(builder, customActions);
        }

        public static void InvokeAll<T>(T target, IEnumerable<Action<T>> actions)
        {
            foreach (var action in actions)
                action.Invoke(target);
        }
    }
}
