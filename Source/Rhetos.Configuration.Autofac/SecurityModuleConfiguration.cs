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
using Rhetos.Extensibility;
using Rhetos.Security;
using Rhetos.Utilities;

namespace Rhetos.Configuration.Autofac
{
    public class SecurityModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WindowsSecurity>().As<IWindowsSecurity>().SingleInstance();
            builder.RegisterType<AuthorizationManager>().As<IAuthorizationManager>().InstancePerLifetimeScope();

            // Default user authentication and authorization components. Custom plugins may override it by registering their own interface implementations.
            builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope().PreserveExistingDefaults();
            //builder.RegisterType<WindowAuthUserInfo>().As<IUserInfo>().InstancePerLifetimeScope().PreserveExistingDefaults();
            builder.RegisterType<NullAuthorizationProvider>().As<IAuthorizationProvider>().PreserveExistingDefaults();

            // Cannot use FindAndRegisterPlugins on IUserInfo because each type should be manually registered with InstancePerLifetimeScope.
            Plugins.FindAndRegisterPlugins<IAuthorizationProvider>(builder);
            Plugins.FindAndRegisterPlugins<IClaimProvider>(builder);

            base.Load(builder);
        }
    }
}