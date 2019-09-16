using Autofac;
using Rhetos.Deployment;
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
        /*
        public static ContainerBuilder AddRhetos(this ContainerBuilder builder, bool deploymentTime, bool deployDatabaseOnly)
        {
            builder.RegisterModule(new DeploymentModuleConfiguration(deploymentTime));
            builder.RegisterModule(new DomModuleConfiguration(deploymentTime));
            builder.RegisterModule(new PersistenceModuleConfiguration(deploymentTime));
            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            builder.RegisterModule(new SecurityModuleConfiguration());
            builder.RegisterModule(new UtilitiesModuleConfiguration());
            builder.RegisterModule(new DslModuleConfiguration(deploymentTime, deployDatabaseOnly));
            builder.RegisterModule(new CompilerConfiguration(deploymentTime));
            builder.RegisterModule(new LoggingConfiguration());
            builder.RegisterModule(new ProcessingModuleConfiguration(deploymentTime));
            builder.RegisterModule(new ExtensibilityModuleConfiguration()); // This is the last registration, so that the plugins can override core components.

            return builder;
        }*/
    }
}
