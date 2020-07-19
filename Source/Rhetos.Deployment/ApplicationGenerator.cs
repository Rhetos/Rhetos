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
        private readonly IDslModel _dslModel;
        private readonly IPluginsContainer<IGenerator> _generatorsContainer;
        private readonly RhetosBuildEnvironment _buildEnvironment;
        private readonly FilesUtility _filesUtility;
        private readonly ISourceWriter _sourceWriter;
        private readonly BuildOptions _buildOptions;

        public ApplicationGenerator(
            ILogProvider logProvider,
            IDslModel dslModel,
            IPluginsContainer<IGenerator> generatorsContainer,
            RhetosBuildEnvironment buildEnvironment,
            FilesUtility filesUtility,
            ISourceWriter sourceWriter,
            BuildOptions buildOptions)
        {
            _logProvider = logProvider;
            _logger = logProvider.GetLogger(GetType().Name);
            _dslModel = dslModel;
            _generatorsContainer = generatorsContainer;
            _buildEnvironment = buildEnvironment;
            _filesUtility = filesUtility;
            _sourceWriter = sourceWriter;
            _buildOptions = buildOptions;
        }

        public void ExecuteGenerators()
        {
            var sw = Stopwatch.StartNew();
            _filesUtility.EmptyDirectory(_buildEnvironment.GeneratedAssetsFolder);
            _filesUtility.SafeCreateDirectory(_buildEnvironment.CacheFolder); // Cache is not deleted between builds.
            if(!string.IsNullOrEmpty(_buildEnvironment.GeneratedSourceFolder))
                _filesUtility.SafeCreateDirectory(_buildEnvironment.GeneratedSourceFolder); // Obsolete source files will be cleaned later. Keeping the existing files to allowing source change detection in Visual Studio.

            CheckDslModelErrors();

            var generators = _generatorsContainer.GetPlugins().ToArray();
            var job = PrepareGeneratorsJob(generators);

            _logger.Info(() => $"Starting parallel execution of {generators.Length} generators.");
            if (_buildOptions.MaxExecuteGeneratorsParallelism > 0)
                _logger.Info(() => $"Using max {_buildOptions.MaxExecuteGeneratorsParallelism} degree of parallelism from configuration.");

            job.RunAllTasks(_buildOptions.MaxExecuteGeneratorsParallelism);
            _logger.Info(() => $"Executed {generators.Length} application generators in {sw.ElapsedMilliseconds:#,0} ms.");

            if(!string.IsNullOrEmpty(_buildEnvironment.GeneratedSourceFolder))
                _sourceWriter.CleanUp();
        }

        private ParallelTopologicalJob PrepareGeneratorsJob(IEnumerable<IGenerator> generators)
        {
            var additionalDependencies = ParseAdditionalDependencies();

            var job = new ParallelTopologicalJob(_logProvider);
            foreach (var generator in generators)
            {
                var dependencies = generator.Dependencies ?? Enumerable.Empty<string>();
                var generatorName = GetGeneratorName(generator);
                if (additionalDependencies.TryGetValue(generatorName, out var additionForGenerator))
                    dependencies = dependencies.Concat(additionForGenerator);

                job.AddTask(generatorName, () =>
                {
                    _logger.Info(() => $"Starting generator '{generatorName}'.");
                    generator.Generate();
                }, dependencies);
            }

            return job;
        }

        private Dictionary<string, List<string>> ParseAdditionalDependencies()
        {
            var parsedDependencies = new Dictionary<string, List<string>>();

            if (_buildOptions.AdditionalGeneratorDependencies == null || _buildOptions.AdditionalGeneratorDependencies.Count() == 0)
                return parsedDependencies;

            var pairs = new List<(string name, string dependency)>();
            foreach (var entry in _buildOptions.AdditionalGeneratorDependencies)
            {
                var parts = entry.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new InvalidOperationException($"Invalid entry '{entry}' in {nameof(BuildOptions)}.{nameof(BuildOptions.AdditionalGeneratorDependencies)} configuration key."
                        + " Expected \"<GeneratorName>:<GeneratorDependencyName>\" format.");

                pairs.Add((parts[0], parts[1]));
            }

            _logger.Info(() => $"Parsed {pairs.Count} additional generator dependencies from configuration.");

            return pairs
                .GroupBy(pair => pair.name)
                .ToDictionary(group => group.Key, group => group.Select(pair => pair.dependency).ToList());
        }

        /// <summary>
        /// Creating the DSL model instance *before* executing code generators, to proved better error reporting
        /// and make it clear that a code generator did not cause a parser error.
        /// </summary>
        private void CheckDslModelErrors()
        {
            _logger.Info(() => "Parsing DSL scripts.");
            int dslModelConceptsCount = _dslModel.Concepts.Count();
            _logger.Info(() => $"Application model has {dslModelConceptsCount} statements.");
        }

        private static string GetGeneratorName(IGenerator generator) =>
            generator.GetType().FullName;
    }
}
