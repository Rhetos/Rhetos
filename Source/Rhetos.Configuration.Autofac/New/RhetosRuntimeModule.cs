using Autofac;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Persistence;
using Rhetos.Processing;
using Rhetos.XmlSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac
{
    public class RhetosRuntimeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DomLoader>().As<IDomainObjectModel>().SingleInstance();
            builder.RegisterType<PersistenceTransaction>().As<IPersistenceTransaction>().InstancePerLifetimeScope();

            // Processing as group?
            builder.RegisterType<XmlDataTypeProvider>().As<IDataTypeProvider>().SingleInstance();
            builder.RegisterType<ProcessingEngine>().As<IProcessingEngine>();
            Plugins.FindAndRegisterPlugins<ICommandData>(builder);
            Plugins.FindAndRegisterPlugins<ICommandImplementation>(builder);
            Plugins.FindAndRegisterPlugins<ICommandObserver>(builder);
            Plugins.FindAndRegisterPlugins<ICommandInfo>(builder);

            base.Load(builder);
        }
    }
}
