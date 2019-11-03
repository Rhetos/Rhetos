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
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Persistence;
using Rhetos.Processing;
using Rhetos.XmlSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class RuntimeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var pluginRegistration = builder.GetPluginRegistration();

            builder.RegisterType<DomLoader>().As<IDomainObjectModel>().SingleInstance();
            builder.RegisterType<PersistenceTransaction>().As<IPersistenceTransaction>().InstancePerLifetimeScope();

            // Processing as group?
            builder.RegisterType<XmlDataTypeProvider>().As<IDataTypeProvider>().SingleInstance();
            builder.RegisterType<ProcessingEngine>().As<IProcessingEngine>();
            pluginRegistration.FindAndRegisterPlugins<ICommandData>();
            pluginRegistration.FindAndRegisterPlugins<ICommandImplementation>();
            pluginRegistration.FindAndRegisterPlugins<ICommandObserver>();
            pluginRegistration.FindAndRegisterPlugins<ICommandInfo>();

            base.Load(builder);
        }
    }
}
