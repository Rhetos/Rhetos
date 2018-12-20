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
using System.Diagnostics;
using System.Linq;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.Globalization;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Logging;
using System.Text;

namespace Rhetos.DatabaseGenerator
{
    public class DatabaseGenerator : IDatabaseGenerator
    {
        protected readonly SqlTransactionBatches _sqlTransactionBatches;
        protected readonly IDslModel _dslModel;
        protected readonly IPluginsContainer<IConceptDatabaseDefinition> _plugins;
        protected readonly IConceptApplicationRepository _conceptApplicationRepository;
        protected readonly ILogger _logger;
		/// <summary>Special logger for keeping track of inserted/updated/deleted concept applications in database.</summary>
        protected readonly ILogger _conceptsLogger;
        protected readonly ILogger _deployPackagesLogger;
        protected readonly ILogger _performanceLogger;
        protected readonly DatabaseGeneratorOptions _options;

        protected bool DatabaseUpdated = false;

        protected readonly object _databaseUpdateLock = new object();

        public DatabaseGenerator(
            SqlTransactionBatches sqlTransactionBatches, 
            IDslModel dslModel,
            IPluginsContainer<IConceptDatabaseDefinition> plugins,
            IConceptApplicationRepository conceptApplicationRepository,
            ILogProvider logProvider,
            DatabaseGeneratorOptions options)
        {
            _sqlTransactionBatches = sqlTransactionBatches;
            _dslModel = dslModel;
            _plugins = plugins;
            _conceptApplicationRepository = conceptApplicationRepository;
            _logger = logProvider.GetLogger("DatabaseGenerator");
            _conceptsLogger = logProvider.GetLogger("DatabaseGenerator Concepts");
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _performanceLogger = logProvider.GetLogger("Performance");
            _options = options;
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

                var newApplications = CreateNewApplications(oldApplications);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Created new concept applications.");
                ConceptApplicationRepository.CheckKeyUniqueness(newApplications, "created");
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Verify new concept applications' integrity.");
                newApplications = TrimEmptyApplications(newApplications);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Removed unused concept applications.");

                List<ConceptApplication> toBeRemoved;
                List<NewConceptApplication> toBeInserted;
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

        protected static void MatchAndComputeNewApplicationIds(List<ConceptApplication> oldApplications, List<NewConceptApplication> newApplications)
        {
            var oldApplicationIds = oldApplications.ToDictionary(oa => oa.GetConceptApplicationKey(), oa => oa.Id);
            foreach (var newApp in newApplications) 
                if (!oldApplicationIds.TryGetValue(newApp.GetConceptApplicationKey(), out newApp.Id))
                    newApp.Id = Guid.NewGuid();
        }

        protected List<NewConceptApplication> TrimEmptyApplications(List<NewConceptApplication> newApplications)
        {
            var emptyCreateQuery = newApplications.Where(ca => string.IsNullOrWhiteSpace(ca.CreateQuery)).ToList();
            var emptyCreateHasRemove = emptyCreateQuery.FirstOrDefault(ca => !string.IsNullOrWhiteSpace(ca.RemoveQuery));
            if (emptyCreateHasRemove != null)
                throw new FrameworkException("A concept that does not create database objects (CreateDatabaseStructure) cannot remove them (RemoveDatabaseStructure): "
                    + emptyCreateHasRemove.GetConceptApplicationKey() + ".");

            var removeLeaves = Graph.RemovableLeaves(emptyCreateQuery, GetDependencyPairs(newApplications));

            foreach (var remove in removeLeaves)
            {
                var r = remove;
                _logger.Trace(() => "Removing empty leaf concept application " + r + ".");
            }
            return newApplications.Except(removeLeaves).ToList();
        }

        protected List<NewConceptApplication> CreateNewApplications(List<ConceptApplication> oldApplications)
        {
            var stopwatch = Stopwatch.StartNew();

            var conceptApplications = new List<NewConceptApplication>();
            foreach (var conceptInfo in _dslModel.Concepts)
            {
                IConceptDatabaseDefinition[] implementations = _plugins.GetImplementations(conceptInfo.GetType()).ToArray();

                if (implementations.Count() == 0)
                    implementations = new[] { new NullImplementation() };

                conceptApplications.AddRange(implementations.Select(impl => new NewConceptApplication(conceptInfo, impl))); // DependsOn, CreateQuery and RemoveQuery will be set later.
            }
            MatchAndComputeNewApplicationIds(oldApplications, conceptApplications);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: Created concept applications from plugins.");

            ComputeDependsOn(conceptApplications);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: Computed dependencies.");

            ComputeCreateAndRemoveQuery(conceptApplications, _dslModel.Concepts);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: Generated SQL queries for new concept applications.");

            _logger.Trace(() => ReportDependencies(conceptApplications));

            return conceptApplications;
        }

        protected void ComputeDependsOn(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (var conceptApplication in newConceptApplications)
                conceptApplication.DependsOn = new ConceptApplicationDependency[] {};

            var dependencies = ExtractDependencies(newConceptApplications);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: ExtractDependencies executed.");

            UpdateConceptApplicationsFromDependencyList(dependencies);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: UpdateConceptApplicationsFromDependencyList executed.");
        }

        /// <summary>
        /// Updates ConceptApplication.DependsOn property from "flat" list of dependencies.
        /// </summary>
        protected static void UpdateConceptApplicationsFromDependencyList(IEnumerable<Dependency> dependencies)
        {
            var dependenciesByConceptApplication = dependencies
                .GroupBy(d => d.Dependent, d => new ConceptApplicationDependency { ConceptApplication = d.DependsOn, DebugInfo = d.DebugInfo });

            foreach (var dependencyGroup in dependenciesByConceptApplication)
            {
                var dependent = dependencyGroup.Key;
                var newDependsOn = dependencyGroup.Distinct().Union(dependent.DependsOn);

                dependent.DependsOn = newDependsOn.ToArray();
            }
        }

        protected IEnumerable<Dependency> ExtractDependencies(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var exFromConceptInfo = ExtractDependenciesFromConceptInfos(newConceptApplications).ToList();
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: ExtractDependenciesFromConceptInfos executed.");
            
            var exFromMefPluginMetadata = ExtractDependenciesFromMefPluginMetadata(_plugins, newConceptApplications).ToList();
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: ExtractDependenciesFromMefPluginMetadata executed.");
            
            var combined = exFromConceptInfo.Union(exFromMefPluginMetadata).ToList();
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: Dependencies union executed.");
            
            return combined;
        }

        protected IEnumerable<Dependency> ExtractDependenciesFromConceptInfos(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            var conceptInfos = newConceptApplications.Select(conceptApplication => conceptApplication.ConceptInfo).Distinct();

            var conceptInfoDependencies = conceptInfos.SelectMany(conceptInfo => conceptInfo.GetAllDependencies()
                .Select(dependency => Tuple.Create(dependency, conceptInfo, "Direct or indirect IConceptInfo reference")));

            return GetConceptApplicationDependencies(conceptInfoDependencies, newConceptApplications);
        }

        protected static IEnumerable<Dependency> GetConceptApplicationDependencies(IEnumerable<Tuple<IConceptInfo, IConceptInfo, string>> conceptInfoDependencies, IEnumerable<ConceptApplication> conceptApplications)
        {
            var conceptApplicationsByConceptInfoKey = conceptApplications
                .GroupBy(ca => ca.ConceptInfoKey)
                .ToDictionary(g => g.Key, g => g.ToList());

            var conceptInfoKeyDependencies = conceptInfoDependencies.Select(dep => Tuple.Create(dep.Item1.GetKey(), dep.Item2.GetKey(), dep.Item3));

            var conceptApplicationDependencies =
                from conceptInfoKeyDependency in conceptInfoKeyDependencies
                where conceptApplicationsByConceptInfoKey.ContainsKey(conceptInfoKeyDependency.Item1)
                      && conceptApplicationsByConceptInfoKey.ContainsKey(conceptInfoKeyDependency.Item2)
                from dependsOnConceptApplication in conceptApplicationsByConceptInfoKey[conceptInfoKeyDependency.Item1]
                from dependentConceptApplication in conceptApplicationsByConceptInfoKey[conceptInfoKeyDependency.Item2]
                select new Dependency
                    {
                        DependsOn = dependsOnConceptApplication,
                        Dependent = dependentConceptApplication,
                        DebugInfo = conceptInfoKeyDependency.Item3
                    };

            return conceptApplicationDependencies.ToList();
        }

        protected static IEnumerable<Dependency> ExtractDependenciesFromMefPluginMetadata(IPluginsContainer<IConceptDatabaseDefinition> plugins, IEnumerable<NewConceptApplication> newConceptApplications)
        {
            var dependencies = new List<Dependency>();

            var conceptApplicationsByImplementation = newConceptApplications
                .GroupBy(ca => ca.ConceptImplementationType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var distinctConceptImplementations = newConceptApplications.Select(ca => ca.ConceptImplementationType).Distinct().ToList();

            var implementationDependencies = GetImplementationDependencies(plugins, distinctConceptImplementations);

            foreach (var implementationDependency in implementationDependencies)
                if (conceptApplicationsByImplementation.ContainsKey(implementationDependency.Item1)
                    && conceptApplicationsByImplementation.ContainsKey(implementationDependency.Item2))
                    AddDependenciesOnSameConceptInfo(
                        conceptApplicationsByImplementation[implementationDependency.Item1],
                        conceptApplicationsByImplementation[implementationDependency.Item2],
                        implementationDependency.Item3,
                        dependencies);

            return dependencies.Distinct().ToList();
        }

        protected static IEnumerable<Tuple<Type, Type, string>> GetImplementationDependencies(IPluginsContainer<IConceptDatabaseDefinition> plugins, IEnumerable<Type> conceptImplementations)
        {
            var dependencies = new List<Tuple<Type, Type, string>>();

            foreach (Type conceptImplementation in conceptImplementations)
            {
                Type dependency = plugins.GetMetadata(conceptImplementation, "DependsOn");

                if (dependency == null)
                    continue;
                Type implements = plugins.GetMetadata(conceptImplementation, "Implements");
                Type dependencyImplements = plugins.GetMetadata(dependency, "Implements");

                if (!implements.Equals(dependencyImplements)
                    && !implements.IsAssignableFrom(dependencyImplements)
                    && !dependencyImplements.IsAssignableFrom(implements))
                    throw new FrameworkException(string.Format(
                        "DatabaseGenerator plugin {0} cannot depend on {1}."
                        + "\"DependsOn\" value in ExportMetadata attribute must reference implementation of same concept."
                        + " This additional dependencies should be used only to disambiguate between plugins that implement same IConceptInfo."
                        + " {2} implements {3}, while {4} implements {5}.",
                        conceptImplementation.FullName,
                        dependency.FullName,
                        conceptImplementation.Name,
                        implements.FullName,
                        dependency.Name,
                        dependencyImplements.FullName));

                dependencies.Add(Tuple.Create(dependency, conceptImplementation, "DependsOn metadata"));
            }

            return dependencies;
        }

        protected static void AddDependenciesOnSameConceptInfo(
            IEnumerable<ConceptApplication> applications1,
            IEnumerable<ConceptApplication> applications2,
            string debugInfo,
            List<Dependency> dependencies)
        {
            var applications2ByConceptInfoKey = applications2.ToDictionary(a => a.ConceptInfoKey);
            dependencies.AddRange(from application1 in applications1
                where applications2ByConceptInfoKey.ContainsKey(application1.ConceptInfoKey)
                select new Dependency
                    {
                        DependsOn = application1,
                        Dependent = applications2ByConceptInfoKey[application1.ConceptInfoKey],
                        DebugInfo = debugInfo
                    });
        }

        protected void ComputeCreateAndRemoveQuery(List<NewConceptApplication> newConceptApplications, IEnumerable<IConceptInfo> allConceptInfos)
        {
            Graph.TopologicalSort(newConceptApplications, GetDependencyPairs(newConceptApplications));

            var conceptInfosByKey = allConceptInfos.ToDictionary(ci => ci.GetKey());

            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            var createdDependencies = new List<Tuple<IConceptInfo, IConceptInfo, string>>();
            foreach (var ca in newConceptApplications)
            {
                AddConceptApplicationSeparator(ca, sqlCodeBuilder);

                // Generate RemoveQuery:

                GenerateRemoveQuery(ca);

                // Generate CreateQuery:

                sqlCodeBuilder.InsertCode(ca.ConceptImplementation.CreateDatabaseStructure(ca.ConceptInfo));

                if (ca.ConceptImplementation is IConceptDatabaseDefinitionExtension)
                {
                    IEnumerable<Tuple<IConceptInfo, IConceptInfo>> pluginCreatedDependencies;
                    ((IConceptDatabaseDefinitionExtension)ca.ConceptImplementation).ExtendDatabaseStructure(ca.ConceptInfo, sqlCodeBuilder, out pluginCreatedDependencies);

                    if (pluginCreatedDependencies != null)
                    {
                        var resolvedDependencies = pluginCreatedDependencies.Select(dep => Tuple.Create(
                            GetValidConceptInfo(dep.Item1.GetKey(), conceptInfosByKey, ca),
                            GetValidConceptInfo(dep.Item2.GetKey(), conceptInfosByKey, ca),
                            "ExtendDatabaseStructure " + ca.ToString())).ToList();
                        
                        createdDependencies.AddRange(resolvedDependencies);
                    }
                }
            }

            ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications);

            var createdConceptApplicationDependencies = GetConceptApplicationDependencies(createdDependencies, newConceptApplications);
            UpdateConceptApplicationsFromDependencyList(createdConceptApplicationDependencies);
        }

        public static void GenerateRemoveQuery(NewConceptApplication ca)
        {
            ca.RemoveQuery = ca.ConceptImplementation.RemoveDatabaseStructure(ca.ConceptInfo);
            if (ca.RemoveQuery != null)
                ca.RemoveQuery = ca.RemoveQuery.Trim();
            else
                ca.RemoveQuery = "";
        }

        protected static IConceptInfo GetValidConceptInfo(string conceptInfoKey, Dictionary<string, IConceptInfo> conceptInfosByKey, NewConceptApplication debugContextNewConceptApplication)
        {
            if (!conceptInfosByKey.ContainsKey(conceptInfoKey))
                throw new FrameworkException(string.Format(
                    "DatabaseGenerator error while generating code with plugin {0}: Extension created a dependency to the nonexistent concept info {1}.",
                    debugContextNewConceptApplication.ConceptImplementationType.Name,
                    conceptInfoKey));
            return conceptInfosByKey[conceptInfoKey];
        }

        /// <returns>Item2 depends on item1.</returns>
        protected static List<Tuple<NewConceptApplication, NewConceptApplication>> GetDependencyPairs(IEnumerable<NewConceptApplication> conceptApplications)
        {
            return conceptApplications
                .SelectMany(dependent => dependent.DependsOn.Select(dependency => Tuple.Create((NewConceptApplication)dependency.ConceptApplication, dependent)))
                .Where(dependency => dependency.Item1 != dependency.Item2)
                .ToList();
        }

        /// <returns>Item2 depends on item1.</returns>
        protected static List<Tuple<ConceptApplication, ConceptApplication>> GetDependencyPairs(IEnumerable<ConceptApplication> conceptApplications)
        {
            return conceptApplications.SelectMany(
                dependent => dependent.DependsOn.Select(dependsOn => Tuple.Create(dependsOn.ConceptApplication, dependent))
                ).ToList();
        }

        protected const string NextConceptApplicationSeparator = "/*NextConceptApplication*/";
        protected const string NextConceptApplicationIdPrefix = "/*ConceptApplicationId:";
        protected const string NextConceptApplicationIdSuffix = "*/";

        protected static void AddConceptApplicationSeparator(ConceptApplication ca, CodeBuilder sqlCodeBuilder)
        {
            sqlCodeBuilder.InsertCode(string.Format("{0}{1}{2}{3}",
                NextConceptApplicationSeparator, NextConceptApplicationIdPrefix, ca.Id, NextConceptApplicationIdSuffix));
        }

        protected static void ExtractCreateQueries(string generatedSqlCode, IEnumerable<ConceptApplication> toBeInserted)
        {
            var sqls = generatedSqlCode.Split(new[] { NextConceptApplicationSeparator }, StringSplitOptions.None).ToList();
            if (sqls.Count > 0) sqls.RemoveAt(0);

            var toBeInsertedById = toBeInserted.ToDictionary(ca => ca.Id);

            int guidLength = Guid.Empty.ToString().Length;
            foreach (var sql in sqls)
            {
                var id = Guid.Parse(sql.Substring(NextConceptApplicationIdPrefix.Length, guidLength));
                toBeInsertedById[id].CreateQuery = sql
                    .Substring(NextConceptApplicationIdPrefix.Length + guidLength +NextConceptApplicationIdSuffix.Length)
                    .Trim();
            }
        }

        private string ReportDependencies(List<NewConceptApplication> conceptApplications)
        {
            var report = new StringBuilder();
            report.Append("Dependencies:");
            foreach (var ca in conceptApplications.Where(x => x.DependsOn.Count() > 0))
            {
                report.AppendLine().Append(ca.ToString()).Append(" depends on:");
                foreach (var dep in ca.DependsOn)
                    report.Append("\r\n  ").Append(dep.ConceptApplication.ToString()).Append(" (").Append(dep.DebugInfo).Append(")");
            };
            return report.ToString();
        }

        protected void CalculateApplicationsToBeRemovedAndInserted(
            IEnumerable<ConceptApplication> oldApplications, IEnumerable<NewConceptApplication> newApplications,
            out List<ConceptApplication> toBeRemoved, out List<NewConceptApplication> toBeInserted)
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
            var changedApplications = existingApplications.Where(appKey => !string.Equals(
                oldApplicationsByKey[appKey].CreateQuery,
                newApplicationsByKey[appKey].CreateQuery)).ToList();

            foreach (string ca in changedApplications)
                _logger.Trace("Changed concept application: " + ca);

            // Find dependent concepts applications to be regenerated:

            var toBeRemovedKeys = directlyRemoved.Union(changedApplications).ToList();
            var oldDependencies = GetDependencyPairs(oldApplications).Select(dep => Tuple.Create(dep.Item1.GetConceptApplicationKey(), dep.Item2.GetConceptApplicationKey()));
            var dependentRemovedApplications = Graph.IncludeDependents(toBeRemovedKeys, oldDependencies).Except(toBeRemovedKeys);

            var toBeInsertedKeys = directlyInserted.Union(changedApplications).ToList();
            var newDependencies = GetDependencyPairs(newApplications).Select(dep => Tuple.Create(dep.Item1.GetConceptApplicationKey(), dep.Item2.GetConceptApplicationKey()));
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

        protected void ApplyChangesToDatabase(
            List<ConceptApplication> oldApplications, List<NewConceptApplication> newApplications,
            List<ConceptApplication> toBeRemoved, List<NewConceptApplication> toBeInserted)
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

        protected List<string> ApplyChangesToDatabase_Remove(List<ConceptApplication> toBeRemoved, List<ConceptApplication> oldApplications)
        {
            var newScripts = new List<string>();

            toBeRemoved = toBeRemoved.OrderBy(ca => ca.OldCreationOrder).ToList(); // TopologicalSort is stable sort, so it will keep this (original) order unless current dependencies direct otherwise.
            Graph.TopologicalSort(toBeRemoved, GetDependencyPairs(oldApplications)); // Concept's dependencies might have changed, without dropping and recreating the concept. It is important to compute up-to-date remove order, otherwise FK constraint FK_AppliedConceptDependsOn_DependsOn might fail.
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

        protected List<string> ApplyChangesToDatabase_Insert(List<NewConceptApplication> toBeInserted, List<NewConceptApplication> newApplications)
        {
            var newScripts = new List<string>();

            Graph.TopologicalSort(toBeInserted, GetDependencyPairs(newApplications));

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

        protected List<string> ApplyChangesToDatabase_Unchanged(List<NewConceptApplication> toBeInserted, List<NewConceptApplication> newApplications, List<ConceptApplication> oldApplications)
        {
            var newScripts = new List<string>();

            var indexInsertedConcepts = new HashSet<string>(toBeInserted.Select(ca => ca.GetConceptApplicationKey()));
            var unchangedApplications = newApplications
                .Where(ca => !indexInsertedConcepts.Contains(ca.GetConceptApplicationKey()));

            var oldApplicationsByKey = oldApplications.ToDictionary(oa => oa.GetConceptApplicationKey());

            foreach (var ca in unchangedApplications)
            {
                var updateMetadataSql = _sqlTransactionBatches.JoinScripts(_conceptApplicationRepository.UpdateMetadataSql(ca, oldApplicationsByKey[ca.GetConceptApplicationKey()]));
                if (updateMetadataSql.Count() > 0)
                {
                    LogDatabaseChanges(ca, "Updating metadata");
                    newScripts.AddRange(updateMetadataSql);
                }
            }

            return newScripts;
        }

        protected static string[] SplitSqlScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return new string[] { };
            return script.Split(new[] { SqlUtility.ScriptSplitterTag }, StringSplitOptions.RemoveEmptyEntries)
                .Where(query => !string.IsNullOrWhiteSpace(query))
                .Select(query => query.Trim()).ToArray();
        }

        protected void LogDatabaseChanges(ConceptApplication conceptApplication, string action, Func<string> additionalInfo = null)
        {
            _conceptsLogger.Trace("{0} {1}, ID={2}.{3}{4}",
                action,
                conceptApplication.GetConceptApplicationKey(),
                SqlUtility.GuidToString(conceptApplication.Id),
                additionalInfo != null ? " " : null,
                additionalInfo != null ? additionalInfo() : null);
        }

        protected void VerifyIntegrity()
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

    /// <summary>
    /// This concept implementation is used for concepts that have no database implementation.
    /// This is useful for handling dependencies between concept application in situations where one concept application depends on another concept info
    /// that has no implementation and which depends on a third concept application. First concept application should indirectly depend on third, even though there
    /// is no second concept application.  Such scenarios are easier to handle if every concept has its implementation.
    /// </summary>
    public class NullImplementation : IConceptDatabaseDefinition
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
    }
}