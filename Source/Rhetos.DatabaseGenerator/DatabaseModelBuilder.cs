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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Builds a <see cref="DatabaseModel"/> from DSL model and database code generators.
    /// </summary>
    public class DatabaseModelBuilder
    {
        private readonly IPluginsContainer<IConceptDatabaseDefinition> _plugins;
        private readonly IDslModel _dslModel;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly DatabaseModelDependencies _databaseModelDependencies;

        public DatabaseModelBuilder(
            IPluginsContainer<IConceptDatabaseDefinition> plugins,
            IDslModel dslModel,
            ILogProvider logProvider,
            DatabaseModelDependencies databaseModelDependencies)
        {
            _plugins = plugins;
            _dslModel = dslModel;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance");
            _databaseModelDependencies = databaseModelDependencies;
        }

        public DatabaseModel CreateDatabaseModel()
        {
            var stopwatch = Stopwatch.StartNew();

            var codeGenerators = CreateCodeGenerators();
            _performanceLogger.Write(stopwatch, $"{nameof(DatabaseModelBuilder)}.{nameof(CreateDatabaseModel)}: Created database objects from plugins.");

            var codeGeneratorDependencies = _databaseModelDependencies.ExtractCodeGeneratorDependencies(codeGenerators, _plugins);
            _performanceLogger.Write(stopwatch, $"{nameof(DatabaseModelBuilder)}.{nameof(CreateDatabaseModel)}: Computed dependencies.");

            ComputeCreateAndRemoveQuery(
                codeGenerators,
                codeGeneratorDependencies,
                _dslModel.Concepts,
                out var createQueryByCodeGenerator,
                out var removeQueryByCodeGenerator,
                out var sqlScriptDependencies);
            _performanceLogger.Write(stopwatch, $"{nameof(DatabaseModelBuilder)}.{nameof(CreateDatabaseModel)}: Generated SQL queries for new database objects.");

            var allDependencies = codeGeneratorDependencies.Concat(sqlScriptDependencies);
            var databaseObjects = ConstructDatabaseObjects(codeGenerators, createQueryByCodeGenerator, removeQueryByCodeGenerator, allDependencies);

            var duplicate = databaseObjects.GroupBy(dbObject => dbObject).Where(group => group.Count() > 1).FirstOrDefault();
            if (duplicate != null)
                throw new FrameworkException($"Duplicate generated database object: {duplicate.Key}, count {duplicate.Count()}.");

            _performanceLogger.Write(stopwatch, $"{nameof(DatabaseModelBuilder)}.{nameof(CreateDatabaseModel)}: Created database objects.");
            return new DatabaseModel { DatabaseObjects = databaseObjects };
        }

        private List<CodeGenerator> CreateCodeGenerators()
        {
            var codeGenerators = new List<CodeGenerator>();
            foreach (var conceptInfo in _dslModel.Concepts)
            {
                IConceptDatabaseDefinition[] implementations = _plugins.GetImplementations(conceptInfo.GetType()).ToArray();

                if (!implementations.Any())
                    implementations = new[] { new NullImplementation() };

                codeGenerators.AddRange(implementations.Select(
                    conceptImplementation => new CodeGenerator(conceptInfo, conceptImplementation)));
            }

            return codeGenerators;
        }

        private static List<DatabaseObject> ConstructDatabaseObjects(
            List<CodeGenerator> codeGenerators,
            Dictionary<int, string> createQueryByCodeGenerator,
            Dictionary<int, string> removeQueryByCodeGenerator,
            IEnumerable<CodeGeneratorDependency> allDependencies)
        {
            var allDependenciesByCodeGenerator = allDependencies.ToMultiDictionary(d => d.Dependent.Id, d => d.DependsOn);

            var databaseObjectByCodeGenerator = codeGenerators.ToDictionary(
                cg => cg.Id,
                cg => new DatabaseObject
                {
                    ConceptInfoTypeName = cg.ConceptInfo.GetType().AssemblyQualifiedName,
                    ConceptInfoKey = cg.ConceptInfo.GetKey(),
                    ConceptImplementationTypeName = cg.ConceptImplementation.GetType().AssemblyQualifiedName,
                    DependsOn = null, // It will be updated later. All database objects must be constructed first, because DependsOn will reference other instances.
                    CreateQuery = createQueryByCodeGenerator.GetValueOrDefault(cg.Id) ?? "",
                    RemoveQuery = removeQueryByCodeGenerator.GetValueOrDefault(cg.Id) ?? "",
                });

            foreach (var ca in databaseObjectByCodeGenerator)
            {
                var codeGeneratorId = ca.Key;
                var generatedDatabaseObject = ca.Value;

                var dependsOnCodeGenerators = allDependenciesByCodeGenerator.GetValueOrDefault(codeGeneratorId);
                var dependsOnDatabaseObjects = dependsOnCodeGenerators
                    ?.Distinct()
                    ?.Where(cg => cg.Id != codeGeneratorId) // Remove any self-reference.
                    ?.Select(cg => databaseObjectByCodeGenerator[cg.Id]).ToArray()
                    ?? Array.Empty<DatabaseObject>();

                generatedDatabaseObject.DependsOn = dependsOnDatabaseObjects;
            }

            var databaseObjects = databaseObjectByCodeGenerator.Values.ToList();
            return databaseObjects;
        }

        private void ComputeCreateAndRemoveQuery(
            List<CodeGenerator> codeGenerators,
            List<CodeGeneratorDependency> codeGeneratorDependencies,
            IEnumerable<IConceptInfo> allConceptInfos,
            out Dictionary<int, string> createQueryByCodeGenerator,
            out Dictionary<int, string> removeQueryByCodeGenerator,
            out List<CodeGeneratorDependency> sqlScriptDependencies)
        {
            // Generate RemoveQuery:

            removeQueryByCodeGenerator = codeGenerators.ToDictionary(
                cg => cg.Id,
                cg => cg.ConceptImplementation.RemoveDatabaseStructure(cg.ConceptInfo)?.Trim() ?? "");

            // Generate CreateQuery:

            Graph.TopologicalSort(codeGenerators, codeGeneratorDependencies.Select(d => Tuple.Create(d.DependsOn, d.Dependent)));
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            var createdDependencies = new List<(IConceptInfo DependsOn, IConceptInfo Dependent)>();
            var conceptInfosByKey = allConceptInfos.ToDictionary(ci => ci.GetKey());

            foreach (var cg in codeGenerators)
            {
                string createQuery = cg.ConceptImplementation.CreateDatabaseStructure(cg.ConceptInfo);
                if (!string.IsNullOrWhiteSpace(createQuery))
                {
                    sqlCodeBuilder.InsertCode(GetCodeGeneratorSeparator(cg.Id));
                    sqlCodeBuilder.InsertCode(createQuery);
                }

                if (cg.ConceptImplementation is IConceptDatabaseDefinitionExtension conceptDatabaseDefinitionExtension)
                {
                    conceptDatabaseDefinitionExtension.ExtendDatabaseStructure(
                        cg.ConceptInfo, sqlCodeBuilder, out var pluginCreatedDependencies);

                    if (pluginCreatedDependencies != null)
                        createdDependencies.AddRange(pluginCreatedDependencies
                            .Select(dep =>
                            (
                                DependsOn: GetValidConceptInfo(dep.Item1.GetKey(), conceptInfosByKey, cg),
                                Dependent: GetValidConceptInfo(dep.Item2.GetKey(), conceptInfosByKey, cg)
                            )));
                }
            }

            createQueryByCodeGenerator = ExtractCreateQueries(sqlCodeBuilder.GeneratedCode);

            sqlScriptDependencies = _databaseModelDependencies.ConceptDependencyToCodeGeneratorsDependency(
                createdDependencies.Select(d => Tuple.Create(d.DependsOn, d.Dependent)),
                codeGenerators);

            var reportDependencies = sqlScriptDependencies;
            _logger.Trace(() => _databaseModelDependencies.ReportDependencies("SQL script", reportDependencies));
        }

        private static IConceptInfo GetValidConceptInfo(string conceptInfoKey, Dictionary<string, IConceptInfo> conceptInfosByKey, CodeGenerator debugContextNewDatabaseObject)
        {
            if (!conceptInfosByKey.ContainsKey(conceptInfoKey))
                throw new FrameworkException(string.Format(
                    "DatabaseGenerator error while generating code with plugin {0}: Extension created a dependency to the nonexistent concept info {1}.",
                    debugContextNewDatabaseObject.ConceptImplementation.GetType().Name,
                    conceptInfoKey));
            return conceptInfosByKey[conceptInfoKey];
        }

        private const string DatabaseObjectSeparatorPrefix = "\r\n--RhetosDatabaseObjectSeparator ";
        private const string DatabaseObjectSeparatorSuffix = "\r\n";

        private static string GetCodeGeneratorSeparator(int codeGeneratorId)
        {
            return DatabaseObjectSeparatorPrefix + codeGeneratorId + DatabaseObjectSeparatorSuffix;
        }

        private static Dictionary<int, string> ExtractCreateQueries(string generatedSqlCode)
        {
            var generatedScripts = generatedSqlCode.Split(new[] { DatabaseObjectSeparatorPrefix }, StringSplitOptions.None).ToList();
            if (generatedScripts.Count > 0 && generatedScripts[0].Length > 0)
                throw new FrameworkException($"Unexpected generated script format: The first segment should be empty:\r\n{generatedScripts[0].Limit(200)}");

            return generatedScripts.Skip(1)
                .Select(ParseGeneratedScript)
                .ToDictionary(script => script.CodeGeneratorId, script => script.Sql);
        }

        private static (int CodeGeneratorId, string Sql) ParseGeneratedScript(string generatedScript)
        {
            int scriptKeyEnd = generatedScript.IndexOf(DatabaseObjectSeparatorSuffix);
            if (scriptKeyEnd < 0)
                throw new FrameworkException($"Unexpected generated script format: Missing {nameof(DatabaseObjectSeparatorSuffix)}.");
            if (scriptKeyEnd == 0)
                throw new FrameworkException($"Unexpected generated script format: Missing script key.");

            return
            (
                CodeGeneratorId: int.Parse(generatedScript.Substring(0, scriptKeyEnd)),
                Sql: generatedScript.Substring(scriptKeyEnd + DatabaseObjectSeparatorSuffix.Length).Trim()
            );
        }
    }
}