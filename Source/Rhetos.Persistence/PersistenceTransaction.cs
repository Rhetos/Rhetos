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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace Rhetos.Persistence
{
    public class PersistenceTransaction : IPersistenceTransaction, IUnitOfWork
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly IUserInfo _userInfo;
        private readonly int _persistenceTransactionId;

        private DbConnection _connection;
        private DbTransaction _transaction;
        private bool _disposed;
        private bool _discardOnDispose;
        private bool _commitOnDispose;
        static int _counter = 0;

        public PersistenceTransaction(ILogProvider logProvider, ConnectionString connectionString, IUserInfo userInfo)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _connectionString = connectionString;
            _userInfo = userInfo;
            _persistenceTransactionId = Interlocked.Increment(ref _counter);
        }

        public void CommitOnDispose()
        {
            _commitOnDispose = true;
        }

        public void DiscardOnDispose()
        {
            _discardOnDispose = true;
        }

        public void CommitAndClose()
        {
            CommitOnDispose();
            Dispose();
        }

        public void RollbackAndClose()
        {
            DiscardOnDispose();
            Dispose();
        }

        public event Action BeforeClose;
        public event Action AfterClose;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        _logger.Trace(() => "Disposing (" + _persistenceTransactionId + ").");
                        if (_commitOnDispose && !_discardOnDispose)
                            Commit();
                        else
                            Rollback();
                    }
                }
                finally
                {
                    BeforeClose = null;
                    AfterClose = null;
                    _disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Commit()
        {
            _logger.Trace(() => "Committing (" + _persistenceTransactionId + ").");
            var exceptions = new List<Exception>();

            Try(
                BeforeClose,
                () => BeforeClose = null,
                exceptions);

            Try(
                !exceptions.Any() ? (Action)CommitTransaction : (Action)RollbackTransaction,
                () => _transaction = null,
                exceptions);

            Try(
                CloseConnection,
                () => _connection = null,
                exceptions);

            if (exceptions.Any())
                ExceptionsUtility.Rethrow(exceptions.First());

            AfterClose?.Invoke();
            AfterClose = null;
        }

        private void Rollback()
        {
            _logger.Trace(() => "Rolling back (" + _persistenceTransactionId + ").");
            var exceptions = new List<Exception>();
            BeforeClose = null;
            AfterClose = null;

            Try(
                RollbackTransaction,
                () => _transaction = null,
                exceptions);

            Try(
                CloseConnection,
                () => _connection = null,
                exceptions);

            // Failure on rollback should be ignored to allow other cleanup code to be executed, and also to avoid masking the original exception on transaction disposal.
            if (exceptions.Any())
                // Logging the error with low severity level, because the Rollback method is called after some other error is caught and reported.
                // The rollback might fail if the main error closed the transaction or database connection, this is expected behavior.
                _logger.Trace("Error on rollback, it can be safely ignored. " + exceptions.First());
        }

        private static void Try(Action action, Action cleanup, List<Exception> exceptions)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
            finally
            {
                cleanup?.Invoke();
            }
        }

        private void CommitTransaction()
        {
            if (_transaction != null)
            {
                _logger.Trace(() => "Committing transaction (" + _persistenceTransactionId + ").");
                _transaction.Commit();
            }
        }

        private void RollbackTransaction()
        {
            if (_transaction != null)
            {
                _logger.Trace(() => "Rolling back transaction (" + _persistenceTransactionId + ").");
                _transaction.Rollback();
            }
        }
        private void CloseConnection()
        {
            if (_connection != null)
            {
                _logger.Trace(() => "Closing connection (" + _persistenceTransactionId + ").");
                _connection.Close();
            }
        }

        public DbConnection Connection
        {
            get
            {
                if (_disposed)
                    throw new FrameworkException("Trying to use the Connection property of a disposed persistence transaction.");

                if (_connection == null)
                {
                    _logger.Trace(() => "Opening connection (" + _persistenceTransactionId + ").");
                    _connection = new SqlConnection(_connectionString);
                    _connection.Open();

                    if (_userInfo.IsUserRecognized)
                    {
                        var sqlCommand = MsSqlUtility.SetUserContextInfoQuery(_userInfo);
                        sqlCommand.Connection = _connection;
                        sqlCommand.ExecuteNonQuery();
                    }

                    _logger.Trace(() => "Beginning transaction (" + _persistenceTransactionId + ").");
                    _transaction = _connection.BeginTransaction();
                }

                return _connection;
            }
        }

        public DbTransaction Transaction
        {
            get
            {
                if (_disposed)
                    throw new FrameworkException("Trying to use the Transaction property of a disposed persistence transaction.");

                return _transaction;
            }
        }
    }
}
