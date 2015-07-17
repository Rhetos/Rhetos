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

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Autofac;
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using System.Configuration;
using System.IO;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Deployment;

namespace Rhetos.Configuration.Autofac
{
    public class DefaultAutofacConfiguration : Module
    {
        private readonly bool _deploymentTime;

        public DefaultAutofacConfiguration(bool deploymentTime)
        {
            _deploymentTime = deploymentTime;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<InstalledPackages>().As<IInstalledPackages>().SingleInstance();
            builder.RegisterModule(new DomModuleConfiguration(_deploymentTime));
            builder.RegisterModule(new PersistenceModuleConfiguration(_deploymentTime));
            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            builder.RegisterModule(new SecurityModuleConfiguration());
            builder.RegisterModule(new UtilitiesModuleConfiguration());
            builder.RegisterModule(new DslModuleConfiguration(_deploymentTime));
            builder.RegisterModule(new CompilerConfiguration(_deploymentTime));
            builder.RegisterModule(new LoggingConfiguration());
            builder.RegisterModule(new ProcessingModuleConfiguration(_deploymentTime));
            builder.RegisterModule(new ExtensibilityModuleConfiguration()); // This is the last registration, so that the plugins can override core components.

            base.Load(builder);
        }
    }
}