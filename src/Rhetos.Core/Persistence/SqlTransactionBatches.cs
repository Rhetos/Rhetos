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

using Autofac;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    /// <summary>
    /// This class adds additional functionality over ISqlExecuter for executing batches of SQL scripts with custom transaction handling and reporting,
    /// while allowing ISqlExecuter implementations to focus on the database technology.
    /// </summary>
    public class SqlTransactionBatches : ISqlTransactionBatches
    {
        private readonly SqlTransactionBatchesOptions _options;
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly Lazy<ISqlExecuter> _sqlExecuter;
        private readonly PersistenceTransactionOptions _persistenceTransactionOptions;
        private readonly IUserInfo _userInfo;
        private readonly ILogger _logger;
        private readonly IDelayedLogger _delayedLogger;

        public SqlTransactionBatches(
            SqlTransactionBatchesOptions options,
            IUnitOfWorkFactory unitOfWorkFactory,
            Lazy<ISqlExecuter> sqlExecuter,
            PersistenceTransactionOptions persistenceTransactionOptions,
            IUserInfo userInfo,
            ILogProvider logProvider,
            IDelayedLogProvider delayedLogProvider)
        {
            _options = options;
            _unitOfWorkFactory = unitOfWorkFactory;
            _sqlExecuter = sqlExecuter;
            _persistenceTransactionOptions = persistenceTransactionOptions;
            _userInfo = userInfo;
            _logger = logProvider.GetLogger(nameof(SqlTransactionBatches));
            _delayedLogger = delayedLogProvider.GetLogger(nameof(SqlTransactionBatches));
        }

        public void Execute(IEnumerable<SqlBatchScript> sqlScripts)
        {
            var scriptParts = sqlScripts
                .SelectMany(script => script.IsBatch
                    ? SqlUtility.SplitBatches(script.Sql).Select(scriptPart => new SqlBatchScript { Name = script.Name, Sql = scriptPart, IsBatch = false })
                    : new[] { script })
                .Where(script => !string.IsNullOrWhiteSpace(script.Sql));

            var sqlBatches = CsUtility.GroupItemsKeepOrdering(scriptParts, script => SqlUtility.ScriptSupportsTransaction(script.Sql))
                .Select(group => new
                {
                    UseTransaction = group.Key,
                    // The empty NoTransactionTag script is used by the DatabaseGenerator to split transactions.
                    // This is why empty scrips are removed *after* grouping.
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
                        _logger.Info($"Executed {executedCount} / {totalCount} SQL scripts. {remainingTime.TotalMinutes:f2} minutes remaining.");
                        lastReportTime = now;
                    }
                };

                var scriptsWithName = sqlBatch.Scripts
                    .Select(script => string.IsNullOrEmpty(script.Name)
                        ? script.Sql
                        : "--Name: " + script.Name.Replace("\r", " ").Replace("\n", " ") + "\r\n" + script.Sql);

                Execute(
                    (ISqlExecuter sqlExecuter) => sqlExecuter.ExecuteSql(scriptsWithName, initializeProgress, reportProgress),
                    sqlBatch.UseTransaction,
                    sqlBatch.Scripts);

                previousBatchesCount += sqlBatch.Scripts.Count;
            }
        }

        private void Execute(Action<ISqlExecuter> sqlExecuterAction, bool useTransaction, IList<SqlBatchScript> errorContext)
        {
            if (_options.ExecuteOnNewConnection || !useTransaction)
            {
                using (var scope = CreateUnitOfWorkScope(useTransaction))
                {
                    var scopeSqlExecuter = scope.Resolve<ISqlExecuter>();
                    sqlExecuterAction.Invoke(scopeSqlExecuter);
                    CheckTransactionCount(scopeSqlExecuter, useTransaction ? 1 : 0, errorContext);
                    scope.CommitAndClose();
                }
            }
            else
            {
                sqlExecuterAction.Invoke(_sqlExecuter.Value);
            }
        }

        private IUnitOfWorkScope CreateUnitOfWorkScope(bool useTransaction)
        {
            var scopeTransactionOptions = CsUtility.ShallowCopy(_persistenceTransactionOptions);
            scopeTransactionOptions.UseDatabaseTransaction = useTransaction;

            return _unitOfWorkFactory.CreateScope(builder =>
            {
                builder.RegisterInstance(scopeTransactionOptions);
                builder.RegisterInstance(_userInfo);
            });
        }

        private void CheckTransactionCount(ISqlExecuter scopeSqlExecuter, int expectedTranCount, IList<SqlBatchScript> errorContext)
        {
            var tranCount = scopeSqlExecuter.GetTransactionCount();
            if (tranCount != expectedTranCount)
            {
                string msg = "Database transaction state has been unexpectedly modified in SQL commands."
                    + $" Transaction count is {tranCount}, expected value is {expectedTranCount}.";

                if (errorContext != null)
                {
                    var log = new StringBuilder(msg);
                    msg += " See error log for more information.";

                    log.AppendLine($" Executed {errorContext.Count} commands:");
                    for (int i = 0; i < Math.Min(errorContext.Count, _options.ErrorReportCommandsLimit); i++)
                    {
                        var c = errorContext[i];
                        log.AppendLine($"{i}: {(!string.IsNullOrEmpty(c.Name) ? c.Name : c.Sql.Limit(_options.ErrorReportScriptSizeLimit, appendTotalLengthInfo: true))}");
                    }
                    if (errorContext.Count > _options.ErrorReportCommandsLimit)
                        log.AppendLine($"... (total {errorContext.Count} scripts)");

                    _logger.Error(() => log.ToString());
                }

                throw new FrameworkException(msg);
            }
        }

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
