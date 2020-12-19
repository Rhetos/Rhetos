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
using System.Collections.Generic;
using System.IO;
using Autofac;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos
{
    public class RhetosHostBuilder : IRhetosHostBuilder
    {
        private ILogProvider _builderLogProvider = LoggingDefaults.DefaultLogProvider;
        private readonly List<Action<ContainerBuilder>> _configureContainerActions = new List<Action<ContainerBuilder>>();
        private readonly List<Action<IConfigurationBuilder>> _configureConfigurationActions = new List<Action<IConfigurationBuilder>>();
        private Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> _customContainerConfigurationAction ;
        private readonly List<string> _probingDirectories = new List<string>();
        private ILogger _buildLogger;

        public RhetosHostBuilder()
        {
        }

        public IRhetosHostBuilder UseBuilderLogProvider(ILogProvider logProvider)
        {
            _builderLogProvider = logProvider;
            return this;
        }

        public IRhetosHostBuilder ConfigureConfiguration(Action<IConfigurationBuilder> configureAction)
        {
            _configureConfigurationActions.Add(configureAction);
            return this;
        }

        public IRhetosHostBuilder ConfigureContainer(Action<ContainerBuilder> configureAction)
        {
            _configureContainerActions.Add(configureAction);
            return this;
        }

        public IRhetosHostBuilder UseCustomContainerConfiguration(Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> containerConfigurationAction)
        {
            _customContainerConfigurationAction = containerConfigurationAction;
            return this;
        }

        public IRhetosHostBuilder AddAssemblyProbingDirectories(params string[] assemblyProbingDirectories)
        {
            _probingDirectories.AddRange(assemblyProbingDirectories);
            return this;
        }

        public RhetosHost Build()
        {
            ResolveEventHandler resolveEventHandler = null;
            
            if (_probingDirectories.Count > 0)
            {
                var assemblyFiles = AssemblyResolver.GetRuntimeAssemblies(_probingDirectories.ToArray());
                resolveEventHandler = AssemblyResolver.GetResolveEventHandler(assemblyFiles, _builderLogProvider, true);
                AppDomain.CurrentDomain.AssemblyResolve += resolveEventHandler;
            }

            _buildLogger = _builderLogProvider.GetLogger(nameof(RhetosHost));
            try
            {
                var configuration = CreateConfiguration();
                var rhetosContainer = BuildContainer(configuration);
                return new RhetosHost(rhetosContainer);
            }
            catch (Exception e)
            {
                _buildLogger.Error(() => $"Error during {nameof(RhetosHostBuilder)}.{nameof(Build)}: {e}");
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

            _buildLogger.Info(() => $"Initializing Rhetos app from '{rhetosAppSettingsFilePath}'.");
            
            var configurationBuilder = new ConfigurationBuilder(_builderLogProvider)
                .AddJsonFile(rhetosAppSettingsFilePath);

            InvokeAll(configurationBuilder, _configureConfigurationActions);

            return configurationBuilder.Build();
        }

        private IContainer BuildContainer(IConfiguration configuration)
        {
            var pluginAssemblies = AssemblyResolver.GetRuntimeAssemblies(configuration);
            var builder = new RhetosContainerBuilder(configuration, _builderLogProvider, pluginAssemblies);

            var configurationAction = _customContainerConfigurationAction ?? DefaultContainerConfiguration;
            configurationAction(configuration, builder, _configureContainerActions);

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
