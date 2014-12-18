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

using System;
using NHibernate;
using NHibernate.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos.Persistence.NHibernate
{
    public class NHibernatePersistenceTransaction : IPersistenceTransaction
    {
        private readonly IPersistenceEngine _persistenceEngine;
        private readonly ILogger _logger;
        private readonly IUserInfo _userInfo;

        private ISession _session;
        private ITransaction _transaction;
        private bool _initialized;
        private bool _disposed;
        private bool _discard;

        public NHibernatePersistenceTransaction(IPersistenceEngine persistenceEngine, ILogProvider logProvider, IUserInfo userInfo)
        {
            _persistenceEngine = persistenceEngine;
            _logger = logProvider.GetLogger("NHibernatePersistenceTransaction");
            _userInfo = userInfo;
        }

        public ISession NHibernateSession
        {
            get
            {
                Initialize();
                return _session;
            }
        }

        private void Initialize()
        {
            if (_disposed)
                throw new FrameworkException("Trying to initialize NHibernatePersistenceTransaction that is already disposed.");

            if (!_initialized)
            {
                var newTran = _persistenceEngine.BeginTransaction(_userInfo);
                _session = newTran.Item1;
                _transaction = newTran.Item2;
                _initialized = true;
            }
        }

        public void DiscardChanges()
        {
            _discard = true;
        }

        /// <summary>
        /// Drops the database connection and creates a new one to release the database locks.
        /// This method should not be used during regular server run-time because it splits the unit of work
        /// making it impossible to rollback the whole session in case of a need.
        /// </summary>
        public void CommitAndReconnect()
        {
            if (_disposed)
                throw new FrameworkException("Trying to commit and reconnect NHibernatePersistenceTransaction that is already disposed.");
            if (_discard)
                throw new FrameworkException("Trying to commit and reconnect a discarded NHibernatePersistenceTransaction.");

            Dispose();
            _disposed = false;
            _initialized = false;
        }

        public void Dispose()
        {
            if (_initialized && !_disposed)
            {
                if (_discard)
                    Rollback();
                else
                    Commit();
            }

            BeforeClose = null;
            _disposed = true;
            FreeResources();
        }

        public event Action BeforeClose;

        private void Commit()
        {
            if (BeforeClose != null)
            {
                BeforeClose();
                BeforeClose = null;
            }

            if (_transaction != null)
                _transaction.Commit();
        }

        private void Rollback()
        {
            BeforeClose = null;
            try
            {
                if (_transaction != null)
                    _transaction.Rollback();
            }
            catch(TransactionException)
            {
            }
        }

        private void FreeResources()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            if (_session != null)
            {
                try
                {
                    _session.Close();
                }
                catch
                {
                }
                _session.Dispose();
                _session = null;
            }
        }
    }
}