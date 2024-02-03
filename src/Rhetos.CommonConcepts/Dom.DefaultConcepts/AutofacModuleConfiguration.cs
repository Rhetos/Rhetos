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
using Rhetos.Dom.DefaultConcepts.Authorization;
using Rhetos.Security;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(Module))]
    public class AutofacModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GenericRepositoryParameters>().PropertiesAutowired();
            builder.RegisterType<GenericRepositories>();
            builder.RegisterGeneric(typeof(GenericRepository<>));
            builder.RegisterType<GenericFilterHelper>();

            // Permission loading is done with repositories that use the user's context (IUserInfo),
            // but it is expected that the user's context will not affect the loading of permissions.
            builder.RegisterType<AuthorizationDataLoader>().InstancePerLifetimeScope();
			// AuthorizationDataCache must not be "single instance" because it could result with leaving an open SQL connection when reading permissions.
            builder.RegisterType<AuthorizationDataCache>().As<AuthorizationDataCache>().As<IAuthorizationData>().InstancePerLifetimeScope();
            builder.RegisterType<RequestAndGlobalCache>().InstancePerLifetimeScope();
            builder.RegisterType<PrincipalWriter>().InstancePerLifetimeScope();
            builder.RegisterType<CommonAuthorizationProvider>().As<IAuthorizationProvider>().InstancePerLifetimeScope();

            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<CommonConceptsOptions>()).SingleInstance(); // Build-time configuration. It is disabled at run-time by AutofacModuleConfiguration in the generated code.
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<CommonConceptsDatabaseSettings>()).SingleInstance(); // Reading configuration at build-time. It is overridden at run-time by AutofacModuleConfiguration in the generated code, to return values from build-time that are hardcoded in the generated application.

            base.Load(builder);
        }
    }
}