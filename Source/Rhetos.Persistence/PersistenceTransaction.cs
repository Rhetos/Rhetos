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
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace Rhetos.Persistence
{
    public class PersistenceTransaction : IPersistenceTransaction
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly IUserInfo _userInfo;

        private DbConnection _connection;
        private DbTransaction _transaction;
        private bool _disposed;
        private bool _discard;
        private int _persistenceTransactionId;
        static int _counter = 0;

        public PersistenceTransaction(ILogProvider logProvider, ConnectionString connectionString, IUserInfo userInfo)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _connectionString = connectionString;
            _userInfo = userInfo;
            _persistenceTransactionId = Interlocked.Increment(ref _counter);
        }

        public void DiscardChanges()
        {
            _discard = true;
        }

        public event Action BeforeClose;

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.Trace(() => "Disposing (" + _persistenceTransactionId + ").");
                if (_discard)
                    Rollback();
                else
                    Commit();
            }

            BeforeClose = null;
            _disposed = true;
        }

        public void CommitAndReconnect()
        {
            string callerInfo = GetCaller();
            _logger.Info(() => "CommitAndReconnect is obsolete. Please upgrade to a latest version of the Rhetos plugin '" + callerInfo + "'.");

            if (_disposed)
                throw new FrameworkException("Trying to commit and reconnect a disposed persistence transaction.");
            if (_discard)
                throw new FrameworkException("Trying to commit and reconnect a discarded persistence transaction.");

            _logger.Trace(() => "CommitAndReconnect (" + _persistenceTransactionId + ").");
            Commit();
        }

        private static string GetCaller()
        {
            try
            {
                var callStack = new StackTrace().GetFrames();
                return new StackTrace().GetFrame(2).GetMethod().DeclaringType.AssemblyQualifiedName;
            }
            catch
            {
                return "";
            };
        }

        private void Commit()
        {
            _logger.Trace(() => "Committing (" + _persistenceTransactionId + ").");
            if (BeforeClose != null)
            {
                BeforeClose();
                BeforeClose = null;
            }

            if (_transaction != null)
            {
                _logger.Trace(() => "Committing transaction (" + _persistenceTransactionId + ").");
                _transaction.Commit();
                _transaction = null;
            }
            if (_connection != null)
            {
                _logger.Trace(() => "Closing connection (" + _persistenceTransactionId + ").");
                _connection.Close();
                _connection = null;
            }
        }

        private void Rollback()
        {
            _logger.Trace(() => "Rolling back (" + _persistenceTransactionId + ").");
            BeforeClose = null;
            try
            {
                if (_transaction != null)
                {
                    _logger.Trace(() => "Rolling back transaction (" + _persistenceTransactionId + ").");
                    _transaction.Rollback();
                }
            }
            catch (Exception ex)
            {
                // Failure on rollback should be ignored to allow other cleanup code to be executed. Also, a previously handled database connection error may have triggered the rollback.
                _logger.Trace("Error on transaction rollback when canceling persistence transaction, it can be safely ignored. " + ex);
            }
            finally
            {
                _transaction = null;
            }

            try
            {
                if (_connection != null)
                {
                    _logger.Trace(() => "Closing connection (" + _persistenceTransactionId + ").");
                    _connection.Close();
                }
            }
            catch (Exception ex)
            {
                // Failure when canceling connection should be ignored to allow other cleanup code to be executed. Also, a previously handled database connection error may have triggered the rollback.
                _logger.Trace("Error on connection close when canceling persistence transaction, it can be safely ignored. " + ex);
            }
            finally
            {
                _connection = null;
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
                        using (var sqlCommand = _connection.CreateCommand())
                        {
                            sqlCommand.CommandText = MsSqlUtility.SetUserContextInfoQuery(_userInfo);
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
