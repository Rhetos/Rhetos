using Autofac;
using Rhetos.Compiler;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac
{
    public class RhetosDeployTimeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DataMigrationScriptsFromDisk>().As<IDataMigrationScriptsProvider>();

            builder.RegisterType<DomGeneratorOptions>().SingleInstance();
            builder.RegisterType<DomGenerator>().As<IDomainObjectModel>().SingleInstance();

            builder.RegisterType<EntityFrameworkMappingGenerator>().As<IGenerator>();
            Plugins.FindAndRegisterPlugins<IConceptMapping>(builder, typeof(ConceptMapping<>));

            // Compiler as group?
            builder.RegisterType<CodeBuilder>().As<ICodeBuilder>();
            builder.RegisterType<CodeGenerator>().As<ICodeGenerator>();
            builder.RegisterType<AssemblyGenerator>().As<IAssemblyGenerator>();
            Plugins.FindAndRegisterPlugins<IConceptCodeGenerator>(builder);

            base.Load(builder);
        }
    }
}
