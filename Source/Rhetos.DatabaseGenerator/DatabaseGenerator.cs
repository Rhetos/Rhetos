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
        private readonly ILogger _conceptsLogger;
        private readonly ILogger _deployPackagesLogger;
        private readonly ILogger _performanceLogger;
        private readonly DatabaseGeneratorOptions _options;
        private readonly DatabaseModel _databaseModel;

        private bool DatabaseUpdated = false;
        private readonly object _databaseUpdateLock = new object();

        public DatabaseGenerator(
            SqlTransactionBatches sqlTransactionBatches, 
            IConceptApplicationRepository conceptApplicationRepository,
            ILogProvider logProvider,
            DatabaseGeneratorOptions options,
            DatabaseModel databaseModel)
        {
            _sqlTransactionBatches = sqlTransactionBatches;
            _conceptApplicationRepository = conceptApplicationRepository;
            _logger = logProvider.GetLogger("DatabaseGenerator");
            _conceptsLogger = logProvider.GetLogger("DatabaseGenerator Concepts");
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _performanceLogger = logProvider.GetLogger("Performance");
            _options = options;
            _databaseModel = databaseModel;
        }

        public void UpdateDatabaseStructure()
        {
            if (DatabaseUpdated) // performance optimization
                _deployPackagesLogger.Trace("Database already updated.");

            lock (_databaseUpdateLock)
            {
                if (DatabaseUpdated)
                    _deployPackagesLogger.Trace("Database already updated.");

                _logger.Trace("Updating database structure.");
                var stopwatchTotal = Stopwatch.StartNew();
                var stopwatch = Stopwatch.StartNew();

                var oldApplications = _conceptApplicationRepository.Load();
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Loaded old concept applications.");

                var newApplications = ConceptApplication.FromDatabaseObjects(_databaseModel.DatabaseObjects);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Got new concept applications.");

                MatchAndComputeNewApplicationIds(oldApplications, newApplications);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Match new and old concept applications.");

                ConceptApplication.CheckKeyUniqueness(newApplications, "generated, after matching");
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Verify new concept applications' integrity.");
                newApplications = TrimEmptyApplications(newApplications);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Removed unused concept applications.");

                List<ConceptApplication> toBeRemoved;
                List<ConceptApplication> toBeInserted;
                CalculateApplicationsToBeRemovedAndInserted(oldApplications, newApplications, out toBeRemoved, out toBeInserted);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Analyzed differences in database structure.");

                ApplyChangesToDatabase(oldApplications, newApplications, toBeRemoved, toBeInserted);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Applied changes to database.");

                VerifyIntegrity();
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Verified integrity of saved concept applications metadata.");

                _performanceLogger.Write(stopwatchTotal, "DatabaseGenerator.UpdateDatabaseStructure");
                DatabaseUpdated = true;
            }
        }

        private static void MatchAndComputeNewApplicationIds(List<ConceptApplication> oldApplications, List<ConceptApplication> newApplications)
        {
            var oldApplicationIds = oldApplications.ToDictionary(oa => oa.GetConceptApplicationKey(), oa => oa.Id);
            foreach (var newApp in newApplications) 
                if (!oldApplicationIds.TryGetValue(newApp.GetConceptApplicationKey(), out newApp.Id))
                    newApp.Id = Guid.NewGuid();
        }

        private List<ConceptApplication> TrimEmptyApplications(List<ConceptApplication> newApplications)
        {
            var emptyCreateQuery = newApplications.Where(ca => string.IsNullOrWhiteSpace(ca.CreateQuery)).ToList();
            var emptyCreateHasRemove = emptyCreateQuery.FirstOrDefault(ca => !string.IsNullOrWhiteSpace(ca.RemoveQuery));
            if (emptyCreateHasRemove != null)
                throw new FrameworkException("A concept that does not create database objects (CreateDatabaseStructure) cannot remove them (RemoveDatabaseStructure): "
                    + emptyCreateHasRemove.GetConceptApplicationKey() + ".");

            var removeLeaves = Graph.RemovableLeaves(emptyCreateQuery, ConceptApplication.GetDependencyPairs(newApplications));

            foreach (var remove in removeLeaves)
            {
                var r = remove;
                _logger.Trace(() => "Removing empty leaf concept application " + r + ".");
            }
            return newApplications.Except(removeLeaves).ToList();
        }

        private void CalculateApplicationsToBeRemovedAndInserted(
            IEnumerable<ConceptApplication> oldApplications, IEnumerable<ConceptApplication> newApplications,
            out List<ConceptApplication> toBeRemoved, out List<ConceptApplication> toBeInserted)
        {
            var oldApplicationsByKey = oldApplications.ToDictionary(a => a.GetConceptApplicationKey());
            var newApplicationsByKey = newApplications.ToDictionary(a => a.GetConceptApplicationKey());

            // Find directly inserted and removed concept applications:

            var directlyRemoved = oldApplicationsByKey.Keys.Except(newApplicationsByKey.Keys).ToList();
            var directlyInserted = newApplicationsByKey.Keys.Except(oldApplicationsByKey.Keys).ToList();

            foreach (string ca in directlyRemoved)
                _logger.Trace("Directly removed concept application: " + ca);
            foreach (string ca in directlyInserted)
                _logger.Trace("Directly inserted concept application: " + ca);
            
            // Find changed concept applications (different create sql query):

            var existingApplications = oldApplicationsByKey.Keys.Intersect(newApplicationsByKey.Keys).ToList();
            var changedApplications = existingApplications
                .Where(appKey => !string.Equals(
                    oldApplicationsByKey[appKey].CreateQuery,
                    newApplicationsByKey[appKey].CreateQuery,
                    StringComparison.Ordinal))
                .ToList();

            foreach (string ca in changedApplications)
                _logger.Trace(() => $"Changed concept application: {ca}\r\n{ReportDiff(oldApplicationsByKey[ca].CreateQuery, newApplicationsByKey[ca].CreateQuery)}");

            // Find dependent concepts applications to be regenerated:

            var toBeRemovedKeys = directlyRemoved.Union(changedApplications).ToList();
            var oldDependencies = ConceptApplication.GetDependencyPairs(oldApplications).Select(dep => Tuple.Create(dep.Item1.GetConceptApplicationKey(), dep.Item2.GetConceptApplicationKey()));
            var dependentRemovedApplications = Graph.IncludeDependents(toBeRemovedKeys, oldDependencies).Except(toBeRemovedKeys);

            var toBeInsertedKeys = directlyInserted.Union(changedApplications).ToList();
            var newDependencies = ConceptApplication.GetDependencyPairs(newApplications).Select(dep => Tuple.Create(dep.Item1.GetConceptApplicationKey(), dep.Item2.GetConceptApplicationKey()));
            var dependentInsertedApplications = Graph.IncludeDependents(toBeInsertedKeys, newDependencies).Except(toBeInsertedKeys);

            var refreshDependents = dependentRemovedApplications.Union(dependentInsertedApplications).ToList();
            toBeRemovedKeys.AddRange(refreshDependents.Intersect(oldApplicationsByKey.Keys));
            toBeInsertedKeys.AddRange(refreshDependents.Intersect(newApplicationsByKey.Keys));

            // Log dependencies for items that need to be refreshed:

            var newDependenciesByDependent = newDependencies.GroupBy(dep => dep.Item2, dep => dep.Item1).ToDictionary(group => group.Key, group => group.ToList());
            var oldDependenciesByDependent = oldDependencies.GroupBy(dep => dep.Item2, dep => dep.Item1).ToDictionary(group => group.Key, group => group.ToList());
            var toBeInsertedIndex = new HashSet<string>(toBeInsertedKeys);
            var toBeRemovedIndex = new HashSet<string>(toBeRemovedKeys);
            var changedApplicationsIndex = new HashSet<string>(changedApplications);
            foreach (string ca in refreshDependents.Intersect(newApplicationsByKey.Keys))
                LogDatabaseChanges(newApplicationsByKey[ca], "Refresh", () =>
                    {
                        var report = new List<string>();
                        var refreshBecauseNew = new HashSet<string>(newDependenciesByDependent.GetValueOrEmpty(ca).Intersect(toBeInsertedIndex));
                        var refreshBecauseOld = new HashSet<string>(oldDependenciesByDependent.GetValueOrEmpty(ca).Intersect(toBeRemovedIndex));
                        var dependsOnNew = string.Join(", ", refreshBecauseNew.Except(refreshBecauseOld));
                        var dependsOnOld = string.Join(", ", refreshBecauseOld.Except(refreshBecauseNew));
                        var dependsOnExisting = refreshBecauseNew.Intersect(refreshBecauseOld);
                        var dependsOnChanged = string.Join(", ", dependsOnExisting.Intersect(changedApplicationsIndex));
                        var dependsOnRefreshed = string.Join(", ", dependsOnExisting.Except(changedApplicationsIndex));
                        if (!string.IsNullOrEmpty(dependsOnNew))
                            report.Add("It depends on new concepts: " + dependsOnNew + ".");
                        if (!string.IsNullOrEmpty(dependsOnChanged))
                            report.Add("It depends on changed concepts: " + dependsOnChanged + ".");
                        if (!string.IsNullOrEmpty(dependsOnRefreshed))
                            report.Add("It depends on refreshed concepts: " + dependsOnRefreshed + ".");
                        if (!string.IsNullOrEmpty(dependsOnOld))
                            report.Add("It depended on removed concepts: " + dependsOnOld + ".");
                        return string.Join(" ", report);
                    });

            // Result:

            toBeRemoved = toBeRemovedKeys.Select(key => oldApplicationsByKey[key]).ToList();
            toBeInserted = toBeInsertedKeys.Select(key => newApplicationsByKey[key]).ToList();
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

            int estimatedNumberOfQueries = (toBeRemoved.Count() + toBeInserted.Count()) * 3;
            var sqlScripts = new List<string>(estimatedNumberOfQueries);

            sqlScripts.AddRange(ApplyChangesToDatabase_Remove(toBeRemoved, oldApplications));
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.ApplyChangesToDatabase: Prepared SQL scripts for removing concept applications.");

            sqlScripts.AddRange(ApplyChangesToDatabase_Unchanged(toBeInserted, newApplications, oldApplications));
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.ApplyChangesToDatabase: Prepared SQL scripts for updating unchanged concept applications' metadata.");

            sqlScripts.AddRange(ApplyChangesToDatabase_Insert(toBeInserted, newApplications));
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.ApplyChangesToDatabase: Prepared SQL scripts for inserting concept applications.");

            _sqlTransactionBatches.Execute(sqlScripts.Select(sql => new SqlTransactionBatches.SqlScript { Sql = sql, IsBatch = false, Name = null }));
            _performanceLogger.Write(stopwatch, $"DatabaseGenerator.ApplyChangesToDatabase: Executed {sqlScripts.Where(sql => !string.IsNullOrEmpty(sql)).Count()} SQL scripts.");
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

            var logLevel = removedCACount > 0 ? EventType.Info : EventType.Trace;
            _deployPackagesLogger.Write(logLevel, "DatabaseGenerator removing " + removedCACount + " concept applications.");
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

            var logLevel = insertedCACount > 0 ? EventType.Info : EventType.Trace;
            _deployPackagesLogger.Write(logLevel, "DatabaseGenerator creating " + insertedCACount + " concept applications.");
            return newScripts;
        }

        private IEnumerable<string> MaybeCommitMetadataAfterDdl(string[] databaseModificationScripts)
        {
            if (_options.ShortTransactions)
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
                return new string[] { };
            return script.Split(new[] { SqlUtility.ScriptSplitterTag }, StringSplitOptions.RemoveEmptyEntries)
                .Where(query => !string.IsNullOrWhiteSpace(query))
                .Select(query => query.Trim()).ToArray();
        }

        private void LogDatabaseChanges(ConceptApplication conceptApplication, string action, Func<string> additionalInfo = null)
        {
            _conceptsLogger.Trace("{0} {1}, ID={2}.{3}{4}",
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