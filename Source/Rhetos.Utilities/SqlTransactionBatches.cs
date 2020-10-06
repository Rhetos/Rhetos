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
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Utilities
{

    /// <summary>
    /// This class adds additional functionality over ISqlExecuter for executing a batch SQL scripts (custom transaction handling and reporting),
    /// while allowing ISqlExecuter implementations to focus on the database technology.
    /// </summary>
    public class SqlTransactionBatches
    {
        private readonly ISqlExecuter _sqlExecuter;
        private readonly SqlTransactionBatchesOptions _options;
        private readonly ILogger _logger;
        private readonly IDelayedLogger _delayedLogger;

        public SqlTransactionBatches(ISqlExecuter sqlExecuter, SqlTransactionBatchesOptions options, ILogProvider logProvider, IDelayedLogProvider delayedLogProvider)
        {
            _sqlExecuter = sqlExecuter;
            _options = options;
            _logger = logProvider.GetLogger(nameof(SqlTransactionBatches));
            _delayedLogger = delayedLogProvider.GetLogger(nameof(SqlTransactionBatches));
        }

        [DebuggerDisplay("{Name ?? CsUtility.Limit(Sql, 100)}")]
        public class SqlScript
        {
            public string Name;
            public string Sql;
            /// <summary>
            /// If the script is a batch, it will be split by a batch separator ("GO") before executing each part separately.
            /// </summary>
            public bool IsBatch;
        };

        /// <summary>
        /// 1. Splits the scripts by the SQL batch delimiter ("GO", for Microsoft SQL Server). See <see cref="SqlUtility.SplitBatches(string)"/>.
        /// 2. Detects and applies the transaction usage tag. See <see cref="SqlUtility.NoTransactionTag"/> and <see cref="SqlUtility.ScriptSupportsTransaction(string)"/>.
        /// 3. Reports progress (Info level) after each minute.
        /// 4. Prefixes each SQL script with a comment containing the script's name.
        /// </summary>
        public void Execute(IEnumerable<SqlScript> sqlScripts)
        {
            var scriptParts = sqlScripts
                .SelectMany(script => script.IsBatch
                    ? SqlUtility.SplitBatches(script.Sql).Select(scriptPart => new SqlScript { Name = script.Name, Sql = scriptPart, IsBatch = false })
                    : new[] { script })
                .Where(script => !string.IsNullOrWhiteSpace(script.Sql));

            var sqlBatches = CsUtility.GroupItemsKeepOrdering(scriptParts, script => SqlUtility.ScriptSupportsTransaction(script.Sql))
                .Select(group => new
                {
                    UseTransaction = group.Key,
                    // The empty NoTransactionTag script is used by the DatabaseGenerator to split transactions.
                    // This is why there scrips are removed *after* grouping 
                    Scripts = group.Items.Where(s => !string.Equals(s.Sql, SqlUtility.NoTransactionTag, StringComparison.Ordinal)).ToList()
                })
                .Where(group => group.Scripts.Count > 0) // Cleanup after removing the empty NoTransactionTag scripts.
                .ToList();

            _logger.Trace(() => "SqlBatches: " + string.Join(", ", sqlBatches.Select(b => $"{(b.UseTransaction ? "tran" : "notran")} {b.Scripts.Count}")));

            int totalCount = sqlBatches.Sum(b => b.Scripts.Count);
            int previousBatchesCount = 0;
            var startTime = DateTime.Now;
            var lastReportTime = startTime;

            foreach (var sqlBatch in sqlBatches)
            {
                IDisposable timeoutWarning = null;

                Func<int, string> sqlScriptDescription = scriptIndex =>
                {
                    var script = sqlBatch.Scripts[scriptIndex];
                    if (!string.IsNullOrEmpty(script.Name))
                        return $" '{script.Name.Trim()}'.";
                    else
                        return $": {script.Sql.Limit(1000).Trim()}";
                };

                Action<int> initializeProgress = scriptIndex =>
                {
                    timeoutWarning = _delayedLogger.PerformanceWarning(() => $"Executing SQL script{sqlScriptDescription(scriptIndex)}");
                };

                Action<int> reportProgress = scriptIndex =>
                {
                    if (timeoutWarning != null)
                    {
                        timeoutWarning.Dispose();
                        timeoutWarning = null;
                    }

                    var now = DateTime.Now;
                    int executedCount = previousBatchesCount + scriptIndex + 1;

                    if (now.Subtract(lastReportTime).TotalMilliseconds > _options.ReportProgressMs
                        && executedCount < totalCount) // No need to report progress if the work is done.
                    {
                        double estimatedTotalMs = now.Subtract(startTime).TotalMilliseconds / executedCount * totalCount;
                        var remainingTime = startTime.AddMilliseconds(estimatedTotalMs).Subtract(now);
                        _logger.Info($"Executed {executedCount} / {totalCount} SQL scripts. {(remainingTime.TotalMinutes).ToString("f2")} minutes remaining.");
                        lastReportTime = now;
                    }
                };

                var scriptsWithName = sqlBatch.Scripts
                    .Select(script => string.IsNullOrEmpty(script.Name)
                        ? script.Sql
                        : "--Name: " + script.Name.Replace("\r", " ").Replace("\n", " ") + "\r\n" + script.Sql);

                _sqlExecuter.ExecuteSql(scriptsWithName, sqlBatch.UseTransaction, initializeProgress, reportProgress);

                previousBatchesCount += sqlBatch.Scripts.Count;
            }
        }

        /// <summary>
        /// Combines multiple SQL scripts to a single one.
        /// Use only for DML SQL scripts to avoid SQL syntax errors on DDL commands that need to stay in a separate scripts.
        /// Scripts are joined to groups, respecting the configuration settings for the limit on total joined script size and count.
        /// </summary>
        /// <returns></returns>
        public List<string> JoinScripts(IEnumerable<string> scripts)
        {
            var joinedScripts = new List<string>();
            var currentBatch = new List<string>();
            int currentBatchSize = 0;

            foreach (var script in scripts)
            {
                if (currentBatch.Count > 0 &&
                    (currentBatch.Count + 1 > _options.MaxJoinedScriptCount
                    || currentBatchSize + 2 + script.Length > _options.MaxJoinedScriptSize))
                {
                    joinedScripts.Add(string.Join("\r\n", currentBatch));
                    currentBatch.Clear();
                    currentBatchSize = 0;
                }

                currentBatch.Add(script);
                currentBatchSize += script.Length + (currentBatch.Count > 1 ? 2 : 0);
            }

            if (currentBatch.Count > 0)
                joinedScripts.Add(string.Join("\r\n", currentBatch));

            return joinedScripts;
        }
    }
}
