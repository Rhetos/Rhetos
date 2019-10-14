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
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder AddRhetosRuntime(this ContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new RuntimeModule());
            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static ContainerBuilder AddRhetosDeployment(this ContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new DeployModule());

            // TODO SS: is this the correct way to do this?
            builder.RegisterType<DslModel>();
            builder.Register(a => a.Resolve<DeployOptions>().DatabaseOnly ? a.Resolve<IDslModelFile>() as IDslModel : a.Resolve<DslModel>() as IDslModel).SingleInstance();

            builder.RegisterModule(new ExtensibilityModule());
            return builder;
        }

        public static ContainerBuilder AddConfiguration(this ContainerBuilder builder, IConfigurationProvider configurationProvider)
        {
            builder.RegisterInstance(configurationProvider);
            return builder;
        }

        public static ContainerBuilder AddOptions<T>(this ContainerBuilder builder, IConfigurationProvider configurationProvider) where T : class
        {
            builder.RegisterInstance(configurationProvider.GetOptions<T>());
            return builder;
        }

        public static ContainerBuilder AddOptions<T>(this ContainerBuilder builder) where T : class
        {
            builder.Register(a => a.Resolve<IConfigurationProvider>().GetOptions<T>()).SingleInstance();
            return builder;
        }
    }
}
