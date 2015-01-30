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
using Rhetos.Extensibility;
using System.Diagnostics.Contracts;

namespace Rhetos.Configuration.Autofac
{
    public class ExtensibilityModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Contract.Requires(builder != null);

            builder.RegisterGeneric(typeof(PluginsMetadataCache<>)).SingleInstance();
            builder.RegisterGeneric(typeof(PluginsContainer<>)).As(typeof(IPluginsContainer<>)).InstancePerLifetimeScope();

            builder.RegisterType<GeneratorPlugins>();
            Rhetos.Extensibility.PluginsUtility.RegisterPlugins<IGenerator>(builder);

            builder.RegisterType<ServerInitializationPlugins>();
            Rhetos.Extensibility.PluginsUtility.RegisterPlugins<IServerInitializer>(builder);

            PluginsUtility.RegisterPluginModules(builder);

            base.Load(builder);
        }
    }
}
