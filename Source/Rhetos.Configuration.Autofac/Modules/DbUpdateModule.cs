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
using Rhetos.DatabaseGenerator;
using Rhetos.Deployment;
using Rhetos.Utilities;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class DbUpdateModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<RhetosAppEnvironment>()).SingleInstance();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<RhetosAppOptions>())
                .As<RhetosAppOptions>().As<IAssetsOptions>().SingleInstance();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<DbUpdateOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<DatabaseSettings>()).SingleInstance();

            builder.RegisterType<DatabaseDeployment>();
            builder.RegisterType<DatabaseCleaner>();

            // Updating database from database model:

            builder.RegisterModule(new DatabaseRuntimeModule());
            builder.RegisterType<DatabaseModelFile>();
            builder.Register(context => context.Resolve<DatabaseModelFile>().Load()).As<DatabaseModel>().SingleInstance();
            builder.RegisterType<ConceptApplicationRepository>().As<IConceptApplicationRepository>();
            builder.RegisterType<DatabaseGenerator.DatabaseAnalysis>();
            builder.RegisterType<DatabaseGenerator.DatabaseGenerator>().As<IDatabaseGenerator>();

            // Executing data migration from SQL scripts:

            builder.RegisterType<DataMigrationScriptsFile>().As<IDataMigrationScriptsFile>();
            builder.Register(context => context.Resolve<IDataMigrationScriptsFile>().Load()).As<DataMigrationScripts>().SingleInstance();
            builder.RegisterType<DataMigrationScriptsExecuter>();

            // Executing data migration from plugins:

            builder.RegisterType<ConceptDataMigrationExecuter>().As<IConceptDataMigrationExecuter>();

            base.Load(builder);
        }
    }
}
