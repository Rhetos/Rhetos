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
using Rhetos.Security;
using Rhetos.Utilities;
using System.Collections.Generic;

namespace Rhetos
{
    public static class RhetosContainerBuilderExtensions
    {
        public static RhetosContainerBuilder AddApplicationInitialization(this RhetosContainerBuilder builder)
        {
            var deployOptions = builder.GetInitializationContext().ConfigurationProvider.GetOptions<DeployOptions>();
            builder.RegisterInstance(deployOptions).PreserveExistingDefaults();
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