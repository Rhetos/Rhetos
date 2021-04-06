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

using System;
using Autofac;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rhetos;
using Rhetos.Host.AspNet;
using Rhetos.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RhetosAspNetCoreServiceCollectionExtensions
    {
        public static RhetosAspNetServiceCollectionBuilder AddRhetos(
            this IServiceCollection serviceCollection,
            Action<IRhetosHostBuilder> configureRhetosHost = null,
            Action<IServiceProvider, IRhetosHostBuilder> configureRhetosHostIntegration = null)
        {
            serviceCollection.AddOptions();
            if (configureRhetosHost != null)
            {
                serviceCollection.Configure<RhetosHostBuilderOptions>(o => o.ConfigureActions.Add(configureRhetosHost));
            }

            serviceCollection.Configure<RhetosHostBuilderOptions>(o => o.ConfigureIntegrationActions.Add(ConfigureLogProvider));
            if (configureRhetosHostIntegration != null)
            {
                serviceCollection.Configure<RhetosHostBuilderOptions>(o => o.ConfigureIntegrationActions.Add(configureRhetosHostIntegration));
            }

            serviceCollection.TryAddSingleton(serviceProvider => CreateRhetosHost(serviceProvider));
            serviceCollection.TryAddScoped<RhetosScopeServiceProvider>();
            serviceCollection.TryAddScoped(typeof(IRhetosComponent<>), typeof(RhetosComponent<>));

            return new RhetosAspNetServiceCollectionBuilder(serviceCollection);
        }

        private static RhetosHost CreateRhetosHost(IServiceProvider serviceProvider)
        {
            var rhetosHostBuilder = new RhetosHostBuilder();

            var options = serviceProvider.GetRequiredService<IOptions<RhetosHostBuilderOptions>>();
            
            foreach (var rhetosHostBuilderConfigureAction in options.Value.ConfigureActions)
            {
                rhetosHostBuilderConfigureAction(rhetosHostBuilder);
            }

            foreach (var rhetosHostBuilderConfigureIntegrationAction in options.Value.ConfigureIntegrationActions)
            {
                rhetosHostBuilderConfigureIntegrationAction(serviceProvider, rhetosHostBuilder);
            }

            return rhetosHostBuilder.Build();
        }

        private static void ConfigureLogProvider(IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder)
        {
            rhetosHostBuilder.ConfigureContainer(builder => builder.RegisterInstance<ILogProvider>(new HostLogProvider(serviceProvider.GetService<ILoggerProvider>())));
        }
    }
}
