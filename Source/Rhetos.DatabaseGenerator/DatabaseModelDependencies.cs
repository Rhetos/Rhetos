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

using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Utility class with helper methods for handling dependencies between code generators.
    /// </summary>
    public class DatabaseModelDependencies
    {
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public DatabaseModelDependencies(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public List<CodeGeneratorDependency> ExtractCodeGeneratorDependencies(
            IEnumerable<CodeGenerator> codeGenerators,
            IPluginsContainer<IConceptDatabaseDefinition> plugins)
        {
            var dependenciesFromConceptInfo = ExtractDependenciesFromConceptInfos(codeGenerators);
           
            var dependenciesFromMefPluginMetadata = ExtractDependenciesFromMefPluginMetadata(plugins, codeGenerators);
            
            return dependenciesFromConceptInfo.Union(dependenciesFromMefPluginMetadata).ToList();
        }

        private List<CodeGeneratorDependency> ExtractDependenciesFromConceptInfos(
            IEnumerable<CodeGenerator> codeGenerators)
        {
            var stopwatch = Stopwatch.StartNew();

            var conceptInfos = codeGenerators.Select(codeGenerator => codeGenerator.ConceptInfo).Distinct();
            var conceptInfoDependencies = conceptInfos.SelectMany(conceptInfo => conceptInfo.GetAllDependencies()
                .Select(dependency => Tuple.Create(dependency, conceptInfo)));
            var dependencies = ConceptDependencyToCodeGeneratorsDependency(conceptInfoDependencies, codeGenerators);

            _performanceLogger.Write(stopwatch, $"{nameof(DatabaseModelDependencies)}.{nameof(ExtractDependenciesFromConceptInfos)}.");
            _logger.Trace(() => ReportDependencies("Direct or indirect IConceptInfo reference", dependencies));
            return dependencies;
        }

        public List<CodeGeneratorDependency> ConceptDependencyToCodeGeneratorsDependency(
            IEnumerable<Tuple<IConceptInfo, IConceptInfo>> conceptInfoDependencies,
            IEnumerable<CodeGenerator> codeGenerators)
        {
            var codeGeneratorsByConceptInfoKey = codeGenerators
                .GroupBy(ca => ca.ConceptInfo.GetKey())
                .ToDictionary(g => g.Key, g => g.ToList());

            var conceptInfoKeyDependencies = conceptInfoDependencies
                .Select(dep => Tuple.Create(dep.Item1.GetKey(), dep.Item2.GetKey()));

            var codeGeneratorDependencies =
                from conceptInfoKeyDependency in conceptInfoKeyDependencies
                where codeGeneratorsByConceptInfoKey.ContainsKey(conceptInfoKeyDependency.Item1)
                      && codeGeneratorsByConceptInfoKey.ContainsKey(conceptInfoKeyDependency.Item2)
                from dependsOnCodeGenerator in codeGeneratorsByConceptInfoKey[conceptInfoKeyDependency.Item1]
                from dependentCodeGenerator in codeGeneratorsByConceptInfoKey[conceptInfoKeyDependency.Item2]
                select new CodeGeneratorDependency
                    {
                        DependsOn = dependsOnCodeGenerator,
                        Dependent = dependentCodeGenerator,
                    };

            return codeGeneratorDependencies.ToList();
        }

        private List<CodeGeneratorDependency> ExtractDependenciesFromMefPluginMetadata(
            IPluginsContainer<IConceptDatabaseDefinition> plugins,
            IEnumerable<CodeGenerator> codeGenerators)
        {
            var stopwatch = Stopwatch.StartNew();
            var dependencies = new List<CodeGeneratorDependency>();

            var codeGeneratorsByImplementationType = codeGenerators
                .GroupBy(ca => ca.ConceptImplementation.GetType())
                .ToDictionary(g => g.Key, g => g.ToList());

            var distinctConceptImplementations = codeGenerators.Select(ca => ca.ConceptImplementation.GetType()).Distinct().ToList();

            var implementationDependencies = GetImplementationDependencies(plugins, distinctConceptImplementations);

            foreach (var implementationDependency in implementationDependencies)
                if (codeGeneratorsByImplementationType.ContainsKey(implementationDependency.Item1)
                    && codeGeneratorsByImplementationType.ContainsKey(implementationDependency.Item2))
                    AddDependenciesOnSameConceptInfo(
                        codeGeneratorsByImplementationType[implementationDependency.Item1],
                        codeGeneratorsByImplementationType[implementationDependency.Item2],
                        dependencies);

            dependencies = dependencies.Distinct().ToList();

            _performanceLogger.Write(stopwatch, $"{nameof(DatabaseModelDependencies)}.{nameof(ExtractDependenciesFromMefPluginMetadata)}.");
            _logger.Trace(() => ReportDependencies("MefPlugin DependsOn", dependencies));

            return dependencies;
        }

        private List<Tuple<Type, Type>> GetImplementationDependencies(IPluginsContainer<IConceptDatabaseDefinition> plugins, IEnumerable<Type> conceptImplementations)
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

        private void AddDependenciesOnSameConceptInfo(
            IEnumerable<CodeGenerator> codeGenerators1,
            IEnumerable<CodeGenerator> codeGenerators2,
            List<CodeGeneratorDependency> dependencies)
        {
            var codeGenerators2ByConceptInfoKey = codeGenerators2.ToDictionary(cg => cg.ConceptInfo.GetKey());
            dependencies.AddRange(from codeGenerator1 in codeGenerators1
                where codeGenerators2ByConceptInfoKey.ContainsKey(codeGenerator1.ConceptInfo.GetKey())
                select new CodeGeneratorDependency
                    {
                        DependsOn = codeGenerator1,
                        Dependent = codeGenerators2ByConceptInfoKey[codeGenerator1.ConceptInfo.GetKey()],
                    });
        }

        public string ReportDependencies(string title, List<CodeGeneratorDependency> codeGeneratorDependencies)
        {
            var report = new StringBuilder();
            report.Append($"{title} dependencies:");
            foreach (var dependentGroup in codeGeneratorDependencies.GroupBy(d => d.Dependent))
            {
                report.Append($"\r\n{dependentGroup.Key} depends on:");
                foreach (var dependency in dependentGroup)
                    report.Append($"\r\n- {dependency.DependsOn}");
            }
            return report.ToString();
        }
    }
}