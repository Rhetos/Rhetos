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
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        void ExecuteReader(string command, Action<DbDataReader> action);

        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        void ExecuteSql(IEnumerable<string> commands);

        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        /// <param name="beforeExecute">Intended for progress reporting. The integer argument is the index of currently executing command in <paramref name="commands"/>.</param>
        /// <param name="afterExecute">Intended for progress reporting. The integer argument is the index of currently executed command in <paramref name="commands"/>.</param>
        void ExecuteSql(IEnumerable<string> commands, Action<int> beforeExecute, Action<int> afterExecute);

        /// <summary>
        /// Executes a parametrized query on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        void ExecuteReaderRaw(string query, object[] parameters, Action<DbDataReader> read);

        /// <summary>
        /// Executes a parametrized query on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        Task ExecuteReaderRawAsync(string query, object[] parameters, Action<DbDataReader> read, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a parametrized command on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        int ExecuteSqlRaw(string query, object[] parameters);

        /// <summary>
        /// Executes a parametrized command on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        /// <remarks>
        /// Executes the SQL command in the current scope's database transaction, if <see cref="PersistenceTransactionOptions.UseDatabaseTransaction"/> is set (default).
        /// </remarks>
        Task<int> ExecuteSqlRawAsync(string query, object[] parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns open transaction count for the current SQL connection.
        /// </summary>
        /// <remarks>
        /// The result can differ from expected value,
        /// for example if the transaction has already been committed or rolled back
        /// without and error returned to the application.
        /// Verifying transaction count is specially useful to detect a case when another database transaction
        /// was created by SQL script or stored procedure, and left unclosed because of some a bug in the script;
        /// it might cause silent bugs and corrupted data when the application's commit
        /// would only reduce transaction count, but not actually commit the transaction.
        /// </remarks>
        int GetTransactionCount();
    }

    public static class SqlExecuterExtensions
    {
        /// <summary>
        /// Executes the SQL queries in a transaction.
        /// </summary>
        public static void ExecuteSql(this ISqlExecuter sqlExecuter, params string[] commands)
            => sqlExecuter.ExecuteSql(commands);

        /// <summary>
        /// Uses interpolated string to execute a parametrized command on the database
        /// </summary>
        public static int ExecuteSqlInterpolated(this ISqlExecuter sqlExecuter, FormattableString query)
            => sqlExecuter.ExecuteSqlRaw(query.Format, query.GetArguments());

        /// <summary>
        /// Uses interpolated string to execute a parametrized command on the database
        /// </summary>
        public static Task<int> ExecuteSqlInterpolatedAsync(this ISqlExecuter sqlExecuter, FormattableString query)
            => sqlExecuter.ExecuteSqlRawAsync(query.Format, query.GetArguments());

        /// <summary>
        /// Uses interpolated string to execute a parametrized query on the database
        /// </summary>
        public static void ExecuteReaderInterpolated(this ISqlExecuter sqlExecuter, FormattableString query, Action<DbDataReader> read)
            => sqlExecuter.ExecuteReaderRaw(query.Format, query.GetArguments(), read);

        /// <summary>
        /// Uses interpolated string to execute a parametrized query on the database
        /// </summary>
        public static Task ExecuteReaderInterpolatedAsync(this ISqlExecuter sqlExecuter, FormattableString query, Action<DbDataReader> read)
            => sqlExecuter.ExecuteReaderRawAsync(query.Format, query.GetArguments(), read);
    }
}
