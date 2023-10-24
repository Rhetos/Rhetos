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
    /// Analyzes the difference between the current generated database model (from DSL scripts)
    /// and the previously created objects in database (from metadata in table Rhetos.AppliedConcept).
    /// Returns a list of database objects that need to be dropped and created.
    /// </summary>
    public class DatabaseAnalysis
    {
        private readonly IConceptApplicationRepository _conceptApplicationRepository;
        private readonly ILogger _logger;
		/// <summary>Special logger for keeping track of inserted/updated/deleted concept applications in database.</summary>
        private readonly ILogger _performanceLogger;
        private readonly DatabaseModel _databaseModel;

        public DatabaseAnalysis(
            IConceptApplicationRepository conceptApplicationRepository,
            ILogProvider logProvider,
            DatabaseModel databaseModel)
        {
            _conceptApplicationRepository = conceptApplicationRepository;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _databaseModel = databaseModel;
        }

        /// <summary>
        /// Compares current generated database model (from DSL scripts)
        /// and the previously created objects in database (from metadata in table Rhetos.AppliedConcept).
        /// </summary>
        public DatabaseDiff Diff()
        {
            var stopwatch = Stopwatch.StartNew();

            var oldApplications = _conceptApplicationRepository.Load();
            _performanceLogger.Write(stopwatch, "Loaded old concept applications.");

            var newApplications = ConceptApplication.FromDatabaseObjects(_databaseModel.DatabaseObjects);
            _performanceLogger.Write(stopwatch, "Got new concept applications.");

            MatchAndComputeNewApplicationIds(oldApplications, newApplications);
            _performanceLogger.Write(stopwatch, "Match new and old concept applications.");

            ConceptApplication.CheckKeyUniqueness(newApplications, "generated, after matching");
            _performanceLogger.Write(stopwatch, "Verify new concept applications' integrity.");

            newApplications = TrimEmptyApplications(newApplications);
            _performanceLogger.Write(stopwatch, "Removed unused concept applications.");

            var diff = CalculateApplicationsToBeRemovedAndInserted(oldApplications, newApplications);
            _performanceLogger.Write(stopwatch, "Analyzed differences in database structure.");
            return diff;
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

            _logger.Trace(() => $"Removing {removeLeaves.Count} empty leaf concept applications:{string.Concat(removeLeaves.Select(l => "\r\n-" + l))}");

            return newApplications.Except(removeLeaves).ToList();
        }

        private DatabaseDiff CalculateApplicationsToBeRemovedAndInserted(List<ConceptApplication> oldApplications, List<ConceptApplication> newApplications)
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
            
            // Find changed concept applications (different create SQL query):

            var existingApplications = oldApplicationsByKey.Keys.Intersect(newApplicationsByKey.Keys).ToList();
            var changedApplications = existingApplications
                .Where(appKey => !string.Equals(
                    oldApplicationsByKey[appKey].CreateQuery,
                    newApplicationsByKey[appKey].CreateQuery,
                    StringComparison.Ordinal))
                .ToList();

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

            // Report dependencies for items that need to be refreshed, for logging and debugging only:

            var newDependenciesByDependent = newDependencies.GroupBy(dep => dep.Item2, dep => dep.Item1).ToDictionary(group => group.Key, group => group.ToList());
            var oldDependenciesByDependent = oldDependencies.GroupBy(dep => dep.Item2, dep => dep.Item1).ToDictionary(group => group.Key, group => group.ToList());
            var toBeInsertedIndex = new HashSet<string>(toBeInsertedKeys);
            var toBeRemovedIndex = new HashSet<string>(toBeRemovedKeys);
            var changedApplicationsIndex = new HashSet<string>(changedApplications);
            var refreshes = new List<(ConceptApplication RefreshedConcept, ConceptApplication Dependency, RefreshDependencyStatus DependencyStatus)>();

            foreach (string ca in refreshDependents.Intersect(newApplicationsByKey.Keys))
            {
                var refreshedConcept = newApplicationsByKey[ca];
                var refreshBecauseNew = new HashSet<string>(newDependenciesByDependent.GetValueOrEmpty(ca).Intersect(toBeInsertedIndex));
                var refreshBecauseOld = new HashSet<string>(oldDependenciesByDependent.GetValueOrEmpty(ca).Intersect(toBeRemovedIndex));
                var dependsOnExisting = refreshBecauseNew.Intersect(refreshBecauseOld);

                refreshes.AddRange(refreshBecauseNew.Except(refreshBecauseOld)
                    .Select(dependency => (refreshedConcept, newApplicationsByKey[dependency], RefreshDependencyStatus.New)));
                refreshes.AddRange(refreshBecauseOld.Except(refreshBecauseNew)
                    .Select(dependency => (refreshedConcept, oldApplicationsByKey[dependency], RefreshDependencyStatus.Removed)));
                refreshes.AddRange(dependsOnExisting.Intersect(changedApplicationsIndex)
                    .Select(dependency => (refreshedConcept, newApplicationsByKey[dependency], RefreshDependencyStatus.Changed)));
                refreshes.AddRange(dependsOnExisting.Except(changedApplicationsIndex)
                    .Select(dependency => (refreshedConcept, newApplicationsByKey[dependency], RefreshDependencyStatus.Refreshed)));
            }

            // Result:

            return new DatabaseDiff
            {
                OldApplications = oldApplications,
                NewApplications = newApplications,
                ToBeRemoved = toBeRemovedKeys.Select(key => oldApplicationsByKey[key]).ToList(),
                ToBeInserted = toBeInsertedKeys.Select(key => newApplicationsByKey[key]).ToList(),
                ChangedQueries = changedApplications.Select(ca => (Old: oldApplicationsByKey[ca], New: newApplicationsByKey[ca])).ToList(),
                Refreshes = refreshes,
            };
        }
    }
}