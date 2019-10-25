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
using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac
{
    /// <summary>
    /// Container builder initialized with InitializationContext.
    /// It makes the InitializationContext accessible during registration process and
    /// registers all members of the context to container
    /// </summary>
    public class ContextContainerBuilder : ContainerBuilder
    {
        public InitializationContext InitializationContext { get; }

        /// <summary>
        /// Initializes a container with specified InitializationContext. Registers ConfigurationProvider and RhetosAppEnvironment instances to newly created container.
        /// LogProvider is not registered and is meant to be used during the lifetime of registration and container building process.
        /// LegacyUtilities will also be initialized with the given configuration.
        /// </summary>
        public ContextContainerBuilder(InitializationContext initializationContext)
        {
            this.InitializationContext = initializationContext;
            this.RegisterInstance(initializationContext.ConfigurationProvider);
            this.RegisterInstance(initializationContext.RhetosAppEnvironment);

            // this is a patch/mock to provide backward compatibility for all usages of old static classes
            LegacyUtilities.Initialize(initializationContext.ConfigurationProvider);
            Plugins.SetInitializationLogging(initializationContext.LogProvider);
        }

        /// <summary>
        /// Initializes a container with new InitializationContext created from specified parameters.
        /// </summary>
        public ContextContainerBuilder(IConfigurationProvider configurationProvider, ILogProvider logProvider)
            : this(new InitializationContext(configurationProvider, logProvider))
        {
        }
    }
}
