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
using Rhetos.Configuration.Autofac.Modules;
using Rhetos.DatabaseGenerator;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Security;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos
{
    public static class RhetosContainerBuilderExtensions
    {
        public static RhetosContainerBuilder AddRhetosBuildModules(this RhetosContainerBuilder builder, List<InstalledPackage> installedPackages)
        {
            var buildOptions = builder.GetInitializationContext().ConfigurationProvider.GetOptions<BuildOptions>();
            builder.RegisterInstance(buildOptions).PreserveExistingDefaults();

            var pluginRegistration = builder.GetPluginRegistration();

            //Core modules
            builder.Register(context => context.Resolve<IConfigurationProvider>().GetOptions<RhetosAppOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.RegisterInstance(new SimpleInstalledPackages(installedPackages)).As<IInstalledPackages>();
            builder.RegisterType<NLogProvider>().As<ILogProvider>().InstancePerLifetimeScope();

            builder.RegisterType<WindowsSecurity>().As<IWindowsSecurity>().SingleInstance();
            builder.RegisterType<AuthorizationManager>().As<IAuthorizationManager>().InstancePerLifetimeScope();

            // Default user authentication and authorization components. Custom plugins may override it by registering their own interface implementations.
            builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope().PreserveExistingDefaults();
            builder.RegisterType<NullAuthorizationProvider>().As<IAuthorizationProvider>().PreserveExistingDefaults();

            // Cannot use FindAndRegisterPlugins on IUserInfo because each type should be manually registered with InstancePerLifetimeScope.
            pluginRegistration.FindAndRegisterPlugins<IAuthorizationProvider>();
            pluginRegistration.FindAndRegisterPlugins<IClaimProvider>();

            builder.RegisterType<XmlUtility>().SingleInstance();
            builder.RegisterType<FilesUtility>().SingleInstance();
            builder.RegisterType<Rhetos.Utilities.Configuration>().As<Rhetos.Utilities.IConfiguration>().SingleInstance();
            pluginRegistration.FindAndRegisterPlugins<ILocalizer>();
            builder.RegisterType<NoLocalizer>().As<ILocalizer>().SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<GeneratedFilesCache>().SingleInstance();

            var sqlImplementations = new[]
            {
                new { Dialect = "MsSql", SqlExecuter = typeof(MsSqlExecuter), SqlUtility = typeof(MsSqlUtility) },
                new { Dialect = "Oracle", SqlExecuter = typeof(OracleSqlExecuter), SqlUtility = typeof(OracleSqlUtility) },
            }.ToDictionary(imp => imp.Dialect);

            var sqlImplementation = sqlImplementations.GetValue(buildOptions.DatabaseLanguage,
                () => "Unsupported database language '" + buildOptions.DatabaseLanguage
                    + "'. Supported languages are: " + string.Join(", ", sqlImplementations.Keys) + ".");

            //We don't need this during the build
            //builder.RegisterType(sqlImplementation.SqlExecuter).As<ISqlExecuter>().InstancePerLifetimeScope();
            builder.RegisterType(sqlImplementation.SqlUtility).As<ISqlUtility>().InstancePerLifetimeScope();
            //We don't need this during the build
            //builder.RegisterType<SqlTransactionBatches>().InstancePerLifetimeScope();

            builder.RegisterType<DslContainer>();
            pluginRegistration.FindAndRegisterPlugins<IDslModelIndex>();
            builder.RegisterType<DslModelIndexByType>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelIndexByReference>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModel>().As<IDslModel>().SingleInstance();
            //-----------------------------------------------------------------------


            builder.RegisterType<DatabaseModelBuilder>().As<IDatabaseModel>();
            builder.RegisterType<ConceptApplicationRepository>().As<IConceptApplicationRepository>();
            builder.RegisterType<DatabaseGenerator.DatabaseGenerator>().As<IDatabaseGenerator>();
            //We don't need this during the build
            //builder.RegisterType<DatabaseGenerator.ConceptDataMigrationExecuter>().As<IConceptDataMigrationExecuter>();
            builder.Register(context => new DatabaseGeneratorOptions { ShortTransactions = context.Resolve<BuildOptions>().ShortTransactions }).SingleInstance();
            pluginRegistration.FindAndRegisterPlugins<IConceptDatabaseDefinition>();
            builder.RegisterType<NullImplementation>().As<IConceptDatabaseDefinition>();
            pluginRegistration.FindAndRegisterPlugins<IConceptDataMigration>(typeof(IConceptDataMigration<>));
            builder.RegisterType<DataMigrationScripts>();
            builder.RegisterType<DatabaseCleaner>();
            builder.RegisterType<ConceptDataMigrationGenerator>().As<IGenerator>();

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

            builder.Register(context => new DomGeneratorOptions() { Debug = context.ResolveOptional<BuildOptions>()?.Debug ?? false }).SingleInstance();
            //We don't need this during the build
            //builder.RegisterType<DomGenerator>().As<IDomainObjectModel>().SingleInstance();
            builder.RegisterType<DomGenerator>().As<IGenerator>().SingleInstance();

            builder.RegisterType<DataMigrationScriptsFromDisk>().As<IDataMigrationScriptsProvider>();
            builder.RegisterType<EntityFrameworkMappingGenerator>().As<IGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IConceptMapping>(typeof(ConceptMapping<>));

            builder.RegisterType<CodeBuilder>().As<ICodeBuilder>();
            builder.RegisterType<CodeGenerator>().As<ICodeGenerator>();
            builder.RegisterType<SimpleAssemblyGenerator>().As<IAssemblyGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IConceptCodeGenerator>();

            //I am not sure what to do with this
            //builder.RegisterType<ApplicationGenerator>();
            pluginRegistration.FindAndRegisterPlugins<IGenerator>();


            // Overriding IDslModel registration from core (DslModelFile), unless deploying DatabaseOnly.
            builder.RegisterType<DslModel>();

            builder.RegisterGeneric(typeof(PluginsMetadataCache<>)).SingleInstance();
            builder.RegisterGeneric(typeof(PluginsContainer<>)).As(typeof(IPluginsContainer<>)).InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(NamedPlugins<>)).As(typeof(INamedPlugins<>)).InstancePerLifetimeScope();
            pluginRegistration.FindAndRegisterModules();

            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();

            return builder;
        }

        public static RhetosContainerBuilder AddRhetosDbupdateModules(this RhetosContainerBuilder builder)
        {
            var pluginRegistration = builder.GetPluginRegistration();

            //Core modules
            builder.Register(context => context.Resolve<IConfigurationProvider>().GetOptions<RhetosAppOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<NLogProvider>().As<ILogProvider>().InstancePerLifetimeScope();

            builder.RegisterType<WindowsSecurity>().As<IWindowsSecurity>().SingleInstance();
            builder.RegisterType<AuthorizationManager>().As<IAuthorizationManager>().InstancePerLifetimeScope();

            // Default user authentication and authorization components. Custom plugins may override it by registering their own interface implementations.
            builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope().PreserveExistingDefaults();
            builder.RegisterType<NullAuthorizationProvider>().As<IAuthorizationProvider>().PreserveExistingDefaults();

            // Cannot use FindAndRegisterPlugins on IUserInfo because each type should be manually registered with InstancePerLifetimeScope.
            pluginRegistration.FindAndRegisterPlugins<IAuthorizationProvider>();
            pluginRegistration.FindAndRegisterPlugins<IClaimProvider>();

            builder.RegisterType<XmlUtility>().SingleInstance();
            builder.RegisterType<FilesUtility>().SingleInstance();
            builder.RegisterType<Rhetos.Utilities.Configuration>().As<Rhetos.Utilities.IConfiguration>().SingleInstance();
            pluginRegistration.FindAndRegisterPlugins<ILocalizer>();
            builder.RegisterType<NoLocalizer>().As<ILocalizer>().SingleInstance().PreserveExistingDefaults();

            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            var sqlImplementations = new[]
            {
                new { Dialect = "MsSql", SqlExecuter = typeof(MsSqlExecuter), SqlUtility = typeof(MsSqlUtility) },
                new { Dialect = "Oracle", SqlExecuter = typeof(OracleSqlExecuter), SqlUtility = typeof(OracleSqlUtility) },
            }.ToDictionary(imp => imp.Dialect);

            var sqlImplementation = sqlImplementations.GetValue(SqlUtility.DatabaseLanguage,
                () => "Unsupported database language '" + SqlUtility.DatabaseLanguage
                    + "'. Supported languages are: " + string.Join(", ", sqlImplementations.Keys) + ".");

            builder.RegisterType(sqlImplementation.SqlExecuter).As<ISqlExecuter>().InstancePerLifetimeScope();
            builder.RegisterType(sqlImplementation.SqlUtility).As<ISqlUtility>().InstancePerLifetimeScope();
            builder.RegisterType<SqlTransactionBatches>().InstancePerLifetimeScope();

            builder.RegisterType<DslContainer>();
            pluginRegistration.FindAndRegisterPlugins<IDslModelIndex>();
            builder.RegisterType<DslModelIndexByType>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelIndexByReference>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelFile>().As<IDslModel>().SingleInstance();

            builder.RegisterType<DatabaseModelBuilder>().As<IDatabaseModel>();
            builder.RegisterType<ConceptApplicationRepository>().As<IConceptApplicationRepository>();
            builder.RegisterType<DatabaseGenerator.DatabaseGenerator>().As<IDatabaseGenerator>();
            builder.RegisterType<DatabaseGenerator.ConceptDataMigrationExecuter>().As<IConceptDataMigrationExecuter>();
            builder.Register(context => new DatabaseGeneratorOptions { ShortTransactions = context.Resolve<BuildOptions>().ShortTransactions }).SingleInstance();
            pluginRegistration.FindAndRegisterPlugins<IConceptDatabaseDefinition>();
            builder.RegisterType<NullImplementation>().As<IConceptDatabaseDefinition>();
            pluginRegistration.FindAndRegisterPlugins<IConceptDataMigration>(typeof(IConceptDataMigration<>));
            builder.RegisterType<DataMigrationScripts>();
            builder.RegisterType<DatabaseCleaner>();
            builder.RegisterType<ConceptDataMigrationGenerator>().As<IGenerator>();

            builder.RegisterType<Tokenizer>().SingleInstance();
            builder.RegisterType<DslModelFile>().As<IDslModelFile>().SingleInstance();
            builder.RegisterType<ConceptMetadata>().SingleInstance();
            builder.RegisterType<InitializationConcept>().As<IConceptInfo>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            pluginRegistration.FindAndRegisterPlugins<IConceptInfo>();
            pluginRegistration.FindAndRegisterPlugins<IConceptMacro>(typeof(IConceptMacro<>));
            pluginRegistration.FindAndRegisterPlugins<IConceptMetadataExtension>();

            builder.Register(context => new DomGeneratorOptions() { Debug = context.ResolveOptional<BuildOptions>()?.Debug ?? false }).SingleInstance();
            builder.RegisterType<DomainObjectModelProvider>().As<IDomainObjectModel>().SingleInstance();

            pluginRegistration.FindAndRegisterPlugins<IConceptMapping>(typeof(ConceptMapping<>));

            builder.RegisterType<CodeBuilder>().As<ICodeBuilder>();
            builder.RegisterType<Dbupdate>();

            builder.RegisterGeneric(typeof(PluginsMetadataCache<>)).SingleInstance();
            builder.RegisterGeneric(typeof(PluginsContainer<>)).As(typeof(IPluginsContainer<>)).InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(NamedPlugins<>)).As(typeof(INamedPlugins<>)).InstancePerLifetimeScope();
            pluginRegistration.FindAndRegisterModules();


            return builder;
        }

        public static RhetosContainerBuilder AddRhetosRuntime(this RhetosContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new RuntimeModule());
            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static RhetosContainerBuilder AddRhetosDeployment(this RhetosContainerBuilder builder)
        {
            var buildOptions = builder.GetInitializationContext().ConfigurationProvider.GetOptions<BuildOptions>();
            builder.RegisterInstance(buildOptions).PreserveExistingDefaults();
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DeployModule());

            // Overriding IDslModel registration from core (DslModelFile), unless deploying DatabaseOnly.
            builder.RegisterType<DslModel>();
            builder.Register(context => context.Resolve<BuildOptions>().DatabaseOnly ? (IDslModel)context.Resolve<IDslModelFile>() : context.Resolve<DslModel>()).SingleInstance();

            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static RhetosContainerBuilder AddApplicationInitialization(this RhetosContainerBuilder builder)
        {
            builder.RegisterType<ApplicationInitialization>();
            builder.GetPluginRegistration().FindAndRegisterPlugins<IServerInitializer>();
            return builder;
        }

        /// <summary>
        /// No matter what authentication plugin is installed, deployment is run as the user that executed the process.
        /// </summary>
        public static RhetosContainerBuilder AddProcessUserOverride(this RhetosContainerBuilder builder)
        {
            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
            return builder;
        }
    }
}
