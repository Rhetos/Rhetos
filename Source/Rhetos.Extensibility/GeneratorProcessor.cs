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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhetos.Utilities;

namespace Rhetos.Extensibility
{
    public class GeneratorProcessor
    {
        private IEnumerable<IGenerator> _generators;

        public GeneratorProcessor(IEnumerable<IGenerator> generators)
        {
            this._generators = generators;
        }

        public string ProcessGenerators()
        {
            var genNames = _generators.Select(gen => gen.GetType().FullName).ToList();
            var genDependencies = _generators.SelectMany(gen => gen.Dependencies.Select(x => Tuple.Create(x, gen.GetType().FullName)));
            Rhetos.Utilities.DirectedGraph.TopologicalSort(genNames, genDependencies);

            var sortedGenerators = _generators.ToArray();
            DirectedGraph.SortByGivenOrder(sortedGenerators, genNames.ToArray(), gen => gen.GetType().FullName);

            foreach (var generator in sortedGenerators)
            {
                try
                {
                    generator.Generate();
                }
                catch (Exception ex) {
                    throw new FrameworkException("Error in processing generator " + generator.GetType().Name + ".", ex);
                }
            }

            if (sortedGenerators.Length > 0)
                return "Generated " + string.Join(", ", sortedGenerators.Select(gen => gen.GetType().Name)) + ".";
            return "No generators.";
        }
    }
}
