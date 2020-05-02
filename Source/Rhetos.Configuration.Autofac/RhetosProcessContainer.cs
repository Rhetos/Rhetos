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
using System;
using System.Diagnostics;

namespace Rhetos.Configuration.Autofac
{
    public class RhetosProcessContainer
    {
        private readonly Lazy<IContainer> _rhetosIocContainer;
        private readonly Lazy<Host> _host;
        private readonly Lazy<IConfiguration> _configuration;

        public IConfiguration Configuration {
            get { return _configuration.Value; }
        }

        /// <summary>
        /// It encapuslates a Dependency Injection container created by calling the method
        /// <see cref="Host.CreateRhetosContainer(string, ILogProvider, Action{IConfigurationBuilder}, Action{ContainerBuilder})"/>
        /// with the parameters passed to the constructor.
        /// </summary>
        public RhetosProcessContainer(Func<string> findRhetosHostFolder = null, ILogProvider logProvider = null,
            Action<IConfigurationBuilder> addCustomConfiguration = null, Action<ContainerBuilder> registerCustomComponents = null)
        {
            logProvider = logProvider ?? new ConsoleLogProvider();
            if(findRhetosHostFolder == null)
                findRhetosHostFolder = () => AppDomain.CurrentDomain.BaseDirectory;

            _host = new Lazy<Host>(() => Host.Find(findRhetosHostFolder(), logProvider), true);
            _configuration = new Lazy<IConfiguration>(() => _host.Value.RhetosRuntime.BuildConfiguration(logProvider, _host.Value.ConfigurationFolder, addCustomConfiguration), true);

            _rhetosIocContainer = new Lazy<IContainer>(() => {
                //The values for rhetosRuntime and configuration are resolved before the call to Stopwatch.StartNew
                //so that the performance logging only takes into account the time needed to build the ioc container
                var rhetosRuntime = _host.Value.RhetosRuntime;
                var configuration = _configuration.Value;
                var sw = Stopwatch.StartNew();
                var iocContainer = rhetosRuntime.BuildContainer(logProvider, configuration, (builder) => {
                    // Override runtime IUserInfo plugins. This container is intended to be used in a simple process or unit tests.
                    builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
                    builder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();
                    registerCustomComponents?.Invoke(builder);
                });
                logProvider.GetLogger("Performance").Write(sw, $"{nameof(RhetosTransactionScopeContainer)}: Built IoC container");
                return iocContainer;
            }, true);
        }

        public T Resolve<T>() => _rhetosIocContainer.Value.Resolve<T>();

        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked. By default it is set to true.
        /// </param>
        /// <param name="configureContainer">
        /// Used to customize the trnsaction scope Dependency Injection container. By default it is set to null.
        /// </param>/// 
        public RhetosTransactionScopeContainer CreateTransactionScope(bool commitChanges, Action<ContainerBuilder> configureContainer = null) => new RhetosTransactionScopeContainer(_rhetosIocContainer, commitChanges, configureContainer);
    }
}
