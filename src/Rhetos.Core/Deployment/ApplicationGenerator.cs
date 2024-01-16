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

namespace Rhetos.Deployment
{
    public class ApplicationGenerator
    {
        private readonly ILogProvider _logProvider;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IPluginsContainer<IGenerator> _generatorsContainer;
        private readonly RhetosBuildEnvironment _buildEnvironment;
        private readonly FilesUtility _filesUtility;
        private readonly ISourceWriter _sourceWriter;
        private readonly BuildOptions _buildOptions;

        public ApplicationGenerator(
            ILogProvider logProvider,
            IPluginsContainer<IGenerator> generatorsContainer,
            RhetosBuildEnvironment buildEnvironment,
            FilesUtility filesUtility,
            ISourceWriter sourceWriter,
            BuildOptions buildOptions)
        {
            _logProvider = logProvider;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _generatorsContainer = generatorsContainer;
            _buildEnvironment = buildEnvironment;
            _filesUtility = filesUtility;
            _sourceWriter = sourceWriter;
            _buildOptions = buildOptions;
        }

        public void ExecuteGenerators()
        {
            _filesUtility.EmptyDirectory(_buildEnvironment.GeneratedAssetsFolder);
            _filesUtility.SafeCreateDirectory(_buildEnvironment.CacheFolder); // Cache is not deleted between builds.
            if (!string.IsNullOrEmpty(_buildEnvironment.GeneratedSourceFolder))
                _filesUtility.SafeCreateDirectory(_buildEnvironment.GeneratedSourceFolder); // Obsolete source files will be cleaned later. Keeping the existing files to allowing source change detection in Visual Studio.

            var generators = _generatorsContainer.GetPlugins()
                .OrderBy(DslParserPriority)
                .ToArray();
            var job = PrepareGeneratorsJob(generators);

            _logger.Trace(() => $"Starting parallel execution of {generators.Length} generators.");
            if (_buildOptions.MaxExecuteGeneratorsParallelism > 0)
                _logger.Info(() => $"Using max {_buildOptions.MaxExecuteGeneratorsParallelism} degree of parallelism from configuration.");

            var sw = Stopwatch.StartNew();
            job.RunAllTasks(_buildOptions.MaxExecuteGeneratorsParallelism);
            _performanceLogger.Write(sw, $"Executed {generators.Length} generators.");

            if (!string.IsNullOrEmpty(_buildEnvironment.GeneratedSourceFolder))
                _sourceWriter.CleanUp();
        }

        /// <summary>
        /// Generate DSL syntax files earlier, to make sure that DSL IntelliSense will work even if there are syntax errors in DSL script or if any other IGenerator fails.
        /// </summary>
        /// <remarks>
        /// The generators are already sorted by some of their dependencies in <see cref="IPluginsContainer{TPlugin}.GetPlugins"/>,
        /// but we can ignore that initial sort here, because the dependencies are handled here in a completely different way
        /// in <see cref="PrepareGeneratorsJob"/> method.
        /// </remarks>
        private int DslParserPriority(IGenerator generator)
        {
            switch (generator.GetType().FullName)
            {
                case "Rhetos.Dsl.DslSyntaxFileGenerator":
                    return 1;
                case "Rhetos.Dsl.DslDocumentationFileGenerator":
                    return 2;
                default:
                    return 3;
            }
        }

        private ParallelJob PrepareGeneratorsJob(IList<IGenerator> generators)
        {
            var allDependencies = ResolveDependencies(generators);
            var validDependencies = FilterInvalidDependencies(generators, allDependencies);

            var job = new ParallelJob(_logProvider);
            foreach (var generator in generators)
            {
                var generatorName = GetGeneratorName(generator);
                validDependencies.TryGetValue(generatorName, out var generatorDependencies);

                job.AddTask(generatorName, () =>
                {
                    _logger.Info(() => $"Starting {generatorName}.");
                    var sw = Stopwatch.StartNew();
                    generator.Generate();
                    _performanceLogger.Write(sw, () => $"{generatorName} completed.");
                }, generatorDependencies ?? new List<string>());
            }

            return job;
        }

        private Dictionary<string, List<string>> ResolveDependencies(IList<IGenerator> generators)
        {
            var explicitDependencies = generators
                .Where(generator => generator.Dependencies != null)
                .SelectMany(generator => generator.Dependencies.Select(dependency => (name: GetGeneratorName(generator), dependency)));
            Log("Explicit dependencies", explicitDependencies);

            var mefDependencies = generators
                .Select(generator => (name: GetGeneratorName(generator), dependency: _generatorsContainer.GetMetadata(generator, MefProvider.DependsOn)?.FullName))
                .Where(pair => pair.dependency != null);
            Log("MEF dependencies", mefDependencies);

            var configurationDependencies = ParseAdditionalDependenciesFromConfiguration();
            Log("Configuration dependencies", configurationDependencies);

            var allPairs = explicitDependencies
                .Concat(mefDependencies)
                .Concat(configurationDependencies);

            return allPairs
                .GroupBy(pair => pair.name)
                .ToDictionary(group => group.Key, group => group.Select(pair => pair.dependency).Distinct().ToList());
        }

        private void Log(string title, IEnumerable<(string name, string dependency)> dependencies)
        {
            _logger.Trace(() => $"{title}: {string.Join(", ", dependencies.Select(d => $"{d.name} -> {d.dependency}"))}.");
        }

        private Dictionary<string, List<string>> FilterInvalidDependencies(IList<IGenerator> generators, Dictionary<string, List<string>> dependencies)
        {
            var validGenerators = new HashSet<string>(generators.Select(GetGeneratorName));
            var validDependencies = new Dictionary<string, List<string>>();

            foreach (var generatorDependencies in dependencies)
            {
                if (validGenerators.Contains(generatorDependencies.Key))
                {
                    var invalidDependenciesForName = generatorDependencies.Value
                        .Where(dependency => !validGenerators.Contains(dependency))
                        .ToList();

                    if (invalidDependenciesForName.Count != 0)
                    {
                        string InvalidDependenciesInfo() => string.Join(", ", invalidDependenciesForName.Select(dependency => $"'{dependency}'"));
                        _logger.Warning(() => $"Invalid dependencies specified for generator '{generatorDependencies.Key}': {InvalidDependenciesInfo()}.");
                    }
                    validDependencies.Add(generatorDependencies.Key, generatorDependencies.Value.Except(invalidDependenciesForName).ToList());
                }
                else
                {
                    _logger.Warning(() => $"Invalid generator name '{generatorDependencies.Key}' encountered in generator dependencies.");
                }
            }

            return validDependencies;
        }

        private List<(string name, string dependency)> ParseAdditionalDependenciesFromConfiguration()
        {
            var pairs = new List<(string name, string dependency)>();

            if (_buildOptions.AdditionalGeneratorDependencies == null || !_buildOptions.AdditionalGeneratorDependencies.Any())
                return pairs;

            foreach (var entry in _buildOptions.AdditionalGeneratorDependencies)
            {
                var parts = entry.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new InvalidOperationException($"Invalid entry '{entry}' in {OptionsAttribute.GetConfigurationPath<BuildOptions>()}:{nameof(BuildOptions.AdditionalGeneratorDependencies)} configuration key."
                        + " Expected \"<GeneratorTypeFullName>:<GeneratorDependencyTypeFullName>\" format.");

                pairs.Add((parts[0], parts[1]));
            }

            return pairs;
        }

        private static string GetGeneratorName(IGenerator generator) =>
            generator.GetType().FullName;
    }
}
