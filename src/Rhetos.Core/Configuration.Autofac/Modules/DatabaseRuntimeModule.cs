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
using Rhetos.Persistence;
using Rhetos.Utilities;

namespace Rhetos.Configuration.Autofac.Modules
{
    /// <summary>
    /// Common components for application runtime and dbupdate command.
    /// </summary>
    internal sealed class DatabaseRuntimeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConnectionString>().SingleInstance().PreserveExistingDefaults();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<DatabaseOptions>()).SingleInstance().PreserveExistingDefaults();

            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<SqlTransactionBatchesOptions>()).InstancePerLifetimeScope();
            builder.RegisterType<SqlTransactionBatches>().As<ISqlTransactionBatches>().InstancePerLifetimeScope();

            builder.RegisterType<UnitOfWorkFactory>().As<UnitOfWorkFactory>().As<IUnitOfWorkFactory>().SingleInstance();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<PersistenceTransactionOptions>()).SingleInstance().PreserveExistingDefaults();
            // PersistenceTransactionOptions.UseDatabaseTransaction is disable on dbupdate,
            // which means that ISqlExecuter will not use transactions.
            // Transactions in dbupdate are often manually controlled by ISqlTransactionBatches.
            builder.RegisterType<PersistenceTransaction>().As<IPersistenceTransaction>().As<IUnitOfWork>()
                .InstancePerMatchingLifetimeScope(UnitOfWorkScope.ScopeName);

            base.Load(builder);
        }
    }
}
