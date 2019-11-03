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
        public ContainerBuilderPluginRegistration PluginRegistration { get; }

        /// <summary>
        /// Initializes a container with specified InitializationContext. Registers ConfigurationProvider and RhetosAppEnvironment instances to newly created container.
        /// LogProvider is not registered and is meant to be used during the lifetime of registration and container building process.
        /// LegacyUtilities will also be initialized with the given configuration.
        /// </summary>
        public ContextContainerBuilder(InitializationContext initializationContext, IPluginScanner pluginScanner = null)
        {
            this.InitializationContext = initializationContext;
            this.RegisterInstance(initializationContext.ConfigurationProvider);
            this.RegisterInstance(initializationContext.RhetosAppEnvironment);

            pluginScanner = pluginScanner ?? new MefPluginScanner();

            this.PluginRegistration = new ContainerBuilderPluginRegistration(this, initializationContext.LogProvider, pluginScanner);
            Plugins.Initialize(builder => GetPluginRegistration(builder));

            // this is a patch/mock to provide backward compatibility for all usages of old static classes
            LegacyUtilities.Initialize(initializationContext.ConfigurationProvider);

            // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
            this.Properties.Add(nameof(Rhetos.Configuration.Autofac.InitializationContext), InitializationContext);
            this.Properties.Add(nameof(ContainerBuilderPluginRegistration), PluginRegistration);
        }

        /// <summary>
        /// Initializes a container with new InitializationContext created from specified parameters.
        /// </summary>
        public ContextContainerBuilder(IConfigurationProvider configurationProvider, ILogProvider logProvider)
            : this(new InitializationContext(configurationProvider, logProvider))
        {
        }

        /// <summary>
        /// Resolves InitializationContext object from Autofac ContainerBuilder.
        /// Use when ContextContainerBuilder instance is not available, as in Module.Load().
        /// </summary>
        public static InitializationContext GetInitializationContext(ContainerBuilder builder)
        {
            if (builder is ContextContainerBuilder contextContainerBuilder)
                return contextContainerBuilder.InitializationContext;

            var key = nameof(Rhetos.Configuration.Autofac.InitializationContext);
            if (builder.Properties.TryGetValue(key, out var initializationContext) && (initializationContext is InitializationContext))
                return initializationContext as InitializationContext;

            throw new FrameworkException($"ContainerBuilder does not contain an entry for {nameof(Rhetos.Configuration.Autofac.InitializationContext)}. " +
                $"This container was probably not created as {nameof(ContextContainerBuilder)}.");
        }

        /// <summary>
        /// Resolves ContainerBuilderPluginRegistration object from Autofac ContainerBuilder.
        /// Use when ContextContainerBuilder instance is not available, as in Module.Load(). 
        /// </summary>
        public static ContainerBuilderPluginRegistration GetPluginRegistration(ContainerBuilder builder)
        {
            if (builder is ContextContainerBuilder contextContainerBuilder)
                return contextContainerBuilder.PluginRegistration;

            // if this is not ContextContainerBuilder then we need to create a new instance of PluginRegistration initialized with this new builder, based on stored properties
            var key = nameof(ContainerBuilderPluginRegistration);
            if (builder.Properties.TryGetValue(key, out var pluginRegistration) && (pluginRegistration is ContainerBuilderPluginRegistration))
            {
                var initializationContext = GetInitializationContext(builder);
                return new ContainerBuilderPluginRegistration(
                    builder, 
                    initializationContext.LogProvider, 
                    (pluginRegistration as ContainerBuilderPluginRegistration).PluginScanner);
            }

            throw new FrameworkException($"ContainerBuilder does not contain an entry for {nameof(ContainerBuilderPluginRegistration)}. " +
                $"This container was probably not created as {nameof(ContextContainerBuilder)}.");
        }
    }
}
