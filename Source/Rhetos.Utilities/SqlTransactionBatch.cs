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
    [Obsolete("Use " + nameof(SqlTransactionBatches) + " instead.")]
    /// <summary>SQL scripts are grouped into batches to handle different transaction usage.</summary>
    public class SqlTransactionBatch : List<string>
    {
        public SqlTransactionBatch(List<string> source) : base(source)
        {
        }

        public bool UseTransacion;

        public static List<SqlTransactionBatch> GroupByTransaction(IEnumerable<string> sqlScripts)
        {
            sqlScripts = sqlScripts.Where(s => !string.IsNullOrWhiteSpace(s.Replace(SqlUtility.NoTransactionTag, "")));
            var batches = CsUtility.GroupItemsKeepOrdering(sqlScripts, SqlUtility.ScriptSupportsTransaction);
            return batches.Select(batch => new SqlTransactionBatch(batch.Items) { UseTransacion = batch.Key }).ToList();
        }
    }
}
