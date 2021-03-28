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

            rhetosBuilder.AddDashboardSnippet(typeof(ServerStatusSnippet),"Server Status", 99);
            rhetosBuilder.AddDashboardSnippet(typeof(InstalledPackagesSnippet), "Installed Packages", 100);

            return rhetosBuilder;
        }

        public static RhetosAspNetServiceCollectionBuilder AddDashboardSnippet(this RhetosAspNetServiceCollectionBuilder rhetosBuilder,
            Type dashboardSnippetViewComponentType, string displayName = "", int order = 0)
        {
            rhetosBuilder.Services.Configure<DashboardOptions>(o =>
            {
                if (o.DashboardSnippets.Any(a => a.ViewComponentType == dashboardSnippetViewComponentType))
                    return;

                var snippetInfo = new DashboardSnippetInfo()
                {
                    DisplayName = displayName,
                    ViewComponentType = dashboardSnippetViewComponentType,
                    Order = order
                };

                o.DashboardSnippets.Add(snippetInfo);
            });

            return rhetosBuilder;
        }
    }
}
