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
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Rhetos.Persistence
{
    public class OracleSqlExecuter : BaseSqlExecuter, ISqlExecuter
    {
        public OracleSqlExecuter(
            ILogProvider logProvider, 
            IPersistenceTransaction persistenceTransaction,
            DatabaseOptions databaseOptions) 
            : base(logProvider, persistenceTransaction, databaseOptions)
        {
        }

        protected override string ReportSqlErrors(DbException exception)
        {
            if (exception is OracleException ex)
            {
                StringBuilder sb = new StringBuilder();

                if (ex.Number == 911)
                    sb.AppendLine("Check that you are not using ';' at the end of the command's SQL query.");

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
            else
                return null;
        }

        public int GetTransactionCount()
        {
            // Currently not implemented; returning expected value.
            return _persistenceTransaction.Transaction != null ? 1 : 0;
        }

        public void GetDbLock(IEnumerable<string> resources, bool wait = true)
        {
            throw new System.NotImplementedException();
        }

        public void ReleaseDbLock(IEnumerable<string> resources)
        {
            throw new System.NotImplementedException();
        }
    }
}
