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

using Oracle.ManagedDataAccess.Client;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Rhetos.Persistence
{
    public class OracleSqlExecuter : BaseSqlExecuter, ISqlExecuter
    {
        private readonly string _connectionString;
        private readonly IUserInfo _userInfo;

        public OracleSqlExecuter(ConnectionString connectionString, 
            ILogProvider logProvider, 
            IUserInfo userInfo, 
            IPersistenceTransaction persistenceTransaction) 
            : base(logProvider, persistenceTransaction)
        {
            _connectionString = connectionString;
            _userInfo = userInfo;
        }

        public void ExecuteSql(IEnumerable<string> commands)
        {
            ExecuteSql(commands, null, null);
        }

        public void ExecuteSql(IEnumerable<string> commands, Action<int> beforeExecute, Action<int> afterExecute)
        {
            _logger.Trace("Executing {0} commands.", commands.Count());

            if (commands.Any(sql => sql == null))
                throw new FrameworkException("SQL script object is null.");

            SafeExecuteCommand(
                com =>
                {
                    foreach (var sql in commands)
                    {
                        _logger.Trace("Executing command: {0}", sql);

                        com.CommandText = sql;
                        com.ExecuteNonQuery();
                    }
                });
        }

        public void ExecuteReader(string command, Action<DbDataReader> action)
        {
            _logger.Trace("Executing reader: {0}", command);

            SafeExecuteCommand(
                com =>
                {
                    com.CommandText = command;
                    ExecuteReader(com, action);
                });
        }

        private void SafeExecuteCommand(Action<OracleCommand> action)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                OracleTransaction transaction = null;
                OracleCommand com;

                try
                {
                    connection.Open();
                    OracleSqlUtility.SetSqlUserInfo(connection, _userInfo);

                    transaction = connection.BeginTransaction();
                    com = connection.CreateCommand();
                    com.CommandTimeout = SqlUtility.SqlCommandTimeout;
                    com.Transaction = transaction;
                }
                catch (OracleException ex)
                {
                    if (transaction != null)
                        transaction.Rollback();

                    var csb = new OracleConnectionStringBuilder(_connectionString);
                    string msg = string.Format(CultureInfo.InvariantCulture, "Could not connect to data source '{0}', userID '{1}'.", csb.DataSource, csb.UserID);
                    _logger.Error(msg);
                    _logger.Error(ex.ToString());
                    throw new FrameworkException(msg, ex);
                }

                try
                {
                    var setNationalLanguage = OracleSqlUtility.SetNationalLanguageQuery();
                    if (!string.IsNullOrEmpty(setNationalLanguage))
                    {
                        _logger.Trace("Setting national language: {0}", SqlUtility.NationalLanguage);
                        com.CommandText = setNationalLanguage;
                        com.ExecuteNonQuery();
                    }

                    action(com);
                    transaction.Commit();
                }
                catch (OracleException ex)
                {
                    if (com != null && !string.IsNullOrWhiteSpace(com.CommandText))
                        _logger.Error("Unable to execute SQL query:\r\n" + com.CommandText);

                    string msg = "OracleException has occurred:\r\n" + ReportSqlErrors(ex);
                    if (ex.Number == 911)
                        msg += "\r\nCheck that you are not using ';' at the end of the command's SQL query.";
                    _logger.Error(msg);
                    _logger.Error(ex.ToString());
                    throw new FrameworkException(msg, ex);
                }
                finally
                {
                    TryRollback(transaction);
                }
            }
        }

        private static void TryRollback(OracleTransaction transaction)
        {
            try
            {
                if (transaction != null)
                    transaction.Rollback();
            }
            catch
            {
                // Error on rollback can be ignored.
                // It would probably result from another earlier error that closed the transaction,
                // so any exception here would just add noise to the original exception report.
            }
        }

        private static string ReportSqlErrors(OracleException ex)
        {
            StringBuilder sb = new StringBuilder();

            OracleError[] errors = new OracleError[ex.Errors.Count];
            ex.Errors.CopyTo(errors, 0);
            foreach (var err in errors)
            {
                sb.Append(err.Message);
                sb.Append(", ErrorNumber ").Append(err.Number);
                if (!string.IsNullOrEmpty(err.Procedure))
                    sb.Append(", Procedure ").Append(err.Procedure);
                if (!string.IsNullOrEmpty(err.Source))
                    sb.Append(", Source: ").Append(err.Source);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public int GetTransactionCount()
        {
            // Currently not implemented; returning expected value.
            return _persistenceTransaction.Transaction != null ? 1 : 0;
        }
    }
}
