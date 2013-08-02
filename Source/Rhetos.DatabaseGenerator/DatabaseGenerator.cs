/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Factory;
using Rhetos.Compiler;
using Rhetos.Logging;

namespace Rhetos.DatabaseGenerator
{
    public class DatabaseGenerator : IDatabaseGenerator
    {
        protected readonly ISqlExecuter _sqlExecuter;
        protected readonly IDslModel _dslModel;
        protected readonly IPluginsContainer<IConceptDatabaseDefinition> _plugins;
        protected readonly ConceptApplicationRepository _conceptApplicationRepository;
        protected readonly ILogger _logger;
        protected readonly ILogger _performanceLogger;

        protected bool DatabaseUpdated = false;

        protected readonly object _databaseUpdateLock = new object();

        public DatabaseGenerator(
            ISqlExecuter sqlExecuter, 
            IDslModel dslModel,
            IPluginsContainer<IConceptDatabaseDefinition> plugins,
            ConceptApplicationRepository conceptApplicationRepository,
            ILogProvider logProvider)
        {
            _sqlExecuter = sqlExecuter;
            _dslModel = dslModel;
            _plugins = plugins;
            _conceptApplicationRepository = conceptApplicationRepository;
            _logger = logProvider.GetLogger("DatabaseGenerator");
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public string UpdateDatabaseStructure()
        {
            if (DatabaseUpdated) // performance optimization
                return "Database already updated.";

            lock (_databaseUpdateLock)
            {
                if (DatabaseUpdated)
                    return "Database already updated.";

                _logger.Trace("Updating database structure.");
                var stopwatchTotal = Stopwatch.StartNew();
                var stopwatch = Stopwatch.StartNew();

                var oldApplications = _conceptApplicationRepository.Load();
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Loaded old concept applications.");

                var newApplications = CreateNewApplications();
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Created new concept applications.");
                ConceptApplicationRepository.CheckKeyUniqueness(newApplications, "created");
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Verify new concept applications' integrity.");
                newApplications = TrimEmptyApplications(newApplications);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Removed unsused concept applications.");

                List<ConceptApplication> toBeRemoved;
                List<NewConceptApplication> toBeInserted;
                CalculateApplicationsToBeRemovedAndInserted(oldApplications, newApplications, out toBeRemoved, out toBeInserted, _logger);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Analized differences in database structure.");

                var report = ApplyChangesToDatabase(oldApplications, newApplications, toBeRemoved, toBeInserted);
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Applied changes to database.");

                VerifyIntegrity();
                _performanceLogger.Write(stopwatch, "DatabaseGenerator: Verified integrity of saved concept applications metadata.");

                _performanceLogger.Write(stopwatchTotal, "DatabaseGenerator.UpdateDatabaseStructure");
                DatabaseUpdated = true;
                return report;
            }
        }

        protected List<NewConceptApplication> TrimEmptyApplications(List<NewConceptApplication> newApplications)
        {
            var emptyCreateQuery = newApplications.Where(ca => string.IsNullOrWhiteSpace(ca.CreateQuery)).ToList();
            var emptyCreateHasRemove = emptyCreateQuery.FirstOrDefault(ca => !string.IsNullOrWhiteSpace(ca.RemoveQuery));
            if (emptyCreateHasRemove != null)
                throw new FrameworkException("A concept that does not create database objects (CreateDatabaseStructure) cannot remove them (RemoveDatabaseStructure): "
                    + emptyCreateHasRemove.GetConceptApplicationKey() + ".");

            var removeLeaves = DirectedGraph.RemovableLeaves(emptyCreateQuery, GetDependencyPairs(newApplications));

            foreach (var remove in removeLeaves)
            {
                var r = remove;
                _logger.Trace(() => "Removing empty leaf concept application " + r + ".");
            }
            return newApplications.Except(removeLeaves).ToList();
        }

        protected List<NewConceptApplication> CreateNewApplications()
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
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: Created concept applications from plugins.");

            ComputeDependsOn(conceptApplications);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: Computed depencies.");

            ComputeCreateAndRemoveQuery(conceptApplications, _dslModel.Concepts);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.CreateNewApplications: Generated SQL queries for new concept applications.");

            return conceptApplications;
        }

        protected void ComputeDependsOn(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            foreach (var conceptApplication in newConceptApplications)
                conceptApplication.DependsOn = new ConceptApplication[] {};

            var dependencies = ExtractDependencies(newConceptApplications);
            UpdateConceptApplicationsFromDependencyList(dependencies);
        }

        /// <summary>
        /// Updates ConceptApplication.DependsOn property from "flat" list of dependencies.
        /// </summary>
        protected static void UpdateConceptApplicationsFromDependencyList(IEnumerable<Dependency> dependencies)
        {
            var dependenciesByConceptApplication = dependencies
                .GroupBy(d => d.Dependent, d => d.DependsOn);

            foreach (var dependencyGroup in dependenciesByConceptApplication)
            {
                var dependent = dependencyGroup.Key;
                var newDependsOn = dependencyGroup.Distinct().Union(dependent.DependsOn);

                dependent.DependsOn = newDependsOn.ToArray();
            }
        }

        protected IEnumerable<Dependency> ExtractDependencies(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            return ExtractDependenciesFromConceptInfos(newConceptApplications)
                    .Union(ExtractDependenciesFromMefPluginMetadata(_plugins, newConceptApplications)).ToList();
        }

        protected static IEnumerable<Dependency> ExtractDependenciesFromConceptInfos(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            var conceptInfos = newConceptApplications.Select(conceptApplication => conceptApplication.ConceptInfo).Distinct();

            var conceptInfoDependencies = conceptInfos.SelectMany(conceptInfo => conceptInfo.GetAllDependencies()
                    .Select(dependency => Tuple.Create(dependency, conceptInfo)));

            return GetConceptApplicationDependencies(conceptInfoDependencies, newConceptApplications);
        }

        protected static IEnumerable<Dependency> GetConceptApplicationDependencies(IEnumerable<Tuple<IConceptInfo, IConceptInfo>> conceptInfoDependencies, IEnumerable<ConceptApplication> conceptApplications)
        {
            var conceptApplicationsByConceptInfoKey = conceptApplications
                .GroupBy(ca => ca.ConceptInfoKey)
                .ToDictionary(g => g.Key, g => g.ToList());

            var conceptInfoKeyDependencies = conceptInfoDependencies.Select(dep => Tuple.Create(dep.Item1.GetKey(), dep.Item2.GetKey()));

            var conceptApplicationDependencies =
                from conceptInfoKeyDependency in conceptInfoKeyDependencies
                where conceptApplicationsByConceptInfoKey.ContainsKey(conceptInfoKeyDependency.Item1)
                      && conceptApplicationsByConceptInfoKey.ContainsKey(conceptInfoKeyDependency.Item2)
                from dependsOnConceptApplication in conceptApplicationsByConceptInfoKey[conceptInfoKeyDependency.Item1]
                from dependentConceptApplication in conceptApplicationsByConceptInfoKey[conceptInfoKeyDependency.Item2]
                select new Dependency
                           {
                               DependsOn = dependsOnConceptApplication,
                               Dependent = dependentConceptApplication
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
                        dependencies);

            return dependencies.Distinct().ToList();
        }

        protected static IEnumerable<Tuple<Type, Type>> GetImplementationDependencies(IPluginsContainer<IConceptDatabaseDefinition> plugins, IEnumerable<Type> conceptImplementations)
        {
            var dependencies = new List<Tuple<Type, Type>>();

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

                dependencies.Add(Tuple.Create(dependency, conceptImplementation));
            }

            return dependencies;
        }

        protected static void AddDependenciesOnSameConceptInfo(
            IEnumerable<ConceptApplication> applications1,
            IEnumerable<ConceptApplication> applications2,
            List<Dependency> dependencies)
        {
            var applications2ByConceptInfoKey = applications2.ToDictionary(a => a.ConceptInfoKey);
            dependencies.AddRange(from application1 in applications1
                where applications2ByConceptInfoKey.ContainsKey(application1.ConceptInfoKey)
                select new Dependency
                           {
                               DependsOn = application1, Dependent = applications2ByConceptInfoKey[application1.ConceptInfoKey]
                           });
        }

        public static IConceptDatabaseDefinition CreateDatabasePluginInstance(ITypeFactory typeFactory, Type databaseGeneratorPluginType)
        {
            var pluginInstance = typeFactory.CreateInstance(databaseGeneratorPluginType) as IConceptDatabaseDefinition;
            if (pluginInstance == null)
                throw new FrameworkException(string.Format(CultureInfo.InvariantCulture,
                    "Could not create instance of \"{0}\" from type \"{1}\". Make sure that this type implements {2} interface compatible with currently used {2} interface.",
                    typeof(IConceptDatabaseDefinition).AssemblyQualifiedName,
                    databaseGeneratorPluginType.AssemblyQualifiedName,
                    typeof(IConceptDatabaseDefinition).Name));
            return pluginInstance;
        }

        protected void ComputeCreateAndRemoveQuery(List<NewConceptApplication> newConceptApplications, IEnumerable<IConceptInfo> allConceptInfos)
        {
            DirectedGraph.TopologicalSort(newConceptApplications, GetDependencyPairs(newConceptApplications));

            var conceptInfosByKey = allConceptInfos.ToDictionary(ci => ci.GetKey());

            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            var createdDependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();
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
                        var caLocal = ca;

                        var resolvedDependencies = pluginCreatedDependencies.Select(dep => Tuple.Create(
                            GetValidConceptInfo(dep.Item1.GetKey(), conceptInfosByKey, caLocal),
                            GetValidConceptInfo(dep.Item2.GetKey(), conceptInfosByKey, caLocal))).ToList();

                        _logger.Trace(() => "Created dependencies for " + caLocal + " are: " + string.Join(", ", resolvedDependencies.Select(
                            dep => dep.Item1.GetShortDescription() + "-" + dep.Item2.GetShortDescription())) + ".");
                        
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
                    "DatabaseGenerator error while generating code with plugin {0}: Plugin created dependency nonexisting concept info {1}.",
                    debugContextNewConceptApplication.ConceptImplementationType.Name,
                    conceptInfoKey));
            return conceptInfosByKey[conceptInfoKey];
        }

        /// <returns>Item2 depends on item1.</returns>
        protected static List<Tuple<NewConceptApplication, NewConceptApplication>> GetDependencyPairs(IEnumerable<NewConceptApplication> conceptApplications)
        {
            return conceptApplications.SelectMany(
                dependent => dependent.DependsOn.Select(dependency => Tuple.Create((NewConceptApplication)dependency, dependent))
                ).ToList();
        }

        /// <returns>Item2 depends on item1.</returns>
        protected static List<Tuple<ConceptApplication, ConceptApplication>> GetDependencyPairs(IEnumerable<ConceptApplication> conceptApplications)
        {
            return conceptApplications.SelectMany(
                dependent => dependent.DependsOn.Select(dependency => Tuple.Create(dependency, dependent))
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

        protected static void CalculateApplicationsToBeRemovedAndInserted(
            IEnumerable<ConceptApplication> oldApplications, IEnumerable<NewConceptApplication> newApplications,
            out List<ConceptApplication> toBeRemoved, out List<NewConceptApplication> toBeInserted,
            ILogger logger)
        {
            var oldApplicationsByKey = oldApplications.ToDictionary(a => a.GetConceptApplicationKey());
            var newApplicationsByKey = newApplications.ToDictionary(a => a.GetConceptApplicationKey());


            // Find directly inserted and removed concept applications:
            var directlyRemoved = oldApplicationsByKey.Keys.Except(newApplicationsByKey.Keys).ToList();
            var directlyInserted = newApplicationsByKey.Keys.Except(oldApplicationsByKey.Keys).ToList();

            foreach (string ca in directlyRemoved)
                logger.Trace("Directly removed concept application: " + ca);
            foreach (string ca in directlyInserted)
                logger.Trace("Directly inserted concept application: " + ca);
            

            // Find changed concept applications (different create sql query):
            var existingApplications = oldApplicationsByKey.Keys.Intersect(newApplicationsByKey.Keys).ToList();
            var changedApplications = existingApplications.Where(appKey => !string.Equals(
                oldApplicationsByKey[appKey].CreateQuery,
                newApplicationsByKey[appKey].CreateQuery)).ToList();

            foreach (string ca in changedApplications)
                logger.Trace("Changed concept application: " + ca);


            // Find dependent concepts applications to be regenerated:
            var toBeRemovedKeys = directlyRemoved.Union(changedApplications).ToList();
            var oldDependencies = GetDependencyPairs(oldApplications).Select(dep => Tuple.Create(dep.Item1.GetConceptApplicationKey(), dep.Item2.GetConceptApplicationKey()));
            var dependentRemovedApplications = DirectedGraph.IncludeDependents(toBeRemovedKeys, oldDependencies).Except(toBeRemovedKeys);

            var toBeInsertedKeys = directlyInserted.Union(changedApplications).ToList();
            var newDependencies = GetDependencyPairs(newApplications).Select(dep => Tuple.Create(dep.Item1.GetConceptApplicationKey(), dep.Item2.GetConceptApplicationKey()));
            var dependentInsertedApplications = DirectedGraph.IncludeDependents(toBeInsertedKeys, newDependencies).Except(toBeInsertedKeys);

            var refreshDependents = dependentRemovedApplications.Union(dependentInsertedApplications).ToList();
            toBeRemovedKeys.AddRange(refreshDependents.Intersect(oldApplicationsByKey.Keys));
            toBeInsertedKeys.AddRange(refreshDependents.Intersect(newApplicationsByKey.Keys));

            foreach (string ca in refreshDependents)
                logger.Trace("Dependent on changed concept application: " + ca);


            // Result:
            toBeRemoved = toBeRemovedKeys.Select(key => oldApplicationsByKey[key]).ToList();
            toBeInserted = toBeInsertedKeys.Select(key => newApplicationsByKey[key]).ToList();
        }

        protected string ApplyChangesToDatabase(
            List<ConceptApplication> oldApplications, List<NewConceptApplication> newApplications,
            List<ConceptApplication> toBeRemoved, List<NewConceptApplication> toBeInserted)
        {
            var stopwatch = Stopwatch.StartNew();

            int estimatedNumberOfQueries = Math.Max(oldApplications.Count, newApplications.Count) * 3;
            var allSql = new List<string>(estimatedNumberOfQueries);

            int reportRemovedCount = ApplyChangesToDatabase_Remove(allSql, toBeRemoved);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.ApplyChangesToDatabase: Prepared SQL scripts for removing concept applications.");

            ApplyChangesToDatabase_Unchanges(allSql, toBeInserted, newApplications);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.ApplyChangesToDatabase: Prepared SQL scripts for updating unchanged concept applications' metadata.");

            int reportInsertedCount = ApplyChangesToDatabase_Insert(allSql, toBeInserted, newApplications);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.ApplyChangesToDatabase: Prepared SQL scripts for inserting concept applications.");

            _sqlExecuter.ExecuteSql(allSql);
            _performanceLogger.Write(stopwatch, "DatabaseGenerator.ApplyChangesToDatabase: Executed " + allSql.Count + " SQL scripts.");

            string report = "Removed " + reportRemovedCount + ", inserted " + reportInsertedCount + " concept applications.";
            _logger.Trace(() => "Report: " + report);
            return report;
        }

        protected int ApplyChangesToDatabase_Remove(List<string> allSql, List<ConceptApplication> toBeRemoved)
        {
            toBeRemoved.Sort((ca1, ca2) => ca2.OldCreationOrder - ca1.OldCreationOrder);

            int reportRemovedCount = 0;
            foreach (var ca in toBeRemoved)
            {
                Log(ca, "Removing previously applied concept");

                string[] removeSqlScripts = SplitSqlScript(ca.RemoveQuery);
                allSql.AddRange(removeSqlScripts);
                if (removeSqlScripts.Length > 0)
                    reportRemovedCount++;

                allSql.Add(ConceptApplicationRepository.DeleteMetadataSql(ca));
            }
            return reportRemovedCount;
        }

        protected int ApplyChangesToDatabase_Insert(List<string> allSql, List<NewConceptApplication> toBeInserted, List<NewConceptApplication> newApplications)
        {
            DirectedGraph.TopologicalSort(toBeInserted, GetDependencyPairs(newApplications));

            int reportInsertedCount = 0;
            foreach (var ca in toBeInserted)
            {
                Log(ca, "Adding new concept");

                string[] createSqlScripts = SplitSqlScript(ca.CreateQuery);
                allSql.AddRange(createSqlScripts);
                if (createSqlScripts.Length > 0)
                    reportInsertedCount++;

                allSql.AddRange(ConceptApplicationRepository.InsertMetadataSql(ca));
            }
            return reportInsertedCount;
        }

        protected void ApplyChangesToDatabase_Unchanges(List<string> allSql, List<NewConceptApplication> toBeInserted, List<NewConceptApplication> newApplications)
        {
            // Metadata must be updated for unchanges concept applications, not only those removed and inserted, because their IDs or dependencies could change:

            allSql.Add(ConceptApplicationRepository.DeleteAllMetadataSql());

            DirectedGraph.TopologicalSort(newApplications, GetDependencyPairs(newApplications));

            var indexInsertedConcepts = new HashSet<string>(toBeInserted.Select(ca => ca.GetConceptApplicationKey()));
            allSql.AddRange(newApplications
                .Where(ca => !indexInsertedConcepts.Contains(ca.GetConceptApplicationKey()))
                .SelectMany(unchangedCa => ConceptApplicationRepository.InsertMetadataSql(unchangedCa)));
        }

        protected static string[] SplitSqlScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return new string[] { };
            return script.Split(new[] { SqlUtility.ScriptSplitter }, StringSplitOptions.RemoveEmptyEntries)
                .Where(query => !string.IsNullOrWhiteSpace(query))
                .Select(query => query.Trim()).ToArray();
        }

        protected void Log(ConceptApplication conceptApplication, string action)
        {
            _logger.Info("{0} {1}, ID={2}.", action, conceptApplication.GetConceptApplicationKey(), SqlUtility.GuidToString(conceptApplication.Id));
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
    /// that has no implementation and which depens on third concept application. First concept application should indirectly depend on third, even thow there
    /// is no second concept application.  Such scenarios are easier to handle if every concept has its implementation.
    /// </summary>
    public class NullImplementation : IConceptDatabaseDefinition
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
    }
}