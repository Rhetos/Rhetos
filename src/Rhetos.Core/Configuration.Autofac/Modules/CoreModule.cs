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
using Rhetos.DatabaseGenerator;
using Rhetos.Deployment;
using Rhetos.Extensibility;
using Rhetos.SqlResources;
using Rhetos.Utilities;

namespace Rhetos.Configuration.Autofac.Modules
{
    /// <summary>
    /// Common components for all contexts (rhetos build, dbupdate, application runtime).
    /// </summary>
    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DelayedLogProvider>().As<IDelayedLogProvider>().SingleInstance();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<LoggingOptions>()).SingleInstance().PreserveExistingDefaults();
            builder.RegisterType<XmlUtility>().SingleInstance();
            builder.RegisterType<FilesUtility>().SingleInstance();
            builder.Register(context => context.Resolve<IConfiguration>().GetOptions<DatabaseSettings>()).SingleInstance();
            builder.RegisterType<NoLocalizer>().As<ILocalizer>().SingleInstance();
            builder.RegisterGeneric(typeof(NoLocalizer<>)).As(typeof(ILocalizer<>)).SingleInstance();

            // Extensibility
            builder.RegisterSource<PluginMetadataRegistrationSource>();
            builder.RegisterGeneric(typeof(PluginsMetadataCache<>)).SingleInstance();
            builder.RegisterGeneric(typeof(PluginsContainer<>)).As(typeof(IPluginsContainer<>)).InstancePerLifetimeScope();
            builder.RegisterGeneric(typeof(NamedPlugins<>)).As(typeof(INamedPlugins<>)).InstancePerLifetimeScope();

            // SQL resources
            builder.RegisterType<SqlResourcesProvider>().As<ISqlResources>().SingleInstance();
            builder.GetRhetosPluginRegistration().FindAndRegisterPlugins<ISqlResourcesPlugin>();

            base.Load(builder);
        }
    }
}
