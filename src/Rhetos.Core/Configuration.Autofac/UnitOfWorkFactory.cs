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
using System;

namespace Rhetos
{
    /// <summary>
    /// Helper for manual scope control.
    /// It is intended for components that need to manually execute a separate unit of work, unrelated to the current operation's unit of work
    /// (for example, unrelated to the current web request).
    /// </summary>
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        /// <summary>
        /// Using WeakReference to avoid interfering with the DI container disposal, in case of a bug within
        /// custom scope management in an business application.
        /// </summary>
        private WeakReference<ILifetimeScope> _containerReference;
        private Action<ContainerBuilder> _customRegistrations;

        /// <summary>
        /// Initialization of the unit of work factory is separate from the constructor, to allow the factory
        /// to have the reference to it's own container.
        /// </summary>
        /// <remarks>
        /// The <see cref="UnitOfWorkFactory"/> does not capture the reference to the <see cref="RhetosHost"/>,
        /// to allow disposal of the root container. It uses a weak reference instead.
        /// </remarks>
        /// <param name="customRegistrations">
        /// Additional container configuration that will be executed for each new unit of work scope.
        /// </param>
        public void Initialize(RhetosHost rhetosHost, Action<ContainerBuilder> customRegistrations = null)
        {
            ArgumentNullException.ThrowIfNull(rhetosHost);

            Initialize(rhetosHost.GetRootContainer(), customRegistrations);
        }

        /// <summary>
        /// Initialization of the unit of work factory is separate from the constructor, to allow the factory
        /// to have the reference to it's own container.
        /// </summary>
        /// <remarks>
        /// The <see cref="UnitOfWorkFactory"/> does not capture the reference to the <paramref name="container"/>,
        /// to allow disposal of the container. It uses a weak reference instead.
        /// </remarks>
        /// <param name="container">
        /// Provide the root container or a customized lifetime scope from RhetosHost.
        /// </param>
        /// <param name="customRegistrations">
        /// Additional container configuration that will be executed for each new unit of work scope.
        /// </param>
        public void Initialize(ILifetimeScope container, Action<ContainerBuilder> customRegistrations = null)
        {
            _containerReference = new WeakReference<ILifetimeScope>(container);
            _customRegistrations = customRegistrations;
        }

        IUnitOfWorkScope IUnitOfWorkFactory.CreateScope(Action<ContainerBuilder> registerScopeComponentsAction)
        {
            return CreateScope(registerScopeComponentsAction);
        }

        protected IUnitOfWorkScope CreateScope(Action<ContainerBuilder> registerScopeComponentsAction)
        {
            var container = GetContainer();
            return new UnitOfWorkScope(container, _customRegistrations + registerScopeComponentsAction);
        }

        private ILifetimeScope GetContainer()
        {
            if (_containerReference == null)
                throw new InvalidOperationException($"{nameof(UnitOfWorkFactory)} not initialized. Call {nameof(UnitOfWorkFactory)}.{nameof(Initialize)} first.");
            if (!_containerReference.TryGetTarget(out var container))
                throw new InvalidOperationException($"The previously provided DI container has already been disposed.");
            return container;
        }
    }
}
