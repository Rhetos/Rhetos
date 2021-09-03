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
using Rhetos.Host.AspNet;
using Rhetos.Host.AspNet.Dashboard;
using Rhetos.Host.AspNet.Dashboard.RhetosDashboardSnippets;
using Rhetos.Utilities;

namespace Rhetos
{
    /// <summary>
    /// Used for adding Rhetos-specific services to <see cref="IServiceCollection"/>.
    /// </summary>
    public static class HostAspNetRhetosServiceCollectionBuilderExtensions
    {
        /// <summary>
        /// Provides user info from the ASP.NET application to the Rhetos components.
        /// </summary>
        /// <remarks>
        /// It reads IHttpContextAccessor.HttpContext.User.Identity.Name and Identity.IsAuthenticated,
        /// and maps it to Rhetos <see cref="IUserInfo"/>.
        /// </remarks>
        public static RhetosServiceCollectionBuilder AddAspNetCoreIdentityUser(this RhetosServiceCollectionBuilder rhetosServiceCollectionBuilder)
        {
            rhetosServiceCollectionBuilder.Services.AddHttpContextAccessor();

            // not using TryAdd, allows subsequent calls to override previous ones
            rhetosServiceCollectionBuilder.Services.AddScoped<IUserInfo, RhetosAspNetCoreIdentityUser>();
            return rhetosServiceCollectionBuilder;
        }

        /// <summary>
        /// Adds the required services for Rhetos dashboard controller.
        /// </summary>
        public static RhetosServiceCollectionBuilder AddDashboard(this RhetosServiceCollectionBuilder rhetosBuilder)
        {
            rhetosBuilder.Services
                .AddControllersWithViews()
                .AddApplicationPart(typeof(RhetosDashboardController).Assembly);

            rhetosBuilder.AddDashboardSnippet<ServerStatusSnippet>();
            rhetosBuilder.AddDashboardSnippet<InstalledPackagesSnippet>();

            return rhetosBuilder;
        }

        public static RhetosServiceCollectionBuilder AddDashboardSnippet<T>(this RhetosServiceCollectionBuilder rhetosBuilder) where T : class, IDashboardSnippet
        {
            rhetosBuilder.Services.AddScoped<IDashboardSnippet, T>();
            return rhetosBuilder;
        }
    }
}
