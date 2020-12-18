using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autofac;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos
{
    public interface IRhetosHostBuilder
    {
        IRhetosHostBuilder UseBuilderLogProvider(ILogProvider logProvider);
        IRhetosHostBuilder ConfigureConfiguration(Action<IConfigurationBuilder> configureAction);
        IRhetosHostBuilder ConfigureContainer(Action<ContainerBuilder> configureAction);
        IRhetosHostBuilder UseCustomContainerConfiguration(Action<IConfiguration, ContainerBuilder, List<Action<ContainerBuilder>>> containerConfigurationAction);
        IRhetosHostBuilder AddAssemblyProbingDirectories(params string[] assemblyProbingDirectories);
        RhetosHost Build();
    }
}
