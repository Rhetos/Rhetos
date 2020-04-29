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

        /// <summary>
        /// It encapuslates a Dependency Injection container created by calling the method
        /// <see cref="Host.CreateRhetosContainer(string, ILogProvider, Action{IConfigurationBuilder}, Action{ContainerBuilder})"/>
        /// with the parameters passed to the constructor.
        /// </summary>
        public RhetosProcessContainer(Func<string> findRhetosHostFolder = null, ILogProvider logProvider = null,
            Action<IConfigurationBuilder> addCustomConfiguration = null, Action<ContainerBuilder> registerCustomComponents = null)
        {
            logProvider = logProvider ?? new ConsoleLogProvider();

            _rhetosIocContainer = new Lazy<IContainer>(() => {
                var sw = Stopwatch.StartNew();
                var iocContainer = Host.CreateRhetosContainer(findRhetosHostFolder(), logProvider, addCustomConfiguration, (builder) => {
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
