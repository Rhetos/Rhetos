﻿/*
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

namespace Rhetos.Utilities
{
    [Options("Rhetos:SqlTransactionBatches")]
    public class SqlTransactionBatchesOptions
    {
        public int ReportProgressMs { get; set; } = 60_000; // Report progress each minute by default

        public int MaxJoinedScriptCount { get; set; } = 100;

        public int MaxJoinedScriptSize { get; set; } = 100_000;

        /// <summary>
        /// If true (default), <see cref="ISqlTransactionBatches"/> executes scripts on a new <b>separate database connection</b> (not in the current scope),
        /// and <b>commits transaction immediately</b> (changes will stay in database even if the current scope fails and rolls back).
        /// </summary>
        /// <remarks>
        /// This option can be set to <see langword="false"/> for testing purpose, to achieve test isolation.
        /// </remarks>
        public bool ExecuteOnNewConnection { get; set; } = true;

        public byte ErrorReportCommandsLimit { get; set; } = 20;

        public int ErrorReportScriptSizeLimit { get; set; } = 10_000;
    }
}
