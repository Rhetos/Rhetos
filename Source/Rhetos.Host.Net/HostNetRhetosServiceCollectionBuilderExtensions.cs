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
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Rhetos;
using Rhetos.Host.Net;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HostNetRhetosServiceCollectionBuilderExtensions
    {
        /// <summary>
        /// Configures Rhetos logging so that it uses the host application's logging (<see cref="Logging.ILogger"/> implementation registered in the <see cref="IServiceCollection"/>).
        /// </summary>
        public static RhetosServiceCollectionBuilder AddHostLogging(this RhetosServiceCollectionBuilder rhetosServiceCollectionBuilder)
        {
            rhetosServiceCollectionBuilder.ConfigureRhetosHost(ConfigureLogProvider);

            return rhetosServiceCollectionBuilder;
        }

        /// <summary>
        /// Configures Rhetos localization so that it uses the <see cref="IStringLocalizerFactory"/> implementation registered in the <see cref="IServiceCollection"/>
        /// </summary>
        public static RhetosServiceCollectionBuilder AddHostLocalization(this RhetosServiceCollectionBuilder rhetosServiceCollectionBuilder, Action<HostLocalizerOptions> configureOptions = null)
        {
            rhetosServiceCollectionBuilder.Services.AddOptions();
            if (configureOptions != null)
            {
                rhetosServiceCollectionBuilder.Services.Configure(configureOptions);
            }
            rhetosServiceCollectionBuilder.ConfigureRhetosHost(ConfigureLocalizer);
            rhetosServiceCollectionBuilder.Services.AddSingleton<HostLocalizer>();

            return rhetosServiceCollectionBuilder;
        }

        private static void ConfigureLogProvider(IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder)
        {
            rhetosHostBuilder.ConfigureContainer(builder => builder.RegisterInstance<ILogProvider>(new HostLogProvider(serviceProvider.GetRequiredService<ILoggerProvider>())));
        }

        private static void ConfigureLocalizer(IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder)
        {
            rhetosHostBuilder.ConfigureContainer(builder => builder.RegisterInstance<ILocalizer>(serviceProvider.GetRequiredService<HostLocalizer>()));
        }
    }
}
