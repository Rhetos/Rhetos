﻿/*
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
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;

namespace Rhetos.Configuration.Autofac.Modules
{
    public class AppInitializeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NLogProvider>().As<ILogProvider>().SingleInstance();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<DbUpdateOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.GetRhetosPluginRegistration().FindAndRegisterPlugins<IServerInitializer>();
            base.Load(builder);
        }
    }
}
