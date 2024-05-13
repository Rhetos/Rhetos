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

using Rhetos.Persistence;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    /// <summary>
    /// <see cref="ISqlExecuter"/> is a helper for executing SQL commands.
    /// </summary>
    /// <remarks>
    /// <see cref="ISqlExecuter"/> uses a database transaction provided by the current unit-of-work scope,
    /// if available, from <see cref="IPersistenceTransaction"/>.
    /// <para>
    /// At <b>run-time</b>, this means that every database command executed in a single scope (a single web request, for example)
    /// will be executed in a single database transaction, that will be committed or rolled back at the and of the scope.
    /// </para>
    /// <para>
    /// At <b>deployment (dbupdate)</b>, the global transaction is not used
    /// (see <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/>).
    /// Many dbupdate components use <see cref="ISqlTransactionBatches"/> instead,
    /// that manages transactions for batches of SQL scripts.
    /// </para>
    /// </remarks>
    public interface ISqlExecuter
    {
        /// <summary>
        /// Executes a parametrized command on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        /// <param name="parameters">
        /// Set to <see langword="null"/> if the query does not have parameters.
        /// </param>
        int ExecuteSqlRaw(string query, object[] parameters);

        /// <summary>
        /// Executes a parametrized command on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        /// <param name="parameters">
        /// Set to <see langword="null"/> if the query does not have parameters.
        /// </param>
        Task<int> ExecuteSqlRawAsync(string query, object[] parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a parametrized query on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        /// <param name="parameters">
        /// Set to <see langword="null"/> if the query does not have parameters.
        /// </param>
        void ExecuteReaderRaw(string query, object[] parameters, Action<DbDataReader> read);

        /// <summary>
        /// Executes a parametrized query on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        /// <param name="parameters">
        /// Set to <see langword="null"/> if the query does not have parameters.
        /// </param>
        Task ExecuteReaderRawAsync(string query, object[] parameters, Action<DbDataReader> read, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns an internal information on the expected transaction state.
        /// It is used for checking the if the transaction is in the expected state after executing the SQL commands,
        /// to avoid silent bugs in critical cases (for example, on database update).
        /// It should be called before executing the SQL commands, and checked after with <see cref="CheckTransactionState(int)"/>.
        /// </summary>
        int GetTransactionInitialState();

        /// <summary>
        /// Checks if the transaction is in the expected state after executing the SQL commands,
        /// to avoid silent bugs in critical cases (for example, on database update).
        /// </summary>
        /// <param name="initialState">
        /// Provided by <see cref="GetTransactionInitialState"/> call *before* executing the SQL commands.
        /// </param>
        /// <returns>
        /// Returns the error message, or <see langword="null"/> if there is no error.
        /// </returns>
        string CheckTransactionState(int initialState);

        /// <summary>
        /// Creates a custom lock in database. It blocks other parallel connections from creating a lock with the same resource name.
        /// This is often use to reduce deadlocks is database when parallel users (or even parallel web requests from one user)
        /// execute complex data modifications.
        /// The lock is automatically closed when the SQL transaction is committed or rolled back (e.g. when the web request return the response).
        /// Note that the issues with parallelism in Microsoft database may depend on READ_COMMITTED_SNAPSHOT setting.
        /// </summary>
        /// <param name="resources">Custom string that represents a unique lock identifier. It is case insensitive.
        /// It is *not* related to any actual database object such as table name.</param>
        /// <param name="wait">If set to <see langword="false"/>, this method will fail immediately if the resource
        /// is locked by another process, instead of waiting for the lock to be removed.</param>
        void GetDbLock(IEnumerable<string> resources, bool wait = true);

        /// <summary>
        /// Releases a custom lock in database, created by <see cref="GetDbLock"/>.
        /// </summary>
        void ReleaseDbLock(IEnumerable<string> resources);
    }

    public static class SqlExecuterExtensions
    {
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        /// <param name="beforeExecute">Intended for progress reporting. The integer argument is the index of currently executing command in <paramref name="commands"/>.</param>
        /// <param name="afterExecute">Intended for progress reporting. The integer argument is the index of currently executed command in <paramref name="commands"/>.</param>
        public static void ExecuteSql(this ISqlExecuter sqlExecuter, IEnumerable<string> commands, Action<int> beforeExecute, Action<int> afterExecute)
        {
            var commandsCollection = CsUtility.Materialized(commands);
            if (commandsCollection.Any(sql => sql == null))
                throw new FrameworkException("SQL script is null.");

            int count = 0;
            foreach (string sql in commandsCollection)
            {
                if (string.IsNullOrWhiteSpace(sql))
                    continue;

                beforeExecute?.Invoke(count);
                sqlExecuter.ExecuteSqlRaw(sql, null);
                afterExecute?.Invoke(count);

                count++;
            }
        }

        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        public static void ExecuteSql(this ISqlExecuter sqlExecuter, IEnumerable<string> commands)
            => sqlExecuter.ExecuteSql(commands, null, null);

        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        public static void ExecuteSql(this ISqlExecuter sqlExecuter, params string[] commands)
            => sqlExecuter.ExecuteSql(commands, null, null);

        /// <summary>
        /// Uses interpolated string to execute a parametrized command on the database.
        /// </summary>
        public static int ExecuteSqlInterpolated(this ISqlExecuter sqlExecuter, FormattableString query)
            => sqlExecuter.ExecuteSqlRaw(query.Format, query.GetArguments());

        /// <summary>
        /// Uses interpolated string to execute a parametrized command on the database.
        /// </summary>
        public static Task<int> ExecuteSqlInterpolatedAsync(this ISqlExecuter sqlExecuter, FormattableString query)
            => sqlExecuter.ExecuteSqlRawAsync(query.Format, query.GetArguments());

        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        public static void ExecuteReader(this ISqlExecuter sqlExecuter, string query, Action<DbDataReader> read)
            => sqlExecuter.ExecuteReaderRaw(query, null, read);

        /// <summary>
        /// Uses interpolated string to execute a parametrized query on the database.
        /// </summary>
        public static void ExecuteReaderInterpolated(this ISqlExecuter sqlExecuter, FormattableString query, Action<DbDataReader> read)
            => sqlExecuter.ExecuteReaderRaw(query.Format, query.GetArguments(), read);

        /// <summary>
        /// Uses interpolated string to execute a parametrized query on the database.
        /// </summary>
        public static Task ExecuteReaderInterpolatedAsync(this ISqlExecuter sqlExecuter, FormattableString query, Action<DbDataReader> read)

            => sqlExecuter.ExecuteReaderRawAsync(query.Format, query.GetArguments(), read);

        /// <summary>
        /// Creates a custom lock in database. It blocks other parallel connections from creating a lock with the same resource name.
        /// This is often use to reduce deadlocks is database when parallel users (or even parallel web requests from one user)
        /// execute complex data modifications.
        /// The lock is automatically closed when the SQL transaction is commited or rolledback (e.g. when the web request return the response).
        /// </summary>
        /// <param name="resource">Custom string that represents a unique lock identifier. It is case insensitive.
        /// It is *not* related to any actual database object such as table name.</param>
        /// <param name="wait">If set to <see langword="false"/>, this method will fail immediately if the resource
        /// is locked by another process, instead of waiting for the lock to be removed.</param>
        public static void GetDbLock(this ISqlExecuter sqlExecuter, string resource, bool wait = true)
        {
            sqlExecuter.GetDbLock(new[] { resource }, wait);
        }

        /// <summary>
        /// Releases a custom lock in database, created by <see cref="GetDbLock"/>.
        /// </summary>
        public static void ReleaseDbLock(this ISqlExecuter sqlExecuter, string resource)
        {
            sqlExecuter.ReleaseDbLock(new[] { resource });
        }
    }
}
