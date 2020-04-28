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
using System;

namespace Rhetos.Configuration.Autofac
{
    /// <summary>
    /// Encapsulates a Dependency Injection container which scope is the same as the scope of the database transaction.
    /// </summary>
    public class RhetosTransactionScopeContainer : IDisposable
    {
        private event Action<ContainerBuilder> _scopeInitialization;
        private bool _commitChanges;
        private Lazy<IContainer> _iocConatiner;
        private ILifetimeScope _lifetimeScope;

        public event Action<ContainerBuilder> ScopeInitialization
        {
            add
            {
                CheckIfScopeHasAlreadyBeenInitialized();
                _scopeInitialization += value;
            }
            remove
            {
                CheckIfScopeHasAlreadyBeenInitialized();
                _scopeInitialization -= value;
            }
        }

        /// <param name="iocConatiner">
        /// The Dependency Injection container used to create the transaction scope container.
        /// </param>
        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked when Dispose is called.
        /// </param>
        public RhetosTransactionScopeContainer(Lazy<IContainer> iocConatiner, bool commitChanges = true)
        {
            _iocConatiner = iocConatiner;
            _commitChanges = commitChanges;
        }

        public T Resolve<T>()
        {
            InitializeScope();
            return _lifetimeScope.Resolve<T>();
        }

        public void Dispose()
        {
            if (!_commitChanges && _lifetimeScope != null)
                _lifetimeScope.Resolve<IPersistenceTransaction>().DiscardChanges();

            if (_lifetimeScope != null)
                _lifetimeScope.Dispose();
        }

        private void InitializeScope()
        {
            if (_lifetimeScope == null)
            {
                if (_scopeInitialization != null)
                    _lifetimeScope = _iocConatiner.Value.BeginLifetimeScope(_scopeInitialization);
                else
                    _lifetimeScope = _iocConatiner.Value.BeginLifetimeScope();
            }
        }

        private void CheckIfScopeHasAlreadyBeenInitialized()
        {
            if (_lifetimeScope != null)
                throw new FrameworkException($"The Dependency Injection container has already been initialized. Customize container configuration with {nameof(ScopeInitialization)} before calling the {nameof(Resolve)} method.");
        }
    }
}
