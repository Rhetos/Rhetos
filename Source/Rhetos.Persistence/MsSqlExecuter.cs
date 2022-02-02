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

namespace Rhetos.Persistence
{
    public class MsSqlExecuter : BaseSqlExecuter, ISqlExecuter
    {
        /// <summary>
        /// In a typical unit of work (a web request, e.g.), a shared persistence transaction is active.
        /// It results with each Execute command call participating in a shared transaction that will be committed
        /// at the end of the lifetime scope (for example, at the end of the web request).
        /// </summary>
        /// <remarks>
        /// At deployment time, any code that needs ISqlExecuter should manually create a unit-of-work scope
        /// (see IUnitOfWorkFactory), with registered IPersistenceTransaction implementation,
        /// and then resolve ISqlExecuter from the scope.
        /// </remarks>
        public MsSqlExecuter(ILogProvider logProvider, IPersistenceTransaction persistenceTransaction, DatabaseOptions databaseOptions)
            : base(logProvider, persistenceTransaction, databaseOptions)
        {
        }

        public void ExecuteSql(IEnumerable<string> commands)
        {
            ExecuteSql(commands, null, null);
        }

        public void ExecuteSql(IEnumerable<string> commands, Action<int> beforeExecute, Action<int> afterExecute)
        {
            CsUtility.Materialize(ref commands);

            _logger.Trace(() => "Executing " + commands.Count() + " commands.");

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

                        com.CommandText = sql;
                        beforeExecute?.Invoke(count - 1);
                        ExecuteSql(com);
                        afterExecute?.Invoke(count - 1);
                    }
                });
        }


        public void ExecuteReader(string commandText, Action<DbDataReader> action)
        {
            _logger.Trace(() => "Executing reader: " + commandText);

            SafeExecuteCommand(
                sqlCommand =>
                {
                    sqlCommand.CommandText = commandText;
                    ExecuteReader(sqlCommand, action);
                });
        }

        private void SafeExecuteCommand(Action<DbCommand> action)
        {
            DbCommand command = _persistenceTransaction.Connection.CreateCommand();
            command.Transaction = _persistenceTransaction.Transaction;
            command.CommandTimeout = _databaseOptions.SqlCommandTimeout;

            try
            {
                action(command);
            }
            catch (SqlException ex)
            {
                if (command != null && !string.IsNullOrWhiteSpace(command.CommandText))
                    _logger.Error("Unable to execute SQL query:\r\n" + command.CommandText.Limit(1_000_000));

                string msg = $"{ex.GetType().Name} has occurred{ReportSqlName(command)}: {ReportSqlErrors(ex)}";
                throw new FrameworkException(msg, ex);
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

        public int GetTransactionCount()
        {
            using var command = _persistenceTransaction.Connection.CreateCommand();
            command.Transaction = _persistenceTransaction.Transaction;
            command.CommandText = "SELECT @@TRANCOUNT";
            return (int)command.ExecuteScalar();
        }
    }
}
