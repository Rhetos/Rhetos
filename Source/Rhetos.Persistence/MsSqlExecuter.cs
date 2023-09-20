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
using System.Reflection.Metadata;
using System.Text;

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

        protected override string ReportSqlErrors(DbException exception)
        {
            if (exception is SqlException sqlException)
            {
                SqlError[] errors = new SqlError[sqlException.Errors.Count];
                sqlException.Errors.CopyTo(errors, 0);
                // If there is only one simple error, it will be reported in a single line (most likely)
                // to improve integration with Visual Studio.
                return string.Join(Environment.NewLine, errors.Select(ReportSqlError));
            }
            else
                return null;
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

        public void GetDbLock(IEnumerable<string> resources, bool wait = true)
        {
            var keys = PreprocessResourceNames(resources);
            if (!keys.Any())
                return;

            const string dbLockPrefix = "DbLock ";

            string timeoutParameter = wait ? "" : ", @LockTimeout = '0'";

            var sql = new StringBuilder();
            sql.AppendLine("DECLARE @lockResult int;");
            foreach (var key in keys)
            {
                sql.AppendLine($"EXEC @lockResult = sp_getapplock {SqlUtility.QuoteText(key.SqlName)}, 'Exclusive'{timeoutParameter};");
                sql.AppendLine($"IF @lockResult < 0 BEGIN; RAISERROR({SqlUtility.QuoteText(dbLockPrefix + key.FullName)}, 16, 10); RETURN; END;");
            }

            try
            {
                this.ExecuteSql(sql.ToString());
            }
            catch (Exception e)
            {
                // Resursi su trenutačno zauzeti, pokušajte ponovno.

                string lockInfo;

                if (e is FrameworkException fe && fe.InnerException is DbException dbe && dbe.Message.StartsWith(dbLockPrefix))
                {
                    lockInfo = dbe.Message;
                }
                else if (e is FrameworkException fe2 && fe2.InnerException is DbException dbe2 && dbe2.Message.StartsWith("Timeout expired"))
                {
                    lockInfo = dbLockPrefix + keys.First().FullName + (keys.Count() > 1 ? $", {keys.Count()}" : "");
                }
                else
                {
                    lockInfo = e.GetType().Name + " " + DateTime.Now.ToString("s");
                    _logger.Warning(e.ToString);
                }

                _logger.Info(() => $"GetDbLock: The resource you are trying to access is currently unavailable. Resource name: '{lockInfo}'");

                throw new UserException("The resource you are trying to access is currently unavailable. Please try again later.", e);
            }
        }

        private static IEnumerable<(string SqlName, string FullName)> PreprocessResourceNames(IEnumerable<string> resources)
        {
            return resources
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .Select(r => r.ToUpperInvariant()) // Lock resource name is case-sensitive in SQL Server, but this might reduce bugs when locks are related to case insensitive data (e.g. usernames).
                .Select(r => (SqlName: CsUtility.LimitWithHash(r, 255), FullName: r)) // SQL Server limits lock resource name to 255.
                .ToList();
        }

        public void ReleaseDbLock(IEnumerable<string> resources)
        {
            var keys = PreprocessResourceNames(resources);
            if (!keys.Any())
                return;

            var sql = new StringBuilder();
            sql.AppendLine("DECLARE @lockResult int;");
            foreach (var key in keys)
                sql.AppendLine($@"EXEC sp_releaseapplock {SqlUtility.QuoteText(key.SqlName)};");

            this.ExecuteSql(sql.ToString());
        }
    }
}
