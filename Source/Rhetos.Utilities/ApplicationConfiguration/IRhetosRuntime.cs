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

namespace Rhetos
{
    /// <summary>
    /// Host application with Rhetos runtime components should have one implementation of this interface.
    /// The implementation provides basic runtime infrastructure: configuration settings and system components.
    /// </summary>
    public interface IRhetosRuntime
    {
        /// <summary>
        /// Initializes configuration settings for Rhetos application.
        /// </summary>
        /// <param name="logProvider">
        /// The specified <paramref name="logProvider"/> will be used while building the configuration.
        /// </param>
        /// <param name="addCustomConfiguration">
        /// Add or override configuration settings.
        /// This is commonly used by utilities and tests that need to customize the host application's settings.
        /// </param>
        /// <param name="configurationFolder">
        /// Folder where application's configuration files are located.
        /// </param>
        IConfiguration BuildConfiguration(ILogProvider logProvider, string configurationFolder, Action<IConfigurationBuilder> addCustomConfiguration);

        /// <summary>
        /// Creates Dependency Injections container for Rhetos application.
        /// </summary>
        /// <param name="logProvider">
        /// The specified <paramref name="logProvider"/> will be used while building the dependency injection container.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// </param>
        IContainer BuildContainer(ILogProvider logProvider, IConfiguration configuration, Action<ContainerBuilder> registerCustomComponents);
    }
}
