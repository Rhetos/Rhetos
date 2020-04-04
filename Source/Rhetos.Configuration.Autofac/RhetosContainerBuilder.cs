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
using System;
using System.Collections.Generic;

namespace Rhetos
{
    /// <summary>
    /// Container builder initialized with <see cref="IConfigurationProvider"/>.
    /// It makes the <see cref="ILogProvider"/> and <see cref="IPluginScanner"/> accessible during registration process and
    /// registers <see cref="IConfigurationProvider"/> to the container.
    /// </summary>
    public class RhetosContainerBuilder : ContainerBuilder
    {
        /// <summary>
        /// Initializes a container with specified <see cref="IConfigurationProvider"/>. 
        /// Registers <see cref="IConfigurationProvider"/> instance to newly created container.
        /// <see cref="ILogProvider"/> is not registered and is meant to be used during the lifetime of registration and container building process.
        /// <see cref="LegacyUtilities"/> will also be initialized with the given configuration.
        /// </summary>
        /// <param name="pluginAssemblies">List of assemblies (DLL file paths) that will be used for plugins search when using the <see cref="ContainerBuilderPluginRegistration"/></param>
        public RhetosContainerBuilder(IConfigurationProvider configurationProvider, ILogProvider logProvider, Func<IEnumerable<string>> pluginAssemblies)
        {
            this.RegisterInstance(configurationProvider);

            var pluginScanner = new PluginScanner(
                pluginAssemblies,
                PluginScanner.GetCacheFolder(configurationProvider),
                logProvider,
                configurationProvider.GetOptions<PluginScannerOptions>());

            // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
            this.Properties.Add(nameof(IPluginScanner), pluginScanner);
            this.Properties.Add(nameof(ILogProvider), logProvider);

            // this is a patch/mock to provide backward compatibility for all usages of old static classes
            LegacyUtilities.Initialize(configurationProvider);

            Plugins.Initialize(builder => builder.GetPluginRegistration());
        }
    }
}
