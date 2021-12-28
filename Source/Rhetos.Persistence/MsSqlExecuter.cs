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
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rhetos.Persistence
{
    public class MsSqlExecuter : BaseSqlExecuter, ISqlExecuter
    {
        private readonly string _connectionString;

        /// <summary>
        /// This constructor is typically used in deployment time, when shared persistence transaction does not exist.
        /// It results with each Execute command call creating and committing its own transaction.
        /// </summary>
        public MsSqlExecuter(ConnectionString connectionString, ILogProvider logProvider, IUserInfo userInfo)
            : this(connectionString, logProvider, userInfo, null)
        {
        }

        /// <summary>
        /// This constructor is typically used in run-time, when shared persistence transaction is active,
        /// in order to execute the SQL queries in the same transaction.
        /// It results with each Execute command call participating in a shared transaction that will be committed
        /// at the end of the lifetime scope (for example, at the end of the web request).
        /// The exception here is ExecuteSql command called with useTransaction=false, which is not recommended at standard application runtime.
        /// </summary>
        public MsSqlExecuter(ConnectionString connectionString, 
            ILogProvider logProvider, 
            IUserInfo userInfo, 
            IPersistenceTransaction persistenceTransaction) 
            : base(logProvider, userInfo, persistenceTransaction)
        {
            _connectionString = connectionString;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void ExecuteReader(string commandText, Action<DbDataReader> action)
        {
            _logger.Trace(() => "Executing reader: " + commandText);

            SafeExecuteCommand(
                sqlCommand =>
                {
                    sqlCommand.CommandText = commandText;
                    ExecuteReader(sqlCommand, action);
                },
                _persistenceTransaction != null);
        }

        private void SafeExecuteCommand(Action<DbCommand> action, bool useTransaction)
        {
            bool createOwnConnection = _persistenceTransaction == null || !useTransaction;
            DbConnection connection = null;

            try
            {
#pragma warning disable CA2000 // Dispose objects before losing scope. This method has a custom SqlConnection disposal mechanism.
                connection = createOwnConnection ? new SqlConnection(_connectionString) : _persistenceTransaction.Connection;
#pragma warning restore CA2000 // Dispose objects before losing scope
                DbTransaction createdTransaction = null;
                DbCommand command;

                try
                {
                    if (createOwnConnection)
                        connection.Open();

                    command = connection.CreateCommand();
                    command.CommandTimeout = SqlUtility.SqlCommandTimeout;
                    if (useTransaction)
                    {
                        if (createOwnConnection)
                        {
                            createdTransaction = connection.BeginTransaction();
                            command.Transaction = createdTransaction;
                        }
                        else
                            command.Transaction = _persistenceTransaction.Transaction;
                    }

                    if (createOwnConnection)
                        SetContextInfo(command.Connection, command.Transaction);
                }
                catch (SqlException ex)
                {
                    SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(_connectionString);
                    string secutiryInfo = csb.IntegratedSecurity ? $"integrated security account {Environment.UserName}" : $"SQL login '{csb.UserID}'";
                    string msg = $"Could not connect to server '{csb.DataSource}', database '{csb.InitialCatalog}' using {secutiryInfo}.";
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
                    if (command != null && !string.IsNullOrWhiteSpace(command.CommandText))
                        _logger.Error("Unable to execute SQL query:\r\n" + command.CommandText.Limit(1000000));

                    string msg = $"{ex.GetType().Name} has occurred{ReportSqlName(command)}: {ReportSqlErrors(ex)}";
                    throw new FrameworkException(msg, ex);
                }
            }
            finally
            {
                if (createOwnConnection && connection != null)
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

        private void SetContextInfo(DbConnection connection, DbTransaction transaction)
        {
            if (_userInfo.IsUserRecognized)
            {
                var sqlCommand = MsSqlUtility.SetUserContextInfoQuery(_userInfo);
                sqlCommand.Connection = connection;
                sqlCommand.Transaction = transaction;
                sqlCommand.ExecuteNonQuery();
            }
        }

        private static string ReportSqlErrors(SqlException exception)
        {
            SqlError[] errors = new SqlError[exception.Errors.Count];
            exception.Errors.CopyTo(errors, 0);
            // If there is only one simple error, it will be reported in a single line (most likely)
            // to improve integration with Visual Studio.
            return string.Join(Environment.NewLine, errors.Select(ReportSqlError));
        }

        private static string ReportSqlError(SqlError e)
        {
            string errorProcedure = !string.IsNullOrEmpty(e.Procedure)
                ? $", Procedure {e.Procedure}"
                : "";
            string errorMetadata = e.Class > 0
                ? $"Msg {e.Number}, Level {e.Class}, State {e.State}{errorProcedure}, Line {e.LineNumber}: "
                : "";
            return errorMetadata + e.Message;
        }
    }
}
