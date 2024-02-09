using Microsoft.Extensions.Hosting;
using Rhetos.Security;
using Rhetos.Utilities;
using Rhetos;
using Autofac;

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
host.Run();
