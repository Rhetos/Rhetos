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

using Rhetos.Dom;
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
        private readonly ILogger _deployPackagesLogger;
        private readonly IDslModel _dslModel;
        private readonly IDomainObjectModel _domGenerator;
        private readonly IPluginsContainer<IGenerator> _generatorsContainer;

        public ApplicationGenerator(
            ILogProvider logProvider,
            IDslModel dslModel,
            IDomainObjectModel domGenerator,
            IPluginsContainer<IGenerator> generatorsContainer)
        {
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _dslModel = dslModel;
            _domGenerator = domGenerator;
            _generatorsContainer = generatorsContainer;
        }

        public void ExecuteGenerators()
        {
            CheckDslModelErrors();

            _deployPackagesLogger.Trace("Compiling DOM assembly.");
            int generatedTypesCount = _domGenerator.GetTypes().Count();
            if (generatedTypesCount == 0)
            {
                _deployPackagesLogger.Info("Warning: Empty assembly is generated.");
            }
            else
                _deployPackagesLogger.Trace("Generated " + generatedTypesCount + " types.");

            var generators = GetSortedGenerators();
            foreach (var generator in generators)
            {
                _deployPackagesLogger.Trace("Executing " + generator.GetType().Name + ".");
                generator.Generate();
            }
            if (!generators.Any())
                _deployPackagesLogger.Trace("No additional generators.");
        }

        /// <summary>
        /// Creating the DSL model instance *before* executing code generators, to proved better error reporting
        /// and make it clear that a code generator did not cause a parser error.
        /// </summary>
        private void CheckDslModelErrors()
        {
            _deployPackagesLogger.Trace("Parsing DSL scripts.");
            int dslModelConceptsCount = _dslModel.Concepts.Count();
            _deployPackagesLogger.Trace("Application model has " + dslModelConceptsCount + " statements.");
        }

        private IList<IGenerator> GetSortedGenerators()
        {
            // The plugins in the container are sorted by their dependencies defined in ExportMetadata attribute (static typed):
            var generators = _generatorsContainer.GetPlugins().ToArray();

            // Additional sorting by loosely-typed dependencies from the Dependencies property:
            var generatorNames = generators.Select(GetGeneratorName).ToList();
            var dependencies = generators.Where(gen => gen.Dependencies != null)
                .SelectMany(gen => gen.Dependencies.Select(dependsOn => Tuple.Create(dependsOn, GetGeneratorName(gen))))
                .ToList();
            Graph.TopologicalSort(generatorNames, dependencies);

            foreach (var missingDependency in dependencies.Where(dep => !generatorNames.Contains(dep.Item1)))
                _deployPackagesLogger.Info($"Missing dependency '{missingDependency.Item1}' for application generator '{missingDependency.Item2}'.");

            Graph.SortByGivenOrder(generators, generatorNames, GetGeneratorName);
            return generators;
        }

        private static string GetGeneratorName(IGenerator gen)
        {
            return gen.GetType().FullName;
        }
    }
}
