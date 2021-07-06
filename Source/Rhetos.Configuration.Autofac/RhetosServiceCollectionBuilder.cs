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

using Rhetos;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Allows configuring host application's services (with <see cref="Services"/>)
    /// and Rhetos dependency injection components and configuration (with <see cref="ConfigureRhetosHost"/>).
    /// </summary>
    public class RhetosServiceCollectionBuilder
    {
        public IServiceCollection Services { get; }

        public RhetosServiceCollectionBuilder(IServiceCollection serviceCollection)
        {
            Services = serviceCollection;
        }

        public RhetosServiceCollectionBuilder ConfigureRhetosHost(Action<IServiceProvider, IRhetosHostBuilder> configureRhetosHost)
        {
            Services.Configure<RhetosHostBuilderOptions>(o => o.ConfigureActions.Add(configureRhetosHost));
            return this;
        }
    }
}
