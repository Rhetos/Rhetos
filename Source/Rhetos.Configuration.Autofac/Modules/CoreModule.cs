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
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            AddCommon(builder);
            AddSecurity(builder);
            AddUtilities(builder);
            AddDsl(builder);

            base.Load(builder);
        }

        private void AddCommon(ContainerBuilder builder)
        {
            builder.RegisterType<InstalledPackages>().As<IInstalledPackages>().SingleInstance();
            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            builder.RegisterType<NLogProvider>().As<ILogProvider>().InstancePerLifetimeScope();
        }

        private void AddSecurity(ContainerBuilder builder)
        {
            builder.RegisterType<WindowsSecurity>().As<IWindowsSecurity>().SingleInstance();
            builder.RegisterType<AuthorizationManager>().As<IAuthorizationManager>().InstancePerLifetimeScope();

            // Default user authentication and authorization components. Custom plugins may override it by registering their own interface implementations.
            builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope().PreserveExistingDefaults();
            builder.RegisterType<NullAuthorizationProvider>().As<IAuthorizationProvider>().PreserveExistingDefaults();

            // Cannot use FindAndRegisterPlugins on IUserInfo because each type should be manually registered with InstancePerLifetimeScope.
            Plugins.FindAndRegisterPlugins<IAuthorizationProvider>(builder);
            Plugins.FindAndRegisterPlugins<IClaimProvider>(builder);
        }

        private void AddUtilities(ContainerBuilder builder)
        {
            builder.RegisterType<XmlUtility>().SingleInstance();
            builder.RegisterType<Rhetos.Utilities.Configuration>().As<Rhetos.Utilities.IConfiguration>().SingleInstance();
            Plugins.FindAndRegisterPlugins<ILocalizer>(builder);
            builder.RegisterType<NoLocalizer>().As<ILocalizer>().SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<GeneratedFilesCache>().SingleInstance();

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
        }

        private void AddDsl(ContainerBuilder builder)
        {
            builder.RegisterType<DslContainer>();
            Plugins.FindAndRegisterPlugins<IDslModelIndex>(builder);
            builder.RegisterType<DslModelIndexByType>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelIndexByReference>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelFile>().As<IDslModel>().SingleInstance();
        }
    }
}
