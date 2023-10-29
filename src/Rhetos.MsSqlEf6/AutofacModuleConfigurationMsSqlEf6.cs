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
using Rhetos.Persistence;
using Rhetos.Utilities;
using System.Collections.Generic;
using System;
using System.ComponentModel.Composition;

namespace Rhetos.MsSqlEf6
{
    [Export(typeof(Module))]
    public class AutofacModuleConfigurationMsSqlEf6 : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Components for all contexts (rhetos build, dbupdate, application runtime):
            builder.RegisterType<MsSqlUtility>().As<ISqlUtility>().InstancePerLifetimeScope();

            // Run-time and DbUpdate:
            builder.RegisterType<MsSqlExecuter>().As<ISqlExecuter>().InstancePerLifetimeScope();

            const string dbLanguage = "MsSql";
            if (SqlUtility.DatabaseLanguage != dbLanguage)
                throw new FrameworkException($"Unsupported database language '{SqlUtility.DatabaseLanguage}'. {GetType().Assembly.GetName()} expects database language {dbLanguage}.");

            base.Load(builder);
        }
    }
}