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
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Rhetos
{
    public class RhetosHostBuilder : IRhetosHostBuilder
    {
        private ILogProvider _builderLogProvider = LoggingDefaults.DefaultLogProvider;
        private readonly List<Action<ContainerBuilder>> _configureContainerActions = new List<Action<ContainerBuilder>>();
        private readonly List<Action<IConfigurationBuilder>> _configureConfigurationActions = new List<Action<IConfigurationBuilder>>();
        private Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> _customContainerConfigurationAction;
        private readonly List<Assembly> _pluginAssemblies = new List<Assembly>();
        private readonly List<Type> _pluginTypes = new List<Type>();
        private ILogger _buildLogger;
        private string _rootFolder;

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

        public IRhetosHostBuilder OverrideContainerConfiguration(Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> containerConfigurationAction)
        {
            _customContainerConfigurationAction = containerConfigurationAction;
            return this;
        }

        public IRhetosHostBuilder UseRootFolder(string rootFolder)
        {
            _rootFolder = rootFolder;
            return this;
        }

        public IRhetosHostBuilder AddPluginAssemblies(IEnumerable<Assembly> assemblies)
        {
            _pluginAssemblies.AddRange(assemblies);
            return this;
        }

        public IRhetosHostBuilder AddPluginTypes(IEnumerable<Type> types)
        {
            _pluginTypes.AddRange(types);
            return this;
        }

        public RhetosHost Build()
        {
            var restoreCurrentDirectory = Environment.CurrentDirectory;

            _buildLogger = _builderLogProvider.GetLogger(nameof(RhetosHost));
            try
            {
                if (!string.IsNullOrEmpty(_rootFolder))
                {
                    Directory.SetCurrentDirectory(_rootFolder);
                    _buildLogger.Info($"Using '{_rootFolder}' as root folder for {nameof(Build)} operation.");
                }
                else
                {
                    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                }

                var configuration = BuildConfiguration();
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
                Directory.SetCurrentDirectory(restoreCurrentDirectory);
            }
        }

        private IConfiguration BuildConfiguration()
        {
            var rhetosAppSettingsFilePath = Path.GetFullPath(RhetosAppEnvironment.ConfigurationFileName);
            if (!File.Exists(rhetosAppSettingsFilePath))
                throw new FrameworkException($"Unable to initialize RhetosHost. Rhetos app settings file '{rhetosAppSettingsFilePath}' not found.");

            _buildLogger.Info(() => $"Initializing Rhetos app from '{rhetosAppSettingsFilePath}'.");
            
            var configurationBuilder = new ConfigurationBuilder(_builderLogProvider)
                .AddJsonFile(rhetosAppSettingsFilePath)
                .AddJsonFile(RhetosAppEnvironment.LocalConfigurationFileName, optional: true)
                .AddKeyValue(ConfigurationProvider.GetKey((RhetosAppEnvironment o) => o.ApplicationRootFolder), Path.GetDirectoryName(rhetosAppSettingsFilePath));

            CsUtility.InvokeAll(configurationBuilder, _configureConfigurationActions);

            return configurationBuilder.Build();
        }

        private IContainer BuildContainer(IConfiguration configuration)
        {
            var pluginScanner = new RuntimePluginScanner(_pluginAssemblies, _pluginTypes, _builderLogProvider);
            var builder = new RhetosContainerBuilder(configuration, _builderLogProvider, pluginScanner);

            var configurationAction = _customContainerConfigurationAction ?? DefaultContainerConfiguration;
            configurationAction(configuration, builder, _configureContainerActions);

            return builder.Build();
        }

#pragma warning disable S1172 // Unused method parameters should be removed
        // Parameter "IConfiguration configuration" is not used here in default configuration,
        // but is allowed as an option for custom configuration implementations.
        private void DefaultContainerConfiguration(IConfiguration configuration, ContainerBuilder builder, List<Action<ContainerBuilder>> customActions)
        {
            builder.AddRhetosRuntime();
            builder.AddPluginModules();

            CsUtility.InvokeAll(builder, customActions);
        }
#pragma warning restore S1172
    }
}
