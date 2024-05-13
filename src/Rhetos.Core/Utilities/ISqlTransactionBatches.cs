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

namespace Rhetos.Utilities
{
    /// <summary>
    /// This class adds additional functionality over ISqlExecuter for executing batches of SQL scripts with custom transaction handling and reporting,
    /// while allowing ISqlExecuter implementations to focus on the database technology.
    /// </summary>
    public interface ISqlTransactionBatches
    {
        /// <summary>
        /// The scripts are executed in a <b>separate database connection</b> (not in current scope),
        /// and <b>committed immediately</b> (changes will stay in database even if the current scope fails and rolls back).
        /// </summary>
        /// <remarks>
        /// This method:
        /// <list type="number">
        /// <item>Splits the scripts by the SQL batch delimiter ("GO", for Microsoft SQL Server). See <see cref="SqlUtility.SplitBatches"/>.</item>
        /// <item>Detects and applies the transaction usage tag. See <see cref="SqlUtility.NoTransactionTag"/> and <see cref="SqlUtility.ScriptSupportsTransaction"/>.</item>
        /// <item>Reports progress (Info level) after each minute.</item>
        /// <item>Prefixes each SQL script with a comment containing the script's name, for better diagnostics.</item>
        /// <item>Checks if transaction is in correct state after executed SQL commands, see <see cref="ISqlExecuter.CheckTransactionState"/>.</item>
        /// </list>
        /// </remarks>
        void Execute(IEnumerable<SqlBatchScript> sqlScripts);

        /// <summary>
        /// Combines multiple SQL scripts to a single one.
        /// Use only for DML SQL scripts to avoid SQL syntax errors on DDL commands that need to stay in a separate scripts.
        /// Scripts are joined to groups, respecting the configuration settings for the limit on total joined script size and count.
        /// </summary>
        List<string> JoinScripts(IEnumerable<string> scripts);
    }
}