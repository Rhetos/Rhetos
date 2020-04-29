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
        private Action<ContainerBuilder> _configureContainer;
        private bool _commitChanges;
        private Lazy<IContainer> _iocConatiner;
        private ILifetimeScope _lifetimeScope;

        /// <param name="iocConatiner">
        /// The Dependency Injection container used to create the transaction scope container.
        /// </param>
        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked when Dispose is called.
        /// </param>
        public RhetosTransactionScopeContainer(Lazy<IContainer> iocConatiner, bool commitChanges, Action<ContainerBuilder> configureContainer)
        {
            _iocConatiner = iocConatiner;
            _commitChanges = commitChanges;
            _configureContainer = configureContainer;
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
                if (_configureContainer != null)
                    _lifetimeScope = _iocConatiner.Value.BeginLifetimeScope(_configureContainer);
                else
                    _lifetimeScope = _iocConatiner.Value.BeginLifetimeScope();
            }
        }
    }
}
