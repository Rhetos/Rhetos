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

        public IConfigurationProvider BuildConfiguration(ILogProvider logProvider, string configurationFolder, Action<IConfigurationBuilder> addCustomConfiguration)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddRhetosRuntimeConfiguration(configurationFolder);

            // Add WCF-specific configuration.
            if (_isHost)
                configurationBuilder.AddConfigurationManagerConfiguration();
            else
                configurationBuilder.AddWebConfiguration(configurationFolder);

            addCustomConfiguration?.Invoke(configurationBuilder);
            return configurationBuilder.Build();
        }

        public IContainer BuildContainer(ILogProvider logProvider, IConfigurationProvider configurationProvider, Action<ContainerBuilder> registerCustomComponents)
        {
            return BuildContainer(logProvider, configurationProvider, registerCustomComponents, LegacyUtilities.GetRuntimeAssembliesDelegate(configurationProvider));
        }

        private IContainer BuildContainer(ILogProvider logProvider, IConfigurationProvider configurationProvider, Action<ContainerBuilder> registerCustomComponents, Func<IEnumerable<string>> getAssembliesDelegate)
        {
            var builder = new RhetosContainerBuilder(configurationProvider, logProvider, getAssembliesDelegate);

            builder.AddRhetosRuntime();

            // WCF-specific component registrations.
            // Can be customized later by plugin modules.
            if (_isHost)
            {
                builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope();
                builder.RegisterType<RhetosService>().As<RhetosService>().As<IServerApplication>();
                builder.RegisterType<Rhetos.Web.GlobalErrorHandler>();
                builder.RegisterType<WebServices>();
                builder.GetPluginRegistration().FindAndRegisterPlugins<IService>();
                builder.GetPluginRegistration().FindAndRegisterPlugins<IHomePageSnippet>();
            }

            builder.AddPluginModules();

            registerCustomComponents?.Invoke(builder);

            return builder.Build();
        }
    }
}