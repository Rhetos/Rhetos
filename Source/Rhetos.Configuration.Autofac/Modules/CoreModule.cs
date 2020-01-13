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
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System.Linq;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            AddCommon(builder);
            AddUtilities(builder);

            base.Load(builder);
        }

        private void AddCommon(ContainerBuilder builder)
        {
            builder.Register(context => context.Resolve<IConfigurationProvider>().GetOptions<AssetsOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            builder.RegisterType<NLogProvider>().As<ILogProvider>().InstancePerLifetimeScope();
        }

        private void AddUtilities(ContainerBuilder builder)
        {
            builder.RegisterType<XmlUtility>().SingleInstance();
            builder.RegisterType<FilesUtility>().SingleInstance();
            builder.RegisterType<Utilities.Configuration>().As<IConfiguration>().SingleInstance();

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
    }
}
