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
    public class DbUpdateModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // TOOD: Remove BuildOptions from DbUpdate.
            builder.Register(context => context.Resolve<IConfigurationProvider>().GetOptions<BuildOptions>()).SingleInstance().PreserveExistingDefaults();

            var pluginRegistration = builder.GetPluginRegistration();

            AddDatabaseGenerator(builder, pluginRegistration);
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
            builder.RegisterType<DatabaseCleaner>();

            // Updating database from database model:

            builder.RegisterType<DatabaseModelFile>();
            builder.Register(context => context.Resolve<DatabaseModelFile>().Load()).As<DatabaseModel>().SingleInstance();
            builder.RegisterType<ConceptApplicationRepository>().As<IConceptApplicationRepository>();
            builder.Register(context => new DatabaseGeneratorOptions { ShortTransactions = context.Resolve<BuildOptions>().ShortTransactions }).SingleInstance();
            builder.RegisterType<DatabaseGenerator.DatabaseGenerator>().As<IDatabaseGenerator>();

            // Executing data migration from SQL scripts:

            builder.RegisterType<DataMigrationScriptsFile>();
            builder.Register(context => context.Resolve<DataMigrationScriptsFile>().Load()).As<DataMigrationScripts>().SingleInstance();
            builder.RegisterType<DataMigrationScriptsExecuter>();

            // Executing data migration from plugins:

            builder.RegisterType<ConceptDataMigrationExecuter>().As<IConceptDataMigrationExecuter>();
        }

        private void AddDom(ContainerBuilder builder)
        {
            builder.Register(context => new DomGeneratorOptions() { Debug = context.ResolveOptional<BuildOptions>()?.Debug ?? false }).SingleInstance();
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
