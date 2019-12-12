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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac.Modules
{
    /// <summary>
    /// This module handles code generation and code compilation. 
    /// Requires refactoring to separate the two.
    /// </summary>
    public class DeployModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var pluginRegistration = builder.GetPluginRegistration();

            AddDatabaseGenerator(builder, pluginRegistration);
            AddDslDeployment(builder, pluginRegistration);
            AddDom(builder);
            AddPersistence(builder, pluginRegistration);
            AddCompiler(builder, pluginRegistration);

            builder.RegisterType<ApplicationGenerator>();
            builder.RegisterType<DatabaseDeployment>();
            pluginRegistration.FindAndRegisterPlugins<IGenerator>();

            base.Load(builder);
        }

        private void AddDatabaseGenerator(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<DatabaseModelGenerator>().As<IGenerator>();
            builder.RegisterType<DatabaseModelFile>().As<IDatabaseModelFile>();
            builder.Register(context => context.Resolve<IDatabaseModelFile>().Load()).As<DatabaseModel>().SingleInstance();
            builder.RegisterType<ConceptApplicationRepository>().As<IConceptApplicationRepository>();
            builder.RegisterType<DatabaseGenerator.DatabaseGenerator>().As<IDatabaseGenerator>();
            builder.RegisterType<ConceptDataMigrationExecuter>().As<IConceptDataMigrationExecuter>();
            builder.Register(context => new DatabaseGeneratorOptions { ShortTransactions = context.Resolve<DeployOptions>().ShortTransactions }).SingleInstance();
            pluginRegistration.FindAndRegisterPlugins<IConceptDatabaseDefinition>();
            builder.RegisterType<NullImplementation>().As<IConceptDatabaseDefinition>();
            pluginRegistration.FindAndRegisterPlugins<IConceptDataMigration>(typeof(IConceptDataMigration<>));
            builder.RegisterType<DataMigrationScripts>();
            builder.RegisterType<DatabaseCleaner>();
            builder.RegisterType<ConceptDataMigrationGenerator>().As<IGenerator>();
        }

        private void AddDslDeployment(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
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
            builder.Register(context => new DomGeneratorOptions() { Debug = context.ResolveOptional<DeployOptions>()?.Debug ?? false }).SingleInstance();
            builder.RegisterType<DomGenerator>().As<IDomainObjectModel>().SingleInstance();
        }

        private void AddPersistence(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<DataMigrationScriptsFromDisk>().As<IDataMigrationScriptsProvider>();
            builder.RegisterType<EntityFrameworkMappingGenerator>().As<IGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IConceptMapping>(typeof(ConceptMapping<>));

        }

        private void AddCompiler(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<CodeBuilder>().As<ICodeBuilder>();
            builder.RegisterType<CodeGenerator>().As<ICodeGenerator>();
            builder.RegisterType<AssemblyGenerator>().As<IAssemblyGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IConceptCodeGenerator>();
        }
    }
}
