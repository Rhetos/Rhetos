/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Autofac;
using Rhetos.Compiler;
using Rhetos.DatabaseGenerator;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Persistence;
using Rhetos.Utilities;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class BuildModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => context.Resolve<IConfigurationProvider>().GetOptions<BuildOptions>()).SingleInstance().PreserveExistingDefaults();

            var pluginRegistration = builder.GetPluginRegistration();

            AddDatabaseGenerator(builder, pluginRegistration);
            AddDsl(builder, pluginRegistration);
            AddDom(builder);
            AddPersistence(builder, pluginRegistration);
            AddCompiler(builder, pluginRegistration);
            builder.RegisterType<PluginScannerCache>().As<IGenerator>();
            builder.RegisterType<InstalledPackagesGenerator>().As<IGenerator>();

            builder.RegisterType<ApplicationGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IGenerator>();

            base.Load(builder);
        }

        private void AddDatabaseGenerator(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            // Generating database model:

            builder.RegisterType<DatabaseModelDependencies>();
            builder.RegisterType<DatabaseModelBuilder>();
            builder.RegisterType<DatabaseModelFile>();
            builder.RegisterType<DatabaseModelGenerator>().As<IGenerator>();

            // Generating data migration from SQL scripts:

            pluginRegistration.FindAndRegisterPlugins<IConceptDataMigration>(typeof(IConceptDataMigration<>));
            builder.RegisterType<DataMigrationScriptsFile>();
            builder.RegisterType<DataMigrationScriptsGenerator>().As<IGenerator>();

            // Generating data migration from plugins:

            pluginRegistration.FindAndRegisterPlugins<IConceptDatabaseDefinition>();
            builder.RegisterType<NullImplementation>().As<IConceptDatabaseDefinition>();
            builder.RegisterType<ConceptDataMigrationGenerator>().As<IGenerator>();
        }

        private void AddDsl(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<DslModel>().As<IDslModel>().SingleInstance();
            builder.RegisterType<DiskDslScriptLoader>().As<IDslScriptsProvider>().SingleInstance();
            builder.RegisterType<Tokenizer>().SingleInstance();
            builder.RegisterType<DslModelFile>().As<IDslModelFile>().SingleInstance();
            builder.RegisterType<DslParser>().As<IDslParser>();
            builder.RegisterType<MacroOrderRepository>().As<IMacroOrderRepository>();
            builder.RegisterType<ConceptMetadata>().SingleInstance();
            builder.RegisterType<InitializationConcept>().As<IConceptInfo>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            pluginRegistration.FindAndRegisterPlugins<IConceptInfo>();
            pluginRegistration.FindAndRegisterPlugins<IConceptMacro>(typeof(IConceptMacro<>));
            pluginRegistration.FindAndRegisterPlugins<IConceptMetadataExtension>();
        }

        private void AddDom(ContainerBuilder builder)
        {
            builder.RegisterType<DomGenerator>().As<IGenerator>();
        }

        private void AddPersistence(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<EntityFrameworkMappingGenerator>().As<IGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IConceptMapping>(typeof(ConceptMapping<>));

        }

        private void AddCompiler(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<CodeBuilder>().As<ICodeBuilder>();
            builder.RegisterType<Compiler.CodeGenerator>().As<ICodeGenerator>();
            builder.RegisterType<AssemblyGenerator>().As<IAssemblyGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IConceptCodeGenerator>();
        }
    }
}
