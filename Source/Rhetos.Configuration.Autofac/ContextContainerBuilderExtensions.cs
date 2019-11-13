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
using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac
{
    public static class ContextContainerBuilderExtensions
    {
        public static ContextContainerBuilder AddRhetosRuntime(this ContextContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new RuntimeModule());
            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static ContextContainerBuilder AddRhetosDeployment(this ContextContainerBuilder builder)
        {
            var deployOptions = builder.InitializationContext.ConfigurationProvider.GetOptions<DeployOptions>();
            builder.RegisterInstance(deployOptions).PreserveExistingDefaults();
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DeployModule());

            // Overriding IDslModel registration from core (DslModelFile), unless deploying DatabaseOnly.
            builder.RegisterType<DslModel>();
            builder.Register(a => a.Resolve<DeployOptions>().DatabaseOnly ? (IDslModel)a.Resolve<IDslModelFile>() : a.Resolve<DslModel>()).SingleInstance();

            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static ContextContainerBuilder AddConfiguration(this ContextContainerBuilder builder, IConfigurationProvider configurationProvider)
        {
            builder.RegisterInstance(configurationProvider);
            return builder;
        }

        public static ContextContainerBuilder AddOptions<T>(this ContextContainerBuilder builder, IConfigurationProvider configurationProvider) where T : class
        {
            builder.RegisterInstance(configurationProvider.GetOptions<T>());
            return builder;
        }

        public static ContextContainerBuilder AddOptions<T>(this ContextContainerBuilder builder) where T : class
        {
            builder.Register(a => a.Resolve<IConfigurationProvider>().GetOptions<T>()).SingleInstance();
            return builder;
        }
    }
}
