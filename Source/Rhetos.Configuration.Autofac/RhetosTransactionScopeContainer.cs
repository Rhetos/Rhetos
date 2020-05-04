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
        private readonly bool _commitChanges;
        private readonly Lazy<ILifetimeScope> _lifetimeScope;

        /// <param name="iocContainer">
        /// The Dependency Injection container used to create the transaction scope container.
        /// </param>
        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked when Dispose is called.
        /// </param>
        public RhetosTransactionScopeContainer(Lazy<IContainer> iocContainer, bool commitChanges, Action<ContainerBuilder> registerCustomComponents = null)
        {
            _commitChanges = commitChanges;
            _lifetimeScope = new Lazy<ILifetimeScope>(() => registerCustomComponents != null ? iocContainer.Value.BeginLifetimeScope(registerCustomComponents) : iocContainer.Value.BeginLifetimeScope());
        }

        public T Resolve<T>()
        {
            return _lifetimeScope.Value.Resolve<T>();
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
