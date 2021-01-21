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
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Persistence;
using Rhetos.Processing;
using Rhetos.Security;
using Rhetos.Utilities;
using Rhetos.XmlSerialization;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class RuntimeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<RhetosAppEnvironment>()).SingleInstance();
            builder.RegisterType<InstalledPackagesProvider>();
            builder.Register(context => context.Resolve<InstalledPackagesProvider>().Load())
#pragma warning disable CS0618 // Registering obsolete IInstalledPackages for backward compatibility.
                .As<InstalledPackages>().As<IInstalledPackages>().SingleInstance();
#pragma warning restore CS0618
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<RhetosAppOptions>())
                .As<RhetosAppOptions>().As<IAssetsOptions>().SingleInstance();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<DatabaseSettings>()).SingleInstance();

            builder.RegisterType<DomLoader>().As<IDomainObjectModel>().SingleInstance();
            builder.RegisterType<PersistenceTransaction>().As<IPersistenceTransaction>().InstancePerLifetimeScope();
            builder.RegisterType<EfMappingViewsFileStore>().SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<EfMappingViewCacheFactory>().SingleInstance().PreserveExistingDefaults();
            builder.RegisterModule(new DatabaseRuntimeModule());

            var pluginRegistration = builder.GetPluginRegistration();
            AddDsl(builder, pluginRegistration);
            AddSecurity(builder, pluginRegistration);
            AddUtilities(builder, pluginRegistration);
            AddCommandsProcessing(builder, pluginRegistration);

            base.Load(builder);
        }

        private static void AddDsl(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<DslModelFile>().As<IDslModel>().SingleInstance();
            builder.RegisterType<ConceptMetadata>().SingleInstance();
            pluginRegistration.FindAndRegisterPlugins<IConceptMetadataExtension>();
        }

        private void AddSecurity(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<AppSecurityOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<AuthorizationManager>().As<IAuthorizationManager>().InstancePerLifetimeScope();

            // Default user authentication and authorization components. Custom plugins may override it by registering their own interface implementations.
            builder.RegisterType<NullAuthorizationProvider>().As<IAuthorizationProvider>().PreserveExistingDefaults();

            // Cannot use FindAndRegisterPlugins on IUserInfo because each type should be manually registered with InstancePerLifetimeScope.
            pluginRegistration.FindAndRegisterPlugins<IAuthorizationProvider>();
            pluginRegistration.FindAndRegisterPlugins<IClaimProvider>();
        }

        private void AddUtilities(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            pluginRegistration.FindAndRegisterPlugins<ILocalizer>();
            builder.RegisterType<NoLocalizer>().As<ILocalizer>().SingleInstance().PreserveExistingDefaults();
        }

        private static void AddCommandsProcessing(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<XmlDataTypeProvider>().As<IDataTypeProvider>().SingleInstance();
            builder.RegisterType<ProcessingEngine>().As<IProcessingEngine>();
            pluginRegistration.FindAndRegisterPlugins<ICommandData>();
            pluginRegistration.FindAndRegisterPlugins<ICommandImplementation>();
            pluginRegistration.FindAndRegisterPlugins<ICommandObserver>();
            pluginRegistration.FindAndRegisterPlugins<ICommandInfo>();
        }
    }
}
