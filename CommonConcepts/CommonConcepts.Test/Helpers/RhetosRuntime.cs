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
using CommonConcepts.Test.Helpers;
using Rhetos;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.ComponentModel.Composition;

namespace CommonConcepts.Test
{
    /// <summary>
    /// Provides basic runtime infrastructure for Rhetos framework: configuration settings and system components registration.
    /// It is used indirectly by <see cref="TestContainer"/> when creating Rhetos DI container.
    /// </summary>
    [Export(typeof(IRhetosRuntime))]
    public class RhetosRuntime : IRhetosRuntime
    {
        public IConfiguration BuildConfiguration(ILogProvider logProvider, string configurationFolder, Action<IConfigurationBuilder> addCustomConfiguration)
        {
            var configurationBuilder = new ConfigurationBuilder(logProvider);
            configurationBuilder.AddRhetosAppEnvironment(configurationFolder);
            addCustomConfiguration?.Invoke(configurationBuilder);
            return configurationBuilder.Build();
        }

        public IContainer BuildContainer(ILogProvider logProvider, IConfiguration configuration, Action<ContainerBuilder> registerCustomComponents)
        {
            var pluginAssemblies = AssemblyResolver.GetRuntimeAssemblies(configuration);
            var builder = new RhetosContainerBuilder(configuration, logProvider, pluginAssemblies);
            
            builder.AddRhetosRuntime();
            builder.AddPluginModules();

            registerCustomComponents?.Invoke(builder);

            return builder.Build();
        }
    }
}