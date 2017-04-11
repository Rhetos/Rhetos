using Autofac;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos;

namespace RhetosWebApi
{
    public class DefaultAutofacConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Specific registrations and initialization:
            Plugins.SetInitializationLogging(new NLogProvider());
            //builder.RegisterType<RhetosService>().As<RhetosService>().As<IServerApplication>();
            //builder.RegisterType<Rhetos.Web.GlobalErrorHandler>();
            Plugins.FindAndRegisterPlugins<IService>(builder);
            Plugins.FindAndRegisterPlugins<IHomePageSnippet>(builder);

            // General registrations:
            builder.RegisterModule(new Rhetos.Configuration.Autofac.DefaultAutofacConfiguration(deploymentTime: false, deployDatabaseOnly: false));

            base.Load(builder);
        }
    }
}