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
using Rhetos.Persistence;
using System;

namespace Rhetos
{
    /// <summary>
    /// Dependency Injection container which scope is the same as the scope of the database transaction, for executing a single unit of work.
    /// Note that the changes in database will be rolled back by default.
    /// To commit changes to database, call <see cref="CommitAndClose"/> at the end of the 'using' block.
    /// </summary>
    public class UnitOfWorkScope : IDisposable
    {
        private bool _commitChanges = false;
        private readonly ILifetimeScope _lifetimeScope;
        private bool disposed;

        /// <param name="iocContainer">
        /// The Dependency Injection container for the transaction scope.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services previously registered in <paramref name="iocContainer"/>.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// <para>
        /// Note that the transaction-scope component registration might not affect singleton components.
        /// Customize the behavior of singleton components in <see cref="ProcessContainer"/> constructor.
        /// </para>
        /// </param>
        public UnitOfWorkScope(IContainer iocContainer, Action<ContainerBuilder> registerCustomComponents = null)
        {
            _lifetimeScope = registerCustomComponents != null
                ? iocContainer.BeginLifetimeScope(registerCustomComponents)
                : iocContainer.BeginLifetimeScope();
        }

        public T Resolve<T>()
        {
            return _lifetimeScope.Resolve<T>();
        }

        /// <summary>
        /// Indicates that all operations within the scope are completed successfully, and the transaction can be committed when the <see cref="UnitOfWorkScope"/> is disposed.
        /// It is a good practice to put the call as the last statement in the using block.
        /// </summary>
        /// <remarks>
        /// This method call will be ignored if the database transaction is discarded by <see cref="IPersistenceTransaction.DiscardChanges()"/>.
        /// </remarks>
        public void CommitOnDispose()
        {
            _commitChanges = true;
        }

        /// <summary>
        /// Commits and closes the database transaction for the current unit of work (lifetime scope).
        /// It is a good practice to put the call as the last statement in the using block.
        /// </summary>
        /// <remarks>
        /// After calling this method, any later database operation in the current scope might result with an error.
        /// The transaction will be rolled back, instead of committed, if <see cref="IPersistenceTransaction.DiscardChanges"/> method was called earlier.
        /// </remarks>
        public void CommitAndClose()
        {
            _lifetimeScope.Resolve<IPersistenceTransaction>().Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (!_commitChanges)
                        _lifetimeScope.Resolve<IPersistenceTransaction>().DiscardChanges();
                    _lifetimeScope.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
