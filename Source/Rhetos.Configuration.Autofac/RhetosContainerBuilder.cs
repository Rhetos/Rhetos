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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos
{
    /// <summary>
    /// Container builder initialized with <see cref="InitializationContext"/>.
    /// It makes the <see cref="InitializationContext"/> and <see cref="IPluginScanner"/> accessible during registration process and
    /// registers <see cref="IConfigurationProvider"/> and <see cref="RhetosAppEnvironment"/> to the container.
    /// </summary>
    public class RhetosContainerBuilder : ContainerBuilder
    {
        /// <summary>
        /// Initializes a container with specified <see cref="InitializationContext"/>. 
        /// Registers <see cref="IConfigurationProvider"/> and <see cref="RhetosAppEnvironment"/> instances to newly created container.
        /// <see cref="ILogProvider"/> is not registered and is meant to be used during the lifetime of registration and container building process.
        /// <see cref="LegacyUtilities"/> will also be initialized with the given configuration.
        /// </summary>
        public RhetosContainerBuilder(InitializationContext initializationContext)
        {
            this.RegisterInstance(initializationContext.ConfigurationProvider);
            this.RegisterInstance(initializationContext.RhetosAppEnvironment);

            var pluginScanner = new MefPluginScanner(initializationContext.RhetosAppEnvironment, initializationContext.LogProvider);

            // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
            this.Properties.Add(nameof(InitializationContext), initializationContext);
            this.Properties.Add(nameof(IPluginScanner), pluginScanner);

            // this is a patch/mock to provide backward compatibility for all usages of old static classes
            LegacyUtilities.Initialize(initializationContext.ConfigurationProvider);

            Plugins.Initialize(builder => builder.GetPluginRegistration());
        }

        /// <summary>
        /// Initializes a container with new <see cref="InitializationContext"/> created from specified arguments. 
        /// Registers <see cref="IConfigurationProvider"/> and <see cref="RhetosAppEnvironment"/> instances to newly created container.
        /// <see cref="ILogProvider"/> is not registered and is meant to be used during the lifetime of registration and container building process.
        /// <see cref="LegacyUtilities"/> will also be initialized with the given configuration.
        /// </summary>
        public RhetosContainerBuilder(IConfigurationProvider configurationProvider, ILogProvider logProvider)
            : this(new InitializationContext(configurationProvider, logProvider))
        {
        }

    }
}
