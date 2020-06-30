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
using System.Threading.Tasks;

namespace Rhetos.Deployment
{
    public class ApplicationGenerator
    {
        private readonly ILogger _logger;
        private readonly IDslModel _dslModel;
        private readonly IPluginsContainer<IGenerator> _generatorsContainer;
        private readonly RhetosBuildEnvironment _buildEnvironment;
        private readonly FilesUtility _filesUtility;
        private readonly ISourceWriter _sourceWriter;

        public ApplicationGenerator(
            ILogProvider logProvider,
            IDslModel dslModel,
            IPluginsContainer<IGenerator> generatorsContainer,
            RhetosBuildEnvironment buildEnvironment,
            FilesUtility filesUtility,
            ISourceWriter sourceWriter)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _dslModel = dslModel;
            _generatorsContainer = generatorsContainer;
            _buildEnvironment = buildEnvironment;
            _filesUtility = filesUtility;
            _sourceWriter = sourceWriter;
        }

        public void ExecuteGenerators()
        {
            var sw = Stopwatch.StartNew();
            _filesUtility.EmptyDirectory(_buildEnvironment.GeneratedAssetsFolder);
            _filesUtility.SafeCreateDirectory(_buildEnvironment.CacheFolder); // Cache is not deleted between builds.
            if(!string.IsNullOrEmpty(_buildEnvironment.GeneratedSourceFolder))
                _filesUtility.SafeCreateDirectory(_buildEnvironment.GeneratedSourceFolder); // Obsolete source files will be cleaned later. Keeping the existing files to allowing source change detection in Visual Studio.

            CheckDslModelErrors();

            var generatorBatches = GetGeneratorBatches();
            foreach (var generatorBatch in generatorBatches)
            {
                _logger.Info($"Executing generator batch (parallel={generatorBatch.parallel}): {string.Join(",", generatorBatch.batch.Select(GetGeneratorName))}.");

                void ExecuteGenerator(IGenerator generator)
                {
                    _logger.Info("Executing " + generator.GetType().Name + ".");
                    generator.Generate();
                }

                if (generatorBatch.parallel)
                {
                    Parallel.ForEach(generatorBatch.batch, ExecuteGenerator);
                }
                else
                {
                    foreach (var generator in generatorBatch.batch)
                        ExecuteGenerator(generator);
                }
            }

            var totalGeneratorCount = generatorBatches.Sum(a => a.batch.Count);
            if (totalGeneratorCount == 0)
                _logger.Info("No application generators found.");
            else
                _logger.Info($"Executed {totalGeneratorCount} application generators in {sw.ElapsedMilliseconds:#,0} ms.");

            if(!string.IsNullOrEmpty(_buildEnvironment.GeneratedSourceFolder))
                _sourceWriter.CleanUp();
        }

        /// <summary>
        /// Creating the DSL model instance *before* executing code generators, to proved better error reporting
        /// and make it clear that a code generator did not cause a parser error.
        /// </summary>
        private void CheckDslModelErrors()
        {
            _logger.Info("Parsing DSL scripts.");
            int dslModelConceptsCount = _dslModel.Concepts.Count();
            _logger.Info("Application model has " + dslModelConceptsCount + " statements.");
        }

        private static readonly string[] _priorityGenerators = new[] { "Rhetos.Dom.DomGenerator", "Rhetos.Deployment.ResourcesGenerator" };
        
        private IList<(IList<IGenerator> batch, bool parallel)> GetGeneratorBatches()
        {
            var remainingGenerators = GetSortedGenerators();
            var priorityBatch = remainingGenerators.Where(a => _priorityGenerators.Contains(GetGeneratorName(a))).ToList();
            remainingGenerators = remainingGenerators.Except(priorityBatch).ToList();
            var noDependencyBatch = remainingGenerators.Where(a => a.Dependencies == null || !a.Dependencies.Any()).ToList();
            remainingGenerators = remainingGenerators.Except(noDependencyBatch).ToList();

            return new List<(IList<IGenerator> batch, bool parallel)>()
            {
                (priorityBatch, true),
                (noDependencyBatch, true),
                (remainingGenerators, false)
            };
        }

        private IList<IGenerator> GetSortedGenerators()
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var generators = _generatorsContainer.GetPlugins().ToArray();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var generatorNames = generators.Select(GetGeneratorName).ToList();

            MoveToFront(generatorNames, _priorityGenerators);

            var dependencies = generators.Where(gen => gen.Dependencies != null)
                .SelectMany(gen => gen.Dependencies.Select(dependsOn => Tuple.Create(dependsOn, GetGeneratorName(gen))))
                .ToList();
            Graph.TopologicalSort(generatorNames, dependencies);

            foreach (var missingDependency in dependencies.Where(dep => !generatorNames.Contains(dep.Item1)))
                _logger.Warning($"Missing dependency '{missingDependency.Item1}' for application generator '{missingDependency.Item2}'.");

            Graph.SortByGivenOrder(generators, generatorNames, GetGeneratorName);

            return generators;
        }

        /// <summary>
        /// For backward compatibility, some generators are manually placed at the beginning,
        /// because before Rhetos v4.0 those generators where executed explicitly before IGenerator plugins,
        /// and other plugins did not need to specify dependency to them.
        /// </summary>
        private static void MoveToFront(List<string> generatorNames, string[] priorityGenerators)
        {
            foreach (string generator in priorityGenerators)
            {
                var indexofDomgenerator = generatorNames.IndexOf(generator);
                if (indexofDomgenerator == -1)
                    throw new FrameworkException($@"Could not find Generator of type {generator}");
                generatorNames.RemoveAt(indexofDomgenerator);
                generatorNames.Insert(0, generator);
            }
        }

        private static string GetGeneratorName(IGenerator gen)
        {
            return gen.GetType().FullName;
        }
    }
}
