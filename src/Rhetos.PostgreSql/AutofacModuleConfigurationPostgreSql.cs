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
using Npgsql;
using Rhetos.PostgreSql.SqlResources;
using Rhetos.SqlResources;
using Rhetos.Utilities;
using System.ComponentModel.Composition;
using System.Data.Common;

namespace Rhetos.PostgreSql
{
    [Export(typeof(Module))]
    public class AutofacModuleConfigurationPostgreSql : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            ExecutionStage stage = builder.GetRhetosExecutionStage();

            builder.RegisterType<PostgreSqlUtility>().As<ISqlUtility>().SingleInstance();

            if (stage.IsBuildTime)
            {
                builder.RegisterType<CommonConceptsBuildSqlResourcesPlugin>().As<ISqlResourcesPlugin>().SingleInstance();
            }

            if (stage.IsDatabaseUpdate)
            {
                builder.RegisterType<CoreDbUpdateSqlResourcesPlugin>().As<ISqlResourcesPlugin>().SingleInstance();
            }

            if (stage.IsDatabaseUpdate || stage.IsRuntime)
            {
                builder.RegisterType<PostgreSqlExecuter>().As<ISqlExecuter>().InstancePerLifetimeScope();
            }

            // SqlClientFactory.Instance is null at this point (Autofac Module Load), is expected to get initialized later.
            builder.Register<DbProviderFactory>(context => NpgsqlFactory.Instance).SingleInstance();

            if (stage.IsBuildTime)
            {
                builder.RegisterType<PostgreSqlResourcesPlugin>().As<ISqlResourcesPlugin>().SingleInstance();
            }

            base.Load(builder);
        }
    }
}