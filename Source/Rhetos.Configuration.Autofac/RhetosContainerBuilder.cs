﻿/*
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

namespace Rhetos
{
    /// <summary>
    /// Dependency injection container builder, initialized with <see cref="IConfiguration"/>.
    /// It makes <see cref="ILogProvider"/> and <see cref="IPluginScanner"/> accessible during registration process
    /// (see <see cref="ContainerBuilderExtensions"/> method),
    /// and registers <see cref="IConfiguration"/> to the container.
    /// </summary>
    public static class RhetosContainerBuilder
    {
        /// <summary>
        /// Initializes a dependency injection container with specified <see cref="IConfiguration"/>. 
        /// Registers <see cref="IConfiguration"/> instance to newly created container.
        /// </summary>
        /// <remarks>
        /// <see cref="ILogProvider"/> is not registered to container and is meant to be used during the lifetime of registration and container building process.
        /// <see cref="LegacyUtilities"/> will also be initialized with the given configuration.
        /// </remarks>
        public static ContainerBuilder Create(IConfiguration configuration, ILogProvider logProvider, IPluginScanner pluginScanner)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterInstance(configuration).ExternallyOwned();

            // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
            containerBuilder.Properties.Add(nameof(IPluginScanner), pluginScanner);
            containerBuilder.Properties.Add(nameof(ILogProvider), logProvider);
            containerBuilder.Properties.Add(nameof(IConfiguration), configuration);

            // this is a patch/mock to provide backward compatibility for all usages of old static classes
            LegacyUtilities.Initialize(configuration);

            return containerBuilder;
        }
    }
}
