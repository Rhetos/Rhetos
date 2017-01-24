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
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    /// <summary>SQL scripts are grouped into batches to handle different transaction usage.</summary>
    public class SqlTransactionBatch : List<string>
    {
        public bool UseTransacion;

        public static List<SqlTransactionBatch> GroupByTransaction(IEnumerable<string> sqlScripts)
        {
            var batches = new List<SqlTransactionBatch>();
            SqlTransactionBatch currentBatch = null;

            foreach (string sqlScript in sqlScripts)
            {
                bool scriptUsesTransaction = !sqlScript.StartsWith(SqlUtility.NoTransactionTag);
                if (currentBatch == null || currentBatch.UseTransacion != scriptUsesTransaction)
                {
                    currentBatch = new SqlTransactionBatch { UseTransacion = scriptUsesTransaction };
                    batches.Add(currentBatch);
                }

                if (!string.IsNullOrWhiteSpace(sqlScript.Replace(SqlUtility.NoTransactionTag, "")))
                    currentBatch.Add(sqlScript);
            }

            return batches.Where(batch => batch.Count > 0).ToList();
        }
    }
}
