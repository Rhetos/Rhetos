using System;
using Rhetos;
using Rhetos.Extensions.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RhetosAspNetCoreServiceCollectionExtensions
    {
        public static RhetosAspNetServiceCollectionBuilder AddRhetos(this IServiceCollection serviceCollection, Action<IRhetosHostBuilder> configureRhetosHost = null)
        {
            var rhetosHostBuilder = new RhetosHostBuilder();
            configureRhetosHost?.Invoke(rhetosHostBuilder);
            var rhetosHost = rhetosHostBuilder.Build();
                
            serviceCollection.AddSingleton(rhetosHost);
            serviceCollection.AddScoped<RhetosScopeServiceProvider>();
            serviceCollection.AddScoped(typeof(IRhetosComponent<>), typeof(RhetosComponent<>));

            return new RhetosAspNetServiceCollectionBuilder(serviceCollection, rhetosHost);
        }
    }
}
