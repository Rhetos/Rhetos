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
using System.IO;
using Autofac;
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using Rhetos.Deployment;
using Rhetos.Dsl;
using Rhetos.Security;

namespace DeployPackages
{
    public class AutofacConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Specific registrations:
            builder.RegisterModule(new DatabaseGeneratorModuleConfiguration());
            builder.RegisterType<DataMigration>();
            builder.RegisterType<DatabaseCleaner>();

            // General registrations:
            string rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
            builder.RegisterModule(new Rhetos.Configuration.Autofac.DefaultAutofacConfiguration(rootPath, generate: true));

            // Specific registrations override:
            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();

            base.Load(builder);
        }
    }
}