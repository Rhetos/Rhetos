/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Rhetos.Factory;
using System.Diagnostics.Contracts;
using Rhetos.Compiler;
using Rhetos.Extensibility;

namespace Rhetos.Configuration.Autofac
{
    public class FactoryModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Contract.Requires(builder != null);

            builder.RegisterType<DynamicProxyFactory>().As<IAspectFactory>().SingleInstance();
            builder.RegisterType<TypeFactory>().As<ITypeFactory>().SingleInstance();

            builder.RegisterType<TypeFactoryBuilder>().As<ITypeFactoryBuilder>();

            builder.RegisterGeneric(typeof(InterceptionFactory<>)).As(typeof(IInterceptionFactory<>)).SingleInstance();

            builder.RegisterType<PluginsInitializer>().As<IPluginsInitializer>().SingleInstance();

            base.Load(builder);
        }
    }
}
