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

using Npgsql;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System.Data.Common;
using System.Text;

namespace Rhetos.PostgreSql
{
    public class PostgreSqlExecuter : BaseSqlExecuter, ISqlExecuter
    {
        private readonly ISqlUtility _sqlUtility;

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
        public PostgreSqlExecuter(ILogProvider logProvider, IPersistenceTransaction persistenceTransaction, DatabaseOptions databaseOptions, ISqlUtility sqlUtility)
            : base(logProvider, persistenceTransaction, databaseOptions)
        {
            _sqlUtility = sqlUtility;
        }

        protected override string ReportSqlErrors(DbException exception)
        {
            if (exception is PostgresException pgException)
                return ReportSqlError(pgException);
            else
                return null;
        }

        private static string ReportSqlError(PostgresException pgException)
        {
            var report = new StringBuilder();

            void Add(string text, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (report.Length > 0) report.Append(", ");
                    report.Append($"{text}: {value}");
                }
            }

            // See PostgresException.GetMessage() for the standard message format. It contains SqlState, MessageText, Position, Detail.
            // Instead of the standard Message that uses NewLine, here we try to put all in a single line, to improve integration with Visual Studio.
            Add(pgException.SqlState, pgException.MessageText ?? pgException.Message);
            Add("POSITION", pgException.Position == 0 ? null : pgException.Position.ToString());
            Add("DETAIL", pgException.Detail);
            Add("HINT", pgException.Hint);
            Add("ROUTINE", pgException.Routine);
            Add("LINE", pgException.Line);

            return report.ToString();
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
            if (keys.Count == 0)
                return;

            const string dbLockPrefix = "DbLock ";

            string timeoutParameter = wait ? "" : ", @LockTimeout = '0'";

            var sql = new StringBuilder();
            sql.AppendLine("DECLARE @lockResult int;");
            foreach (var key in keys)
            {
                sql.AppendLine($"EXEC @lockResult = sp_getapplock {_sqlUtility.QuoteText(key.SqlName)}, 'Exclusive'{timeoutParameter};");
                sql.AppendLine($"IF @lockResult < 0 BEGIN; RAISERROR({_sqlUtility.QuoteText(dbLockPrefix + key.FullName)}, 16, 10); RETURN; END;");
            }

            try
            {
                this.ExecuteSql(sql.ToString());
            }
            catch (Exception e)
            {
                string lockInfo;

                if (e is FrameworkException fe && fe.InnerException is DbException dbe && dbe.Message.StartsWith(dbLockPrefix))
                {
                    lockInfo = dbe.Message;
                }
                else if (e is FrameworkException fe2 && fe2.InnerException is DbException dbe2 && dbe2.Message.StartsWith("Timeout expired"))
                {
                    lockInfo = dbLockPrefix + keys.First().FullName + (keys.Count > 1 ? $", {keys.Count}" : "");
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

        private static List<(string SqlName, string FullName)> PreprocessResourceNames(IEnumerable<string> resources)
        {
            return resources
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .Select(r => r.ToUpperInvariant()) // Lock resource name is case-sensitive in SQL Server, but this might reduce bugs when locks are related to case insensitive data (e.g. usernames).
                .Select(r => (SqlName: r.LimitWithHash(255), FullName: r)) // SQL Server limits lock resource name to 255.
                .ToList();
        }

        public void ReleaseDbLock(IEnumerable<string> resources)
        {
            var keys = PreprocessResourceNames(resources);
            if (keys.Count == 0)
                return;

            var sql = new StringBuilder();
            sql.AppendLine("DECLARE @lockResult int;");
            foreach (var key in keys)
                sql.AppendLine($@"EXEC sp_releaseapplock {_sqlUtility.QuoteText(key.SqlName)};");

            this.ExecuteSql(sql.ToString());
        }
    }
}
