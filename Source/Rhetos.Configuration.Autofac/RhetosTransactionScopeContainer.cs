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

namespace Rhetos
{
    /// <summary>
    /// Dependency Injection container which scope is the same as the scope of the database transaction, for executing a single unit of work.
    /// Note that the changes in database will be rolled back by default.
    /// To commit changes to database, call <see cref="CommitChanges"/> at the end of the 'using' block.
    /// </summary>
    public class RhetosTransactionScopeContainer : IDisposable
    {
        private bool _commitChanges = false;
        private readonly Lazy<ILifetimeScope> _lifetimeScope;

        /// <param name="iocContainer">
        /// The Dependency Injection container used to create the transaction scope container.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// </param>
        public RhetosTransactionScopeContainer(IContainer iocContainer, Action<ContainerBuilder> registerCustomComponents = null)
        {
            _lifetimeScope = new Lazy<ILifetimeScope>(() => registerCustomComponents != null ? iocContainer.BeginLifetimeScope(registerCustomComponents) : iocContainer.BeginLifetimeScope());
        }

        public T Resolve<T>()
        {
            return _lifetimeScope.Value.Resolve<T>();
        }

        /// <summary>
        /// The changes are not committed immediately, they will be committed on DI container disposal.
        /// Call this method at the end of the 'using' block to mark the current database transaction to be committed.
        /// </summary>
        public void CommitChanges()
        {
            _commitChanges = true;
        }

        private bool disposed = false; // Standard IDisposable pattern to detect redundant calls.

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (!_commitChanges && _lifetimeScope.IsValueCreated)
                    _lifetimeScope.Value.Resolve<IPersistenceTransaction>().DiscardChanges();

                if (_lifetimeScope.IsValueCreated)
                    _lifetimeScope.Value.Dispose();
            }

            disposed = true;
        }
    }
}
