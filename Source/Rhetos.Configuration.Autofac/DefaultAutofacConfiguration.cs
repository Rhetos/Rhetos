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
using Rhetos.Utilities;
using Rhetos.Deployment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Configuration.Autofac
{
    public class DefaultAutofacConfiguration : Module
    {
        private readonly bool _deploymentTime;
        private readonly bool _deployDatabaseOnly;

        public DefaultAutofacConfiguration(bool deploymentTime, bool deployDatabaseOnly)
        {
            _deploymentTime = deploymentTime;
            _deployDatabaseOnly = deployDatabaseOnly;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule(new DeploymentModuleConfiguration());
            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            builder.RegisterModule(new SecurityModuleConfiguration());
            builder.RegisterModule(new UtilitiesModuleConfiguration());
            builder.RegisterModule(new DslModuleConfiguration(_deploymentTime, _deployDatabaseOnly));
            builder.RegisterModule(new LoggingConfiguration());

            if (_deploymentTime)
            {
                builder.RegisterModule(new RhetosDeployTimeModule());
            }
            else
            {
                builder.RegisterModule(new RhetosRuntimeModule());
            }

            builder.RegisterModule(new ExtensibilityModuleConfiguration()); // This is the last registration, so that the plugins can override core components.

            base.Load(builder);
        }
    }
}