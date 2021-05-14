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

namespace Rhetos
{
    public static class RhetosHostBuilderExtensions
    {
        /// <summary>
        /// It configures the <see cref="IRhetosHostBuilder"/> to use the <see cref="ILoggerProvider"/> that is registered in the <see cref="IServiceProvider"/>.
        /// If no <see cref="ILoggerProvider"/> was found it will not change the <see cref="IRhetosHostBuilder"/> configuration.
        /// </summary>
        public static IRhetosHostBuilder UseBuilderLogProviderFromHost(this IRhetosHostBuilder rhetosHostBuilder, IServiceProvider serviceProvider)
        {
            var loggerProvider = serviceProvider.GetService<ILoggerProvider>();
            if(loggerProvider != null)
                rhetosHostBuilder.UseBuilderLogProvider(new HostLogProvider(loggerProvider));
            return rhetosHostBuilder;        
        }
    }
}
