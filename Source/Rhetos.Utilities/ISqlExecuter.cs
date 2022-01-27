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
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public interface ISqlExecuter
    {
        void ExecuteReader(string command, Action<DbDataReader> action);

        void ExecuteSql(IEnumerable<string> commands);

        void ExecuteSql(IEnumerable<string> commands, Action<int> beforeExecute, Action<int> afterExecute);

        /// <summary>
        /// Executes a parametrized query on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        void ExecuteReaderRaw(string query, object[] parameters, Action<DbDataReader> read);

        /// <summary>
        /// Executes a parametrized query on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        Task ExecuteReaderRawAsync(string query, object[] parameters, Action<DbDataReader> read, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a parametrized command on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        int ExecuteSqlRaw(string query, object[] parameters);

        /// <summary>
        /// Executes a parametrized command on the database.
        /// If you need more control on how a parameter is mapped to a database type, <see cref="DbParameter"/> can be used as a parameter.
        /// </summary>
        Task<int> ExecuteSqlRawAsync(string query, object[] parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Throw an exception if database connection does not match the expected level count.
        /// For example if it has already been committed or rolled back.
        /// This check is specially useful in case when an database transaction
        /// created by SQL script or stored procedure has been left unclosed by an error;
        /// it might cause silent bugs and corrupted data when the application's commit
        /// would only reduce transaction count, but not actually commit the transaction.
        /// </summary>
        void CheckTransactionCount(int expected);
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
