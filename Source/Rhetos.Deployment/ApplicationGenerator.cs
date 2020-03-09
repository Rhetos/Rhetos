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
using System.Linq;

namespace Rhetos.Deployment
{
    public class ApplicationGenerator
    {
        private readonly ILogger _logger;
        private readonly IDslModel _dslModel;
        private readonly IPluginsContainer<IGenerator> _generatorsContainer;
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly BuildOptions _buildOptions;
        private readonly FilesUtility _filesUtility;
        private readonly ISourceWriter _sourceWriter;

        public ApplicationGenerator(
            ILogProvider logProvider,
            IDslModel dslModel,
            IPluginsContainer<IGenerator> generatorsContainer,
            RhetosAppEnvironment rhetosAppEnvironment,
            BuildOptions buildOptions,
            FilesUtility filesUtility,
            ISourceWriter sourceWriter)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _dslModel = dslModel;
            _generatorsContainer = generatorsContainer;
            _rhetosAppEnvironment = rhetosAppEnvironment;
            _buildOptions = buildOptions;
            _filesUtility = filesUtility;
            _sourceWriter = sourceWriter;
        }

        public void ExecuteGenerators()
        {
            _filesUtility.EmptyDirectory(_rhetosAppEnvironment.AssetsFolder);
            _filesUtility.SafeCreateDirectory(_buildOptions.CacheFolder); // Cache is not deleted between builds.
            if(!string.IsNullOrEmpty(_buildOptions.GeneratedSourceFolder))
                _filesUtility.SafeCreateDirectory(_buildOptions.GeneratedSourceFolder); // Obsolete source files will be cleaned later. Keeping the existing files to allowing source change detection in Visual Studio.

            CheckDslModelErrors();

            var generators = GetSortedGenerators();
            foreach (var generator in generators)
            {
                _logger.Info("Executing " + generator.GetType().Name + ".");
                generator.Generate();
            }
            if (!generators.Any())
                _logger.Info("No additional generators.");

            if(!string.IsNullOrEmpty(_buildOptions.GeneratedSourceFolder))
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

        private IList<IGenerator> GetSortedGenerators()
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var generators = _generatorsContainer.GetPlugins().ToArray();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var generatorNames = generators.Select(GetGeneratorName).ToList();

            MoveToFront(generatorNames, new[] { "Rhetos.Dom.DomGenerator", "Rhetos.Deployment.ResourcesGenerator" } );

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
