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
using System.Reflection;

namespace Rhetos
{
    /// <summary>
    /// Container builder initialized with <see cref="IConfiguration"/>.
    /// It makes the <see cref="ILogProvider"/> and <see cref="IPluginScanner"/> accessible during registration process and
    /// registers <see cref="IConfiguration"/> to the container.
    /// </summary>
    public class RhetosContainerBuilder : ContainerBuilder
    {
        private RhetosContainerBuilder(IConfiguration configuration, ILogProvider logProvider, IPluginScanner pluginScanner)
        {
            this.RegisterInstance(configuration);

            // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
            this.Properties.Add(nameof(IPluginScanner), pluginScanner);
            this.Properties.Add(nameof(ILogProvider), logProvider);

            // this is a patch/mock to provide backward compatibility for all usages of old static classes
            LegacyUtilities.Initialize(configuration);
        }

        /// <summary>
        /// Initializes a container with specified <see cref="IConfiguration"/>. 
        /// Registers <see cref="IConfiguration"/> instance to newly created container.
        /// Registers <see cref="PluginInfoContainer"/> instance to newly created container.
        /// <see cref="ILogProvider"/> is not registered and is meant to be used during the lifetime of registration and container building process.
        /// <see cref="LegacyUtilities"/> will also be initialized with the given configuration.
        /// This <see cref="RhetosContainerBuilder"/> instance is used during build time when all the specified assembly needs to be loaded.
        /// </summary>
        /// <param name="pluginAssemblies">List of assemblies (DLL file paths) that will be used for plugins search when using the <see cref="ContainerBuilderPluginRegistration"/></param>
        public static RhetosContainerBuilder CreateBuildTimeContainerBuilder(IConfiguration configuration, ILogProvider logProvider, IEnumerable<string> pluginAssemblies)
        {
            var pluginScanner = new PluginScanner(
                pluginAssemblies,
                PluginScanner.GetCacheFolder(configuration),
                logProvider,
                configuration.GetOptions<PluginScannerOptions>());

            var containerBuilder =  new RhetosContainerBuilder(configuration, logProvider, pluginScanner);
            containerBuilder.Register(context => new PluginInfoContainer(pluginScanner.FindAllPlugins()));
            return containerBuilder;
        }

        /// <summary>
        /// Initializes a container with specified <see cref="IConfiguration"/>. 
        /// Registers <see cref="IConfiguration"/> instance to newly created container.
        /// <see cref="ILogProvider"/> is not registered and is meant to be used during the lifetime of registration and container building process.
        /// <see cref="LegacyUtilities"/> will also be initialized with the given configuration.
        /// </summary>
        /// <param name="assemblies">List of assemblies that will be used for plugins search when using the <see cref="ContainerBuilderPluginRegistration"/></param>
        /// /// <param name="types">List of types that will be used for plugins search when using the <see cref="ContainerBuilderPluginRegistration"/></param>
        public static RhetosContainerBuilder CreateRunTimeContainerBuilder(IConfiguration configuration, ILogProvider logProvider, IEnumerable<Assembly> assemblies, IEnumerable<Type> types)
        {
            var pluginScanner = new RuntimePluginScanner(assemblies, types, logProvider);
            return new RhetosContainerBuilder(configuration, logProvider, pluginScanner);
        }
    }
}
