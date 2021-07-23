using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapRhetosDashboard(this IEndpointRouteBuilder routeBuilder, string route = "/rhetos/dashboard")
        {
            routeBuilder.MapControllerRoute("RhetosDashboard", route, new {controller = "RhetosDashboard", action = "Dashboard"});
            return routeBuilder;
        }
    }
}
