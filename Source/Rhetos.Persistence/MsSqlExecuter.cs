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
using System.Configuration;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.Diagnostics;

namespace Rhetos.Persistence
{
    public class MsSqlExecuter : ISqlExecuter
    {
        private readonly string _connectionString;
        private readonly IUserInfo _userInfo;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IPersistenceTransaction _persistenceTransaction;

        /// <summary>
        /// This constructor is typically used in deployment time, when persistence transaction does not exist.
        /// </summary>
        public MsSqlExecuter(ConnectionString connectionString, ILogProvider logProvider, IUserInfo userInfo)
            : this(connectionString, logProvider, userInfo, null)
        {
        }

        /// <summary>
        /// This constructor is typically used in run-time, when persistence transaction is active, in order to execute
        /// the SQL queries in the same transaction.
        /// </summary>
        public MsSqlExecuter(ConnectionString connectionString, ILogProvider logProvider, IUserInfo userInfo, IPersistenceTransaction persistenceTransaction)
        {
            _connectionString = connectionString;
            _userInfo = userInfo;
            _logger = logProvider.GetLogger("MsSqlExecuter");
            _performanceLogger = logProvider.GetLogger("Performance");
            _persistenceTransaction = persistenceTransaction;
        }

        public void ExecuteSql(IEnumerable<string> commands, bool useTransaction)
        {
            ExecuteSql(commands, useTransaction, null, null);
        }

        public void ExecuteSql(IEnumerable<string> commands, bool useTransaction, Action<int> beforeExecute, Action<int> afterExecute)
        {
            CsUtility.Materialize(ref commands);

            _logger.Trace(() => "Executing " + commands.Count() + " commands" + (useTransaction ? "" : " without transaction") + ".");

            SafeExecuteCommand(
                com =>
                {
                    int count = 0;
                    foreach (var sql in commands)
                    {
                        count++;
                        if (sql == null)
                            throw new FrameworkException("SQL script is null.");

                        _logger.Trace(() => "Executing command: " + sql);

                        if (string.IsNullOrWhiteSpace(sql))
                            continue;

                        var sw = Stopwatch.StartNew();
                        try
                        {
                            com.CommandText = sql;

                            beforeExecute?.Invoke(count - 1);
                            com.ExecuteNonQuery();
                        }
                        finally
                        {
                            afterExecute?.Invoke(count - 1);
                            LogPerformanceIssue(sw, sql);
                        }
                    }
                    CheckTransactionState(useTransaction, com, commands);
                },
                useTransaction);
        }

        private static void CheckTransactionState(bool useTransaction, DbCommand com, IEnumerable<string> commands)
        {
            if (useTransaction)
            {
                try
                {
                    com.CommandText = @"IF @@TRANCOUNT <> 1 RAISERROR('Transaction count is %d, expected value is 1.', 16, 10, @@TRANCOUNT)";
                    com.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    throw new FrameworkException(
                        "The SQL scripts have changed transaction level. " + ReportSqlScripts(commands, 1000),
                        ex);
                }
            }
        }

        private static string ReportSqlScripts(IEnumerable<string> commands, int maxLength)
        {
            var report = new StringBuilder();
            report.Append($"Executing {commands.Count()} scripts:");

            foreach (var sql in commands)
            {
                report.Append("\r\n");
                report.Append(sql.Limit(maxLength, true));
                if (report.Length > maxLength)
                {
                    report.Append("\r\n...");
                    break;
                }
            }

            return report.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void ExecuteReader(string commandText, Action<DbDataReader> action)
        {
            _logger.Trace(() => "Executing reader: " + commandText);

            SafeExecuteCommand(
                sqlCommand =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        sqlCommand.CommandText = commandText;
                        var dataReader = sqlCommand.ExecuteReader();
                        while (!dataReader.IsClosed && dataReader.Read())
                            action(dataReader);
                        dataReader.Close();
                    }
                    finally
                    {
                        LogPerformanceIssue(sw, commandText);
                    }
                },
                _persistenceTransaction != null);
        }

        private void SafeExecuteCommand(Action<DbCommand> action, bool useTransaction)
        {
            bool createOwnConnection = _persistenceTransaction == null || !useTransaction;

            var connection = createOwnConnection ? new SqlConnection(_connectionString) : _persistenceTransaction.Connection;

            try
            {
                DbTransaction createdTransaction = null;
                DbCommand command;

                try
                {
                    if (createOwnConnection)
                        connection.Open();

                    command = connection.CreateCommand();
                    command.CommandTimeout = SqlUtility.SqlCommandTimeout;
                    if (createOwnConnection)
                    {
                        if (useTransaction)
                        {
                            createdTransaction = connection.BeginTransaction();
                            command.Transaction = createdTransaction;
                        }
                    }
                    else
                        if (useTransaction)
                            command.Transaction = _persistenceTransaction.Transaction;

                    if (createOwnConnection)
                        SetContextInfo(command);
                }
                catch (SqlException ex)
                {
                    SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(_connectionString);
                    string msg = string.Format(CultureInfo.InvariantCulture,
                            "Could not connect to server '{0}', database '{1}' using {2}.",
                                csb.DataSource,
                                csb.InitialCatalog,
                                csb.IntegratedSecurity ? "integrated security account " + Environment.UserName : "SQL login '" + csb.UserID + "'");

                    _logger.Error("{0} {1}", msg, ex);
                    throw new FrameworkException(msg, ex);
                }

                try
                {
                    action(command);
                    if (createdTransaction != null)
                        createdTransaction.Commit();
                }
                catch (SqlException ex)
                {
                    string msg = "SqlException has occurred" + ReportSqlName(command) + ":\r\n" + ReportSqlErrors(ex);
                    if (command != null && !string.IsNullOrWhiteSpace(command.CommandText))
                        _logger.Error("Unable to execute SQL query:\r\n" + command.CommandText);

                    _logger.Error("{0} {1}", msg, ex);
                    throw new FrameworkException(msg, ex);
                }
            }
            finally
            {
                if (createOwnConnection)
                    if (connection != null)
                        ((IDisposable)connection).Dispose();
            }
        }

        private string ReportSqlName(DbCommand command)
        {
            const string namePrefix = "--Name: ";
            if (command?.CommandText?.StartsWith(namePrefix) == true)
                return " in '" + CsUtility.FirstLine(command.CommandText).Substring(namePrefix.Length).Limit(1000, "...") + "'";
            else
                return "";
        }

        private void LogPerformanceIssue(Stopwatch sw, string sql)
        {
            if (sw.Elapsed >= LoggerHelper.SlowEvent) // Avoid flooding the performance trace log.
                _performanceLogger.Write(sw, () => "MsSqlExecuter: " + sql.Limit(50000, true));
            else
                sw.Restart(); // _performanceLogger.Write would restart the stopwatch.
        }

        private void SetContextInfo(DbCommand sqlCommand)
        {
            if (_userInfo.IsUserRecognized)
            {
                sqlCommand.CommandText = MsSqlUtility.SetUserContextInfoQuery(_userInfo);
                sqlCommand.ExecuteNonQuery();
            }
        }

        private static string ReportSqlErrors(SqlException ex)
        {
            StringBuilder sb = new StringBuilder();
            SqlError[] errors = new SqlError[ex.Errors.Count];
            ex.Errors.CopyTo(errors, 0);
            foreach (var err in errors.OrderBy(e => e.LineNumber))
            {
                if (err.Class > 0)
                {
                    sb.Append("Msg ").Append(err.Number);
                    sb.Append(", Level ").Append(err.Class);
                    sb.Append(", State ").Append(err.State);
                    if (!string.IsNullOrEmpty(err.Procedure))
                        sb.Append(", Procedure ").Append(err.Procedure);
                    sb.Append(", Line ").Append(err.LineNumber);
                    sb.AppendLine();
                }
                sb.AppendLine(err.Message);
            }

            return sb.ToString();
        }
    }
}
