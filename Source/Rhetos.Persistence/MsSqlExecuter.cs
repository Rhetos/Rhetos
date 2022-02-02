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
    }
}
