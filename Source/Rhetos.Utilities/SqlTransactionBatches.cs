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
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Utilities
{
    /// <summary>
    /// This class add additional functionality over ISqlExecuter for executing a batch SQL scripts (custom transaction handling and reporting),
    /// while allowing ISqlExecuter implementations to focus on the database technology.
    /// </summary>
    public class SqlTransactionBatches
    {
        private readonly ISqlExecuter _sqlExecuter;
        private readonly int _reportDelayMs;
        private readonly ILogger _logger;

        public SqlTransactionBatches(ISqlExecuter sqlExecuter, IConfiguration configuration, ILogProvider logProvider)
        {
            _sqlExecuter = sqlExecuter;
            _reportDelayMs = configuration.GetInt("SqlExecuter.ReportProgressMs", 1000 * 60).Value; // Report progress each minute by default
            _logger = logProvider.GetLogger(nameof(SqlTransactionBatches));
        }

        public class SqlScript
        {
            public string Name;
            public string Sql;
        };

        /// <summary>
        /// 1. Splits the scripts by the SQL batch delimiter ("GO", for Microsoft SQL Server). See <see cref="SqlUtility.SplitBatches(string)"/>.
        /// 2. Detects and applies the transaction usage tag. See <see cref="SqlUtility.NoTransactionTag"/> and <see cref="SqlUtility.ScriptSupportsTransaction(string)"/>.
        /// 3. Reports progress (Info level) after each minute.
        /// </summary>
        public void Execute(IEnumerable<string> sqlScripts)
        {
            Execute(sqlScripts.Select(sql => new SqlScript { Name = null, Sql = sql }));
        }

        /// <summary>
        /// 1. Splits the scripts by the SQL batch delimiter ("GO", for Microsoft SQL Server). See <see cref="SqlUtility.SplitBatches(string)"/>.
        /// 2. Detects and applies the transaction usage tag. See <see cref="SqlUtility.NoTransactionTag"/> and <see cref="SqlUtility.ScriptSupportsTransaction(string)"/>.
        /// 3. Reports progress (Info level) after each minute.
        /// 4. Prefixes each SQL script with a comment containing the script's name.
        /// </summary>
        public void Execute(IEnumerable<SqlScript> sqlScripts)
        {
            var scriptParts = sqlScripts
                .SelectMany(script => SqlUtility.SplitBatches(script.Sql)
                    .Select(scriptPart => new { script.Name, Sql = scriptPart }))
                .Where(script => !string.IsNullOrWhiteSpace(script.Sql.Replace(SqlUtility.NoTransactionTag, "")))
                .ToList();

            var sqlBatches = CsUtility.GroupItemsKeepOrdering(scriptParts, script => SqlUtility.ScriptSupportsTransaction(script.Sql))
                .Select(group => new
                {
                    UseTransaction = group.Key,
                    Scripts = group.Items
                        .Select(script => string.IsNullOrEmpty(script.Name)
                            ? script.Sql
                            : SqlUtility.Comment("Name: " + script.Name) + "\r\n" + script.Sql)
                        .ToList()
                }).ToList();

            int totalCount = sqlBatches.Sum(b => b.Scripts.Count);
            int previousBatchesCount = 0;
            var startTime = DateTime.Now;
            var lastReportTime = startTime;

            foreach (var sqlBatch in sqlBatches)
            {
                Action<int> reportProgress = currentBatchCount =>
                {
                    var now = DateTime.Now;
                    int executedCount = previousBatchesCount + currentBatchCount + 1;

                    if (now.Subtract(lastReportTime).TotalMilliseconds > _reportDelayMs
                        && executedCount < totalCount) // No need to report progress if the work is done.
                    {
                        double estimatedTotalMs = now.Subtract(startTime).TotalMilliseconds / executedCount * totalCount;
                        var remainingTime = startTime.AddMilliseconds(estimatedTotalMs).Subtract(now);
                        _logger.Info($"Executed {executedCount} / {totalCount} SQL scripts. {(remainingTime.TotalMinutes).ToString("f2")} minutes remaining.");
                        lastReportTime = now;
                    }
                };

                _sqlExecuter.ExecuteSql(sqlBatch.Scripts, sqlBatch.UseTransaction, null, reportProgress);

                previousBatchesCount += sqlBatch.Scripts.Count;
            }
        }
    }
}
