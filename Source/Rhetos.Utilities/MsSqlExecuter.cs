/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Utilities
{
    public class MsSqlExecuter : ISqlExecuter
    {
        private readonly string _connectionString;
        private readonly IUserInfo _userInfo;
        private readonly ILogger _logger;

        public MsSqlExecuter(ConnectionString connectionString, ILogProvider logProvider, IUserInfo userInfo)
        {
            _connectionString = connectionString;
            _userInfo = userInfo;
            _logger = logProvider.GetLogger("MsSqlExecuter");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void ExecuteSql(IEnumerable<string> commands)
        {
            _logger.Trace("Executing {0} commands.", commands.Count());

            SafeExecuteCommand(
                com =>
                {
                    foreach (var sql in commands)
                    {
                        if (sql == null)
                            throw new FrameworkException("SQL script is null.");

                        _logger.Trace("Executing command: {0}", sql);

                        if (string.IsNullOrWhiteSpace(sql))
                            continue;

                        com.CommandText = sql;
                        com.ExecuteNonQuery();

                        try
                        {
                            com.CommandText = @"IF @@TRANCOUNT <> 1 RAISERROR('Transaction count is %d, expected value is 1.', 16, 10, @@TRANCOUNT)";
                            com.ExecuteNonQuery();
                        }
                        catch (SqlException ex)
                        {
                            throw new FrameworkException(
                                string.Format(CultureInfo.InvariantCulture,
                                    "SQL script has changed transaction level.{0}{1}",
                                        Environment.NewLine, 
                                        sql.Substring(0, Math.Min(1000, sql.Length))),
                                ex);
                        }
                    }
                },
                true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void ExecuteReader(string command, Action<DbDataReader> action)
        {
            _logger.Trace("Executing reader: {0}", command);

            SafeExecuteCommand(
                com =>
                {
                    com.CommandText = command;
                    var dr = com.ExecuteReader();
                    while (dr.Read())
                        action(dr);
                    dr.Close();
                }, 
                false);
        }

        private void SafeExecuteCommand(Action<SqlCommand> action, bool useTransaction)
        {
            using (var dbConnection = new SqlConnection(_connectionString))
            {
                SqlTransaction tran = null;
                SqlCommand com;

                try
                {
                    dbConnection.Open();
                    com = dbConnection.CreateCommand();
                    com.CommandTimeout = SqlUtility.SqlCommandTimeout;
                    if (useTransaction)
                    {
                        tran = dbConnection.BeginTransaction();
                        com.Transaction = tran;
                    }

                    SetContextInfo(com);
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
                    action(com);
                    if (tran != null)
                        tran.Commit();
                }
                catch (SqlException ex)
                {
                    string msg = "SqlException has occurred:\r\n" + ReportSqlErrors(ex);
                    if (com != null && !string.IsNullOrWhiteSpace(com.CommandText))
                        _logger.Error("Unable to execute SQL query:\r\n" + com.CommandText);

                    _logger.Error("{0} {1}", msg, ex);
                    throw new FrameworkException(msg, ex);
                }
            }
        }

        private void SetContextInfo(SqlCommand sqlCommand)
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
