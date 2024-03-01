using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rhetos;
using Rhetos.Security;
using Rhetos.Utilities;

IHostBuilder builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices(services =>
{
    services.AddRhetosHost((IServiceProvider serviceProvider, IRhetosHostBuilder rhetosHostBuilder) =>
        {
            rhetosHostBuilder
                .ConfigureRhetosAppDefaults()
                .ConfigureConfiguration(configurationBuilder => configurationBuilder
                    .AddJsonFile("local.settings.json"))
                .ConfigureContainer(builder =>
                {
                    builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                });
        });
});

IHost host = builder.Build();

var rhetosAppOptions = host.Services.GetService<IRhetosComponent<RhetosAppOptions>>()?.Value;
Console.WriteLine($"Running Rhetos app '{rhetosAppOptions?.RhetosAppAssemblyFileName}'.");
