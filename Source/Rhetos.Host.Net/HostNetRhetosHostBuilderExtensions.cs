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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using Rhetos.Host.Net;
using Rhetos.Utilities;

namespace Rhetos
{
    public static class HostNetRhetosHostBuilderExtensions
    {
        /// <summary>
        /// It configures the <see cref="IRhetosHostBuilder"/> to use the <see cref="ILoggerProvider"/> from host application
        /// (from <see cref="IServiceProvider"/>) during Rhetos host initialization.
        /// </summary>
        /// <remarks>
        /// This method configures logging only for Rhetos host initialization.
        /// To set the host application log provider for application run-time, use <see cref="HostNetRhetosServiceCollectionBuilderExtensions.AddHostLogging"/>
        /// If no <see cref="ILoggerProvider"/> is registered in the host application, this method call will be ignored and
        /// <see cref="IRhetosHostBuilder"/> will use <see cref="LoggingDefaults.DefaultLogProvider"/> by default.
        /// </remarks>
        public static IRhetosHostBuilder UseBuilderLogProviderFromHost(this IRhetosHostBuilder rhetosHostBuilder, IServiceProvider serviceProvider)
        {
            var loggerProvider = serviceProvider.GetService<ILoggerProvider>();
            if(loggerProvider != null)
                rhetosHostBuilder.UseBuilderLogProvider(new HostLogProvider(loggerProvider));
            return rhetosHostBuilder;
        }
    }
}
