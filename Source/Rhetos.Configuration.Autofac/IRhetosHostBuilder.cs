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
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;

namespace Rhetos
{
    public interface IRhetosHostBuilder
    {
        /// <summary>
        /// The specified <paramref name="logProvider"/> will be used during Rhetos host initialization.
        /// If not specified, <see cref="IRhetosHostBuilder"/> will use default logging implementation
        /// <see cref="LoggingDefaults.DefaultLogProvider"/>.
        /// </summary>
        /// <remarks>
        /// This method configures logging only for host initialization.
        /// To set a custom <see cref="ILogProvider"/> during application run-time, call <see cref="ConfigureContainer"/>.
        /// </remarks>
        IRhetosHostBuilder UseBuilderLogProvider(ILogProvider logProvider);

        /// <summary>
        /// Adds or overrides configuration settings for Rhetos application.
        /// </summary>
        IRhetosHostBuilder ConfigureConfiguration(Action<IConfigurationBuilder> configureAction);

        /// <summary>
        /// Configures services and plugins in the dependency injection container.
        /// </summary>
        /// <remarks>
        /// Implementation of <paramref name="configureAction"/> delegate may use Rhetos logging and plugins management components,
        /// available from <see cref="ContainerBuilder"/> by extension methods in <see cref="ContainerBuilderExtensions"/> class.
        /// </remarks>
        IRhetosHostBuilder ConfigureContainer(Action<ContainerBuilder> configureAction);

        /// <summary>
        /// Overrides default Rhetos components registrations
        /// (<see cref="ContainerBuilderExtensions.AddRhetosRuntime(ContainerBuilder)"/>
        /// and <see cref="ContainerBuilderExtensions.AddRhetosPluginModules(ContainerBuilder)"/>).
        /// </summary>
        IRhetosHostBuilder OverrideContainerConfiguration(Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> containerConfigurationAction);

        /// <summary>
        /// Sets base directory for the application's configuration.
        /// </summary>
        /// <remarks>
        /// This method is intended for external applications that reference the generated Rhetos application
        /// (CLI utilities, for example) to specify the application's root folder.
        /// In most cases, the root folder is automatically configured by <see cref="RhetosHost.FindBuilder(string)"/>,
        /// and there is no need to call this method directly from the custom application code.
        /// <para>
        /// This method solves the issue of relative paths in application's configuration, by making sure
        /// that the base folder that is used for loading configuration by external utility applications,
        /// is the same as the one used by the main host application.
        /// AppDomain.CurrentDomain.BaseDirectory is used by default.
        /// </para>
        /// </remarks>
        IRhetosHostBuilder UseRootFolder(string rootFolder);

        /// <summary>
        /// The assemblies will be scanned for plugins while building the
        /// dependency injection container.
        /// Plugin is a class marked with <see cref="ExportAttribute"/>,
        /// and optionally additional metadata in <see cref="ExportMetadataAttribute"/>.
        /// </summary>
        IRhetosHostBuilder AddPluginAssemblies(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// The types will be scanned for plugins while building the
        /// dependency injection container.
        /// Plugin is a class marked with <see cref="ExportAttribute"/>,
        /// and optionally additional metadata in <see cref="ExportMetadataAttribute"/>.
        /// </summary>
        IRhetosHostBuilder AddPluginTypes(IEnumerable<Type> types);

        /// <summary>
        /// Create the <see cref="RhetosHost"/> instance, that serves as a wrapper around Rhetos system configuration and 
        /// dependency injections container.
        /// </summary>
        /// <remarks>
        /// Internally, it builds the configuration and the dependency injections container,
        /// as previously configured by calls to <see cref="ConfigureConfiguration(Action{IConfigurationBuilder})"/>
        /// and <see cref="ConfigureContainer(Action{ContainerBuilder})"/> methods.
        /// </remarks>
        RhetosHost Build();
    }
}
