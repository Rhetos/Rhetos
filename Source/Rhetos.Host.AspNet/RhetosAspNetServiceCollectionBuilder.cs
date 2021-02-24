using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rhetos.Utilities;

namespace Rhetos.Extensions.AspNetCore
{
    public class RhetosAspNetServiceCollectionBuilder
    {
        public IServiceCollection Services { get; }
        public RhetosHost RhetosHost { get; }
        public RhetosAspNetServiceCollectionBuilder(IServiceCollection serviceCollection, RhetosHost rhetosHost)
        {
            Services = serviceCollection;
            RhetosHost = rhetosHost;
        }

        public RhetosAspNetServiceCollectionBuilder UseAspNetCoreIdentityUser()
        {
            Services.AddHttpContextAccessor();
            Services.AddScoped<IUserInfo, RhetosAspNetCoreIdentityUser>();
            return this;
        }
    }
}
