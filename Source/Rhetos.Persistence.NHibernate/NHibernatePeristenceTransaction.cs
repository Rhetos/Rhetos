/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Persistence.NHibernate
{
    public class NHibernatePersistenceTransaction : IPersistenceTransaction
    {
        private ISession _session;
        private ITransaction _transaction;
        private ILogger _logger;
        public delegate NHibernatePersistenceTransaction Factory(ISession session, ITransaction transaction); // TODO: This might be unnecessary when Autofac offers Func<ISession, ITransaction, IPersistenceTransaction>.

        enum TransactionState { Active, Submitted, Rollbacked }
        private TransactionState _state;

        public NHibernatePersistenceTransaction(ISession session, ITransaction transaction, ILogProvider logProvider)
        {
            _session = session;
            _transaction = transaction;
            _logger = logProvider.GetLogger("NHibernatePersistenceTransaction");
            _state = TransactionState.Active;
        }

        public ISession NHibernateSession
        {
            get { return _session; }
        }

        public void ApplyChanges()
        {
            if (_state != TransactionState.Active)
                throw new FrameworkException(string.Format("Cannot apply changes. Transaction is already {0}.", _state));

            _transaction.Commit();
            _state = TransactionState.Submitted;
            FreeResources();
        }

        public void DiscardChanges()
        {
            if (_state == TransactionState.Rollbacked) // It is acceptable for DiscardChanges to be called multiple time during the error handlinga process.
                return;
            if (_state != TransactionState.Active)
                throw new FrameworkException(string.Format("Cannot discard changes. Transaction is already {0}.", _state));

            try
            {
                _transaction.Rollback();
            }
            catch(TransactionException)
            {
            }
            _state = TransactionState.Rollbacked;
            FreeResources();
        }

        public void Dispose()
        {
            FreeResources();
        }

        public void FreeResources()
        {
            if (_transaction != null)
            {
                try
                {
                    if (_state == TransactionState.Active)
                    {
                        _transaction.Rollback();
                        _state = TransactionState.Rollbacked;
                        try
                        {
                            // Unexpected open transaction while releaseing resources is logged, but the exception is not thrown,
                            // because that exception would mask the original exception that caused the resources to be released.
                            // It would make debugging very difficult.
                            _logger.Error("The transaction was not closed a regular way (ApplyChanges or DiscardChanges). It is automatically rollbacked while disposing NHibernatePersistenceTransaction.");
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }
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