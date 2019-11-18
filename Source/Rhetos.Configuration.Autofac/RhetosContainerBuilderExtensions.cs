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
using Rhetos.Configuration.Autofac.Modules;
using Rhetos.Deployment;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos
{
    public static class RhetosContainerBuilderExtensions
    {
        public static RhetosContainerBuilder AddRhetosRuntime(this RhetosContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new RuntimeModule());
            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static RhetosContainerBuilder AddRhetosDeployment(this RhetosContainerBuilder builder)
        {
            var deployOptions = builder.GetInitializationContext().ConfigurationProvider.GetOptions<DeployOptions>();
            builder.RegisterInstance(deployOptions).PreserveExistingDefaults();
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DeployModule());

            // Overriding IDslModel registration from core (DslModelFile), unless deploying DatabaseOnly.
            builder.RegisterType<DslModel>();
            builder.Register(context => context.Resolve<DeployOptions>().DatabaseOnly ? (IDslModel)context.Resolve<IDslModelFile>() : context.Resolve<DslModel>()).SingleInstance();

            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }
    }
}
