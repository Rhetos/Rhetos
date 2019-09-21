using Autofac;
using Rhetos.Configuration.Autofac.Modules;
using Rhetos.Deployment;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder AddRhetosRuntime(this ContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new RuntimeModule());
            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static ContainerBuilder AddRhetosDeployment(this ContainerBuilder builder, bool shortTransactions, DeployType deployType)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DeployModule(shortTransactions));

            if (deployType == DeployType.DeployFull)
                builder.RegisterType<DslModel>().As<IDslModel>().SingleInstance();

            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }
    }
}
