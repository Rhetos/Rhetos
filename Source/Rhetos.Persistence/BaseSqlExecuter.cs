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
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Persistence
{
    /// <summary>
    /// Contains some default implementations for <see cref="ISqlExecuter" />.
    /// Consumer should use it as a mixin class, without having to reimplement the default methods.
    /// Also, it provides some cross-cutting concern utility methods, which are database provider agnostic.
    /// </summary>
    public class BaseSqlExecuter
    {
        protected readonly IPersistenceTransaction _persistenceTransaction;
        protected readonly DatabaseOptions _databaseOptions;
        protected readonly ILogger _logger;
        protected readonly ILogger _performanceLogger;

        public BaseSqlExecuter(ILogProvider logProvider, IPersistenceTransaction persistenceTransaction, DatabaseOptions databaseOptions)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _persistenceTransaction = persistenceTransaction;
            _databaseOptions = databaseOptions;
        }

        #region Partial implementation of ISqlExecuter

        public int ExecuteSqlRaw(string query, object[] parameters)
        {
            _logger.Trace(() => "Executing command: " + query);
            using var command = CreateCommand(query, parameters);
            var sw = Stopwatch.StartNew();
            try
            {
                return command.ExecuteNonQuery();
            }
            catch (DbException e)
            {
                throw ReportError(command, e);
            }
            finally
            {
                LogPerformanceIssue(sw, command.CommandText);
            }
        }

        public async Task<int> ExecuteSqlRawAsync(string query, object[] parameters, CancellationToken cancellationToken)
        {
            _logger.Trace(() => "Executing command: " + query);
            using var command = CreateCommand(query, parameters);
            var sw = Stopwatch.StartNew();
            try
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbException e)
            {
                throw ReportError(command, e);
            }
            finally
            {
                LogPerformanceIssue(sw, command.CommandText);
            }
        }

        public void ExecuteReaderRaw(string query, object[] parameters, Action<DbDataReader> read)
        {
            _logger.Trace(() => "Executing reader: " + query);
            using var command = CreateCommand(query, parameters);
            var sw = Stopwatch.StartNew();
            try
            {
                using var dataReader = command.ExecuteReader();
                while (!dataReader.IsClosed && dataReader.Read())
                    read(dataReader);
                dataReader.Close();
            }
            catch (DbException e)
            {
                throw ReportError(command, e);
            }
            finally
            {
                LogPerformanceIssue(sw, command.CommandText);
            }
        }

        public async Task ExecuteReaderRawAsync(string query, object[] parameters, Action<DbDataReader> read, CancellationToken cancellationToken)
        {
            _logger.Trace(() => "Executing reader: " + query);
            using var command = CreateCommand(query, parameters);
            var sw = Stopwatch.StartNew();
            try
            {
                using var dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (!dataReader.IsClosed && await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    read(dataReader);
                await dataReader.CloseAsync().ConfigureAwait(false);
            }
            catch (DbException e)
            {
                throw ReportError(command, e);
            }
            finally
            {
                LogPerformanceIssue(sw, command.CommandText);
            }
        }

        #endregion Partial implementation of ISqlExecuter

        protected DbCommand CreateCommand(string sql, object[] parameters)
        {
            var command = _persistenceTransaction.Connection.CreateCommand();
            command.Transaction = _persistenceTransaction.Transaction;
            command.CommandTimeout = _databaseOptions.SqlCommandTimeout;
            PrepareCommandTextAndParameters(command, sql, parameters);
            return command;
        }

        protected static readonly string parameterPrefix = "@__p";

        protected void PrepareCommandTextAndParameters(DbCommand command, string sql, object[] parameters)
        {
            if (parameters == null)
            {
                command.CommandText = sql;
                return;
            }

            var substitutions = new List<string>();
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter is DbParameter dbParameter)
                {
                    if (dbParameter.ParameterName.StartsWith(parameterPrefix))
                        throw new ArgumentException($"¨SQL parameter name should not start with '{parameterPrefix}'.");
                    substitutions.Add(dbParameter.ParameterName);
                    command.Parameters.Add(dbParameter);
                }
                else
                {
                    substitutions.Add(parameterPrefix + i);
                    var createdDbParameter = command.CreateParameter();
                    createdDbParameter.ParameterName = parameterPrefix + i.ToString(CultureInfo.InvariantCulture);
                    createdDbParameter.Value = parameter ?? DBNull.Value;
                    command.Parameters.Add(createdDbParameter);
                }
            }

            command.CommandText = string.Format(sql, substitutions.ToArray());
        }

        protected void LogPerformanceIssue(Stopwatch sw, string sql)
        {
            if (sw.Elapsed >= LoggerHelper.SlowEvent) // Avoid flooding the performance trace log.
                _performanceLogger.Write(sw, () => sql.Limit(50_000, true));
            else
                sw.Restart(); // _performanceLogger.Write would restart the stopwatch.
        }

        private FrameworkException ReportError(DbCommand command, DbException e)
        {
            if (command != null && !string.IsNullOrWhiteSpace(command.CommandText))
                _logger.Error("Unable to execute SQL query:\r\n" + command.CommandText.Limit(1_000_000, true));

            string msg = $"{e.GetType().Name} has occurred{ReportScriptName(command)}";

            var errorsReport = ReportSqlErrors(e);
            if (string.IsNullOrEmpty(errorsReport))
                msg += ".";
            else
                msg += ": " + errorsReport;

            return new FrameworkException(msg, e);
        }

        protected string ReportScriptName(DbCommand command)
        {
            const string namePrefix = "--Name: ";
            if (command?.CommandText?.StartsWith(namePrefix) == true)
                return " in '" + CsUtility.FirstLine(command.CommandText).Substring(namePrefix.Length).Limit(1000, "...") + "'";
            else
                return "";
        }

        protected virtual string ReportSqlErrors(DbException exception) => null;
    }
}
