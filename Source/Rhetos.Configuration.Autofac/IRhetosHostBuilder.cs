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

namespace Rhetos
{
    public interface IRhetosHostBuilder
    {
        IRhetosHostBuilder UseBuilderLogProvider(ILogProvider logProvider);

        IRhetosHostBuilder ConfigureConfiguration(Action<IConfigurationBuilder> configureAction);

        /// <summary>
        /// Configures services and plugins in the dependency injection container.
        /// </summary>
        IRhetosHostBuilder ConfigureContainer(Action<ContainerBuilder> configureAction);

        IRhetosHostBuilder UseCustomContainerConfiguration(Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> containerConfigurationAction);

        /// <summary>
        /// This method is intended for external applications that reference the generated Rhetos application
        /// (CLI utilities, for example) to specify the Rhetos app root folder.
        /// </summary>
        /// <remarks>
        /// In most cases, the root folder is automatically configured, and there is no need to call this
        /// method directly from the custom application code.
        /// </remarks>
        IRhetosHostBuilder UseRootFolder(string rootFolder);

        /// <summary>
        /// Create the <see cref="RhetosHost"/> instance that was previously configured by other <see cref="IRhetosHostBuilder"/> methods.
        /// </summary>
        RhetosHost Build();
    }
}
