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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Rhetos;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RhetosCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required to run <see cref="RhetosHost"/> to the specified <see cref="IServiceCollection"/>,
        /// and allows scoped Rhetos components to be resolved within scope of HTTP request as <see cref="IRhetosComponent{T}"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="RhetosServiceCollectionBuilder"/> that can be used to add additional Rhetos-specific services to <see cref="IServiceCollection"/>.
        /// </returns>
        public static RhetosServiceCollectionBuilder AddRhetosHost(
            this IServiceCollection serviceCollection,
            Action<IServiceProvider, IRhetosHostBuilder> configureRhetosHost = null)
        {
            serviceCollection.AddOptions();
            if (configureRhetosHost != null)
            {
                serviceCollection.Configure<RhetosHostBuilderOptions>(o => o.ConfigureActions.Add(configureRhetosHost));
            }

            serviceCollection.TryAddSingleton(serviceProvider => CreateRhetosHost(serviceProvider));
            serviceCollection.TryAddScoped<RhetosScopeServiceProvider>();
            serviceCollection.TryAddScoped(typeof(IRhetosComponent<>), typeof(RhetosComponent<>));

            return new RhetosServiceCollectionBuilder(serviceCollection);
        }

        private static RhetosHost CreateRhetosHost(IServiceProvider serviceProvider)
        {
            var rhetosHostBuilder = new RhetosHostBuilder();

            var options = serviceProvider.GetRequiredService<IOptions<RhetosHostBuilderOptions>>();

            foreach (var rhetosHostBuilderConfigureAction in options.Value.ConfigureActions)
            {
                rhetosHostBuilderConfigureAction(serviceProvider, rhetosHostBuilder);
            }

            return rhetosHostBuilder.Build();
        }
    }
}
