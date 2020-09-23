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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Updates the target database structure.
    /// Executes DDL SQL scripts based on the difference between the current generated database model (from DSL scripts)
    /// and the previously created objects in database (from metadata in table Rhetos.AppliedConcept).
    /// </summary>
    public class DatabaseGenerator : IDatabaseGenerator
    {
        private readonly SqlTransactionBatches _sqlTransactionBatches;
        private readonly IConceptApplicationRepository _conceptApplicationRepository;
        private readonly ILogger _logger;
		/// <summary>Special logger for keeping track of inserted/updated/deleted concept applications in database.</summary>
        private readonly ILogger _changesLogger;
        private readonly ILogger _performanceLogger;
        private readonly DbUpdateOptions _dbUpdateOptions;
        private readonly DatabaseAnalysis _databaseAnalysis;

        public DatabaseGenerator(
            SqlTransactionBatches sqlTransactionBatches, 
            IConceptApplicationRepository conceptApplicationRepository,
            ILogProvider logProvider,
            DbUpdateOptions dbUpdateOptions,
            DatabaseAnalysis databaseAnalysis)
        {
            _sqlTransactionBatches = sqlTransactionBatches;
            _conceptApplicationRepository = conceptApplicationRepository;
            _logger = logProvider.GetLogger(GetType().Name);
            _changesLogger = logProvider.GetLogger("DatabaseGeneratorChanges");
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _dbUpdateOptions = dbUpdateOptions;
            _databaseAnalysis = databaseAnalysis;
        }

        public void UpdateDatabaseStructure()
        {
            _logger.Trace("Updating database structure.");
            var stopwatchTotal = Stopwatch.StartNew();
            var stopwatch = Stopwatch.StartNew();

            var diff = _databaseAnalysis.Diff();
            _performanceLogger.Write(stopwatch, "Database diff analysis.");

            LogAnalysisResults(diff);
            _performanceLogger.Write(stopwatch, "Log analysis results.");

            ApplyChangesToDatabase(diff.OldApplications, diff.NewApplications, diff.ToBeRemoved, diff.ToBeInserted);
            _performanceLogger.Write(stopwatch, "Applied changes to database.");

            VerifyIntegrity();
            _performanceLogger.Write(stopwatch, "Verified integrity of saved concept applications metadata.");

            _performanceLogger.Write(stopwatchTotal, "UpdateDatabaseStructure");
        }

        private void LogAnalysisResults(DatabaseDiff diff)
        {
            // Log modified database objects:

            foreach (var changed in diff.ChangedQueries)
                _changesLogger.Trace(() => $"Changed concept application: {changed.Old.GetConceptApplicationKey()}\r\n" +
                    $"{ReportDiff(changed.Old.CreateQuery, changed.New.CreateQuery)}");

            // Log dependencies for items that need to be refreshed:

            foreach (var refreshed in diff.Refreshes.GroupBy(r => r.RefreshedConcept, r => (r.Dependency, r.DependencyStatus)))
                LogDatabaseChanges(refreshed.Key, "Refresh", () =>
                {
                    var dependenciesByStatus = refreshed.GroupBy(r => r.DependencyStatus, r => r.Dependency).OrderBy(group => group.Key);
                    var report = dependenciesByStatus.Select(group =>
                    {
                        var dependencies = string.Join(", ", group.Select(dependency => dependency.GetConceptApplicationKey()));
                        return $"It depends on {group.Key.ToString().ToLower()} concepts: {dependencies}.";
                    });
                    return string.Join(" ", report);
                });
        }

        private string ReportDiff(string oldQuery, string newQuery)
        {
            int c = 0;
            for (; c < Math.Min(oldQuery.Length, newQuery.Length); c++)
                if (oldQuery[c] != newQuery[c])
                    break;
            return $"Old: {CsUtility.ReportSegment(oldQuery, c, 400)}\r\nNew: {CsUtility.ReportSegment(newQuery, c, 400)}";
        }

        private void ApplyChangesToDatabase(
            List<ConceptApplication> oldApplications, List<ConceptApplication> newApplications,
            List<ConceptApplication> toBeRemoved, List<ConceptApplication> toBeInserted)
        {
            var stopwatch = Stopwatch.StartNew();

            int estimatedNumberOfQueries = (toBeRemoved.Count + toBeInserted.Count) * 3;
            var sqlScripts = new List<string>(estimatedNumberOfQueries);

            sqlScripts.AddRange(ApplyChangesToDatabase_Remove(toBeRemoved, oldApplications));
            _performanceLogger.Write(stopwatch, "ApplyChangesToDatabase: Prepared SQL scripts for removing concept applications.");

            sqlScripts.AddRange(ApplyChangesToDatabase_Unchanged(toBeInserted, newApplications, oldApplications));
            _performanceLogger.Write(stopwatch, "ApplyChangesToDatabase: Prepared SQL scripts for updating unchanged concept applications' metadata.");

            sqlScripts.AddRange(ApplyChangesToDatabase_Insert(toBeInserted, newApplications));
            _performanceLogger.Write(stopwatch, "ApplyChangesToDatabase: Prepared SQL scripts for inserting concept applications.");

            _sqlTransactionBatches.Execute(sqlScripts.Select(sql => new SqlTransactionBatches.SqlScript { Sql = sql, IsBatch = false, Name = null }));
            _performanceLogger.Write(stopwatch, $"ApplyChangesToDatabase: Executed {sqlScripts.Count(sql => !string.IsNullOrEmpty(sql))} SQL scripts.");
        }

        private List<string> ApplyChangesToDatabase_Remove(List<ConceptApplication> toBeRemoved, List<ConceptApplication> oldApplications)
        {
            var newScripts = new List<string>();

            toBeRemoved = toBeRemoved.OrderBy(ca => ca.OldCreationOrder).ToList(); // TopologicalSort is stable sort, so it will keep this (original) order unless current dependencies direct otherwise.
            Graph.TopologicalSort(toBeRemoved, ConceptApplication.GetDependencyPairs(oldApplications)); // Concept's dependencies might have changed, without dropping and recreating the concept. It is important to compute up-to-date remove order, otherwise FK constraint FK_AppliedConceptDependsOn_DependsOn might fail.
            toBeRemoved.Reverse();

            int removedCACount = 0;
            foreach (var ca in toBeRemoved)
            {
                string[] removeSqlScripts = SplitSqlScript(ca.RemoveQuery);
                if (removeSqlScripts.Length > 0)
                {
                    LogDatabaseChanges(ca, "Removing");
                    removedCACount++;
                }

                newScripts.AddRange(removeSqlScripts);
                newScripts.AddRange(_sqlTransactionBatches.JoinScripts(_conceptApplicationRepository.DeleteMetadataSql(ca)));
                newScripts.AddRange(MaybeCommitMetadataAfterDdl(removeSqlScripts));
            }

            _logger.Info($"Removing {removedCACount} concept applications.");
            return newScripts;
        }

        private List<string> ApplyChangesToDatabase_Insert(List<ConceptApplication> toBeInserted, List<ConceptApplication> newApplications)
        {
            var newScripts = new List<string>();

            Graph.TopologicalSort(toBeInserted, ConceptApplication.GetDependencyPairs(newApplications));

            int insertedCACount = 0;
            foreach (var ca in toBeInserted)
            {
                string[] createSqlScripts = SplitSqlScript(ca.CreateQuery);
                if (createSqlScripts.Length > 0)
                {
                    LogDatabaseChanges(ca, "Creating");
                    insertedCACount++;
                }

                newScripts.AddRange(createSqlScripts);
                newScripts.AddRange(_sqlTransactionBatches.JoinScripts(_conceptApplicationRepository.InsertMetadataSql(ca)));
                newScripts.AddRange(MaybeCommitMetadataAfterDdl(createSqlScripts));
            }

            _logger.Info($"Creating {insertedCACount} concept applications.");
            return newScripts;
        }

        private IEnumerable<string> MaybeCommitMetadataAfterDdl(string[] databaseModificationScripts)
        {
            if (_dbUpdateOptions.ShortTransactions)
                yield return SqlUtility.NoTransactionTag; // The NoTransaction script will force commit of the previous (metadata) scripts.

            // If a DDL script is executed out of transaction, its metadata should also be committed immediately,
            // to avoid rolling back the concept application's metadata in case of any error that might occur later in the transaction.
            else if (databaseModificationScripts.Any(script => script.StartsWith(SqlUtility.NoTransactionTag)))
                yield return SqlUtility.NoTransactionTag;

            // Oracle must commit metadata changes before modifying next database object, to ensure metadata consistency if next DDL command fails
            // (Oracle db automatically commits changes on DDL commands, so the previous DDL command has already been committed).
            yield return Sql.Get("DatabaseGenerator_CommitAfterDDL");
        }

        private List<string> ApplyChangesToDatabase_Unchanged(List<ConceptApplication> toBeInserted, List<ConceptApplication> newApplications, List<ConceptApplication> oldApplications)
        {
            var newScripts = new List<string>();

            var indexInsertedConcepts = new HashSet<string>(toBeInserted.Select(ca => ca.GetConceptApplicationKey()));
            var unchangedApplications = newApplications
                .Where(ca => !indexInsertedConcepts.Contains(ca.GetConceptApplicationKey()));

            var oldApplicationsByKey = oldApplications.ToDictionary(oa => oa.GetConceptApplicationKey());

            foreach (var ca in unchangedApplications)
            {
                var updateMetadataSql = _sqlTransactionBatches.JoinScripts(_conceptApplicationRepository.UpdateMetadataSql(ca, oldApplicationsByKey[ca.GetConceptApplicationKey()]));
                if (updateMetadataSql.Any())
                {
                    LogDatabaseChanges(ca, "Updating metadata");
                    newScripts.AddRange(updateMetadataSql);
                }
            }

            return newScripts;
        }

        private static string[] SplitSqlScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return Array.Empty<string>();
            return script.Split(new[] { SqlUtility.ScriptSplitterTag }, StringSplitOptions.RemoveEmptyEntries)
                .Where(query => !string.IsNullOrWhiteSpace(query))
                .Select(query => query.Trim()).ToArray();
        }

        private void LogDatabaseChanges(ConceptApplication conceptApplication, string action, Func<string> additionalInfo = null)
        {
            _changesLogger.Trace("{0} {1}, ID={2}.{3}{4}",
                action,
                conceptApplication.GetConceptApplicationKey(),
                SqlUtility.GuidToString(conceptApplication.Id),
                additionalInfo != null ? " " : null,
                additionalInfo != null ? additionalInfo() : null);
        }

        private void VerifyIntegrity()
        {
            try
            {
                _conceptApplicationRepository.Load();
            }
            catch (Exception ex)
            {
                throw new FrameworkException("Metadata integrity error after applying changes to database. " + ex.Message, ex);
            }
        }
    }
}