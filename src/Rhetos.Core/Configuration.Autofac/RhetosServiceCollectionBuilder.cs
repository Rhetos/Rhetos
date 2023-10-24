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

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rhetos
{
    /// <summary>
    /// Allows configuring host application's services (with <see cref="Services"/>)
    /// and Rhetos dependency injection components and configuration (with <see cref="ConfigureRhetosHost"/>).
    /// </summary>
    /// <remarks>
    /// This is just a helper for accessing <see cref="IServiceCollection"/>, that promotes more readable code
    /// for configuring Rhetos-related services by bundling Rhetos plugins configuration in a single block of code.
    /// </remarks>
    public class RhetosServiceCollectionBuilder
    {
        public IServiceCollection Services { get; }

        public RhetosServiceCollectionBuilder(IServiceCollection serviceCollection)
        {
            Services = serviceCollection;
        }

        /// <summary>
        /// Configures Rhetos dependency injection container and Rhetos configuration settings.
        /// The provided delegate will be executed to configure a singleton <see cref="RhetosHost"/>,
        /// therefore is should not be used to resolve <i>scoped</i> services from <see cref="IServiceProvider"/>.
        /// </summary>
        public RhetosServiceCollectionBuilder ConfigureRhetosHost(Action<IServiceProvider, IRhetosHostBuilder> configureRhetosHost)
        {
            Services.Configure<RhetosHostBuilderOptions>(o => o.ConfigureActions.Add(configureRhetosHost));
            return this;
        }
    }
}
