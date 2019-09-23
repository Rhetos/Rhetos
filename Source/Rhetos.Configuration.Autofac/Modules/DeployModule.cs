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
        private readonly bool shortTransactions;

        public DeployModule(bool shortTransactions)
        {
            this.shortTransactions = shortTransactions;
        }

        protected override void Load(ContainerBuilder builder)
        {
            AddDatabaseGenerator(builder);
            AddDsl(builder);
            AddDom(builder);
            AddPersistence(builder);
            AddCompiler(builder);

            builder.RegisterType<ApplicationGenerator>();
            Plugins.FindAndRegisterPlugins<IGenerator>(builder);

            base.Load(builder);
        }

        private void AddDatabaseGenerator(ContainerBuilder builder)
        {
            builder.RegisterType<ConceptApplicationRepository>().As<IConceptApplicationRepository>();
            builder.RegisterType<DatabaseGenerator.DatabaseGenerator>().As<IDatabaseGenerator>();
            builder.RegisterType<DatabaseGenerator.ConceptDataMigrationExecuter>().As<IConceptDataMigrationExecuter>();
            builder.RegisterInstance(new DatabaseGeneratorOptions { ShortTransactions = shortTransactions });
            Plugins.FindAndRegisterPlugins<IConceptDatabaseDefinition>(builder);
            builder.RegisterType<NullImplementation>().As<IConceptDatabaseDefinition>();
            Plugins.FindAndRegisterPlugins<IConceptDataMigration>(builder, typeof(IConceptDataMigration<>));
            builder.RegisterType<DataMigrationScripts>();
            builder.RegisterType<DatabaseCleaner>();
        }

        // TODO: this is a misnomer, since CoreModule also has AddDsl method
        private void AddDsl(ContainerBuilder builder)
        {
            builder.RegisterType<DiskDslScriptLoader>().As<IDslScriptsProvider>().SingleInstance();
            builder.RegisterType<Tokenizer>().SingleInstance();
            builder.RegisterType<DslModelFile>().As<IDslModelFile>().SingleInstance();
            builder.RegisterType<DslParser>().As<IDslParser>();
            builder.RegisterType<MacroOrderRepository>().As<IMacroOrderRepository>();
            builder.RegisterType<ConceptMetadata>().SingleInstance();
            builder.RegisterType<InitializationConcept>().As<IConceptInfo>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            Plugins.FindAndRegisterPlugins<IConceptInfo>(builder);
            Plugins.FindAndRegisterPlugins<IConceptMacro>(builder, typeof(IConceptMacro<>));
            Plugins.FindAndRegisterPlugins<IConceptMetadataExtension>(builder);
        }

        private void AddDom(ContainerBuilder builder)
        {
            builder.RegisterType<DomGeneratorOptions>().SingleInstance();
            builder.RegisterType<DomGenerator>().As<IDomainObjectModel>().SingleInstance();
        }

        private void AddPersistence(ContainerBuilder builder)
        {
            builder.RegisterType<DataMigrationScriptsFromDisk>().As<IDataMigrationScriptsProvider>();
            builder.RegisterType<EntityFrameworkMappingGenerator>().As<IGenerator>();
            Plugins.FindAndRegisterPlugins<IConceptMapping>(builder, typeof(ConceptMapping<>));

        }

        private void AddCompiler(ContainerBuilder builder)
        {
            builder.RegisterType<CodeBuilder>().As<ICodeBuilder>();
            builder.RegisterType<CodeGenerator>().As<ICodeGenerator>();
            builder.RegisterType<AssemblyGenerator>().As<IAssemblyGenerator>();
            Plugins.FindAndRegisterPlugins<IConceptCodeGenerator>(builder);
        }
    }
}
