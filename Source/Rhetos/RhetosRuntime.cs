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
using Rhetos.HomePage;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using Rhetos.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos
{
    [Export(typeof(IRhetosRuntime))]
    public class RhetosRuntime : IRhetosRuntime
    {
        private readonly bool _isHost;

        public RhetosRuntime() : this(false) { }

        internal RhetosRuntime(bool isHost)
        {
            _isHost = isHost;
        }

        public IConfiguration BuildConfiguration(ILogProvider logProvider, string configurationFolder, Action<IConfigurationBuilder> addCustomConfiguration)
        {
            var configurationBuilder = new ConfigurationBuilder();

            // Main application configuration (usually Web.config).
            if (_isHost)
                configurationBuilder.AddConfigurationManagerConfiguration(); 
            else
                configurationBuilder.AddWebConfiguration(configurationFolder);

            // Rhetos runtime configuration JSON files.
            configurationBuilder.AddRhetosAppEnvironment(configurationFolder); 

            addCustomConfiguration?.Invoke(configurationBuilder);
            return configurationBuilder.Build();
        }

        public IContainer BuildContainer(ILogProvider logProvider, IConfiguration configuration, Action<ContainerBuilder> registerCustomComponents)
        {
            return BuildContainer(logProvider, configuration, registerCustomComponents, LegacyUtilities.GetRuntimeAssembliesDelegate(configuration));
        }

        private IContainer BuildContainer(ILogProvider logProvider, IConfiguration configuration, Action<ContainerBuilder> registerCustomComponents, Func<IEnumerable<string>> getAssembliesDelegate)
        {
            var builder = new RhetosContainerBuilder(configuration, logProvider, getAssembliesDelegate);

            builder.AddRhetosRuntime();

            if (_isHost)
            {
                // WCF-specific component registrations.
                // Can be customized later by plugin modules.
                builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope();
                builder.RegisterType<RhetosService>().As<RhetosService>().As<IServerApplication>();
                builder.RegisterType<Rhetos.Web.GlobalErrorHandler>();
                builder.RegisterType<WebServices>();
                builder.GetPluginRegistration().FindAndRegisterPlugins<IService>();
            }

            builder.AddPluginModules();

            if (_isHost)
            {
                // HomePageServiceInitializer must be register after other core services and plugins to allow routing overrides.
                builder.RegisterType<HomePageService>().InstancePerLifetimeScope();
                builder.RegisterType<HomePageServiceInitializer>().As<IService>();
                builder.GetPluginRegistration().FindAndRegisterPlugins<IHomePageSnippet>();
            }

            registerCustomComponents?.Invoke(builder);

            return builder.Build();
        }
    }
}