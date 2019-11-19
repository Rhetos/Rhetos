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
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var pluginRegistration = builder.GetPluginRegistration();

            AddCommon(builder);
            AddSecurity(builder, pluginRegistration);
            AddUtilities(builder, pluginRegistration);
            AddDsl(builder, pluginRegistration);

            base.Load(builder);
        }

        private void AddCommon(ContainerBuilder builder)
        {
            builder.Register(context => context.Resolve<IConfigurationProvider>().GetOptions<RhetosAppOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<InstalledPackages>().As<IInstalledPackages>().SingleInstance();
            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            builder.RegisterType<NLogProvider>().As<ILogProvider>().InstancePerLifetimeScope();
        }

        private void AddSecurity(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<WindowsSecurity>().As<IWindowsSecurity>().SingleInstance();
            builder.RegisterType<AuthorizationManager>().As<IAuthorizationManager>().InstancePerLifetimeScope();

            // Default user authentication and authorization components. Custom plugins may override it by registering their own interface implementations.
            builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope().PreserveExistingDefaults();
            builder.RegisterType<NullAuthorizationProvider>().As<IAuthorizationProvider>().PreserveExistingDefaults();

            // Cannot use FindAndRegisterPlugins on IUserInfo because each type should be manually registered with InstancePerLifetimeScope.
            pluginRegistration.FindAndRegisterPlugins<IAuthorizationProvider>();
            pluginRegistration.FindAndRegisterPlugins<IClaimProvider>();
        }

        private void AddUtilities(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
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

            if (string.IsNullOrEmpty(SqlUtility.DatabaseLanguage))
                throw new FrameworkException("SqlUtility has not been initialized. LegacyUtilities.Initialize() should be called at application startup.");

            var sqlImplementation = sqlImplementations.GetValue(SqlUtility.DatabaseLanguage,
                () => "Unsupported database language '" + SqlUtility.DatabaseLanguage
                    + "'. Supported languages are: " + string.Join(", ", sqlImplementations.Keys) + ".");

            builder.RegisterType(sqlImplementation.SqlExecuter).As<ISqlExecuter>().InstancePerLifetimeScope();
            builder.RegisterType(sqlImplementation.SqlUtility).As<ISqlUtility>().InstancePerLifetimeScope();
            builder.RegisterType<SqlTransactionBatches>().InstancePerLifetimeScope();
        }

        private void AddDsl(ContainerBuilder builder, ContainerBuilderPluginRegistration pluginRegistration)
        {
            builder.RegisterType<DslContainer>();
            pluginRegistration.FindAndRegisterPlugins<IDslModelIndex>();
            builder.RegisterType<DslModelIndexByType>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelIndexByReference>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelFile>().As<IDslModel>().SingleInstance();
        }
    }
}
