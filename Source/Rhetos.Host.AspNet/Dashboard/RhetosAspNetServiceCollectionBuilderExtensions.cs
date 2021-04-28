using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rhetos.Host.AspNet;
using Rhetos.Host.AspNet.Dashboard;
using Rhetos.Host.AspNet.Dashboard.RhetosDashboardSnippets;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RhetosAspNetServiceCollectionBuilderExtensions
    {
        public static RhetosAspNetServiceCollectionBuilder AddDashboard(this RhetosAspNetServiceCollectionBuilder rhetosBuilder)
        {
            rhetosBuilder.Services
                .AddControllersWithViews()
                .AddApplicationPart(typeof(RhetosDashboardController).Assembly);

            rhetosBuilder.AddDashboardSnippet<ServerStatusSnippet>();
            rhetosBuilder.AddDashboardSnippet<InstalledPackagesSnippet>();

            return rhetosBuilder;
        }

        public static RhetosAspNetServiceCollectionBuilder AddDashboardSnippet<T>(this RhetosAspNetServiceCollectionBuilder rhetosBuilder) where T : class, IDashboardSnippet
        {
            rhetosBuilder.Services.AddScoped<IDashboardSnippet, T>();
            return rhetosBuilder;
        }
    }
}
