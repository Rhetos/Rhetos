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

using Rhetos.Logging;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl
{
    public static class AlternativeInitialization
    {
        public static List<IConceptInfo> InitializeNonparsableProperties(IEnumerable<IConceptInfo> concepts, ILogger traceLogger)
        {
            var newConcepts = new List<IConceptInfo>();

            foreach (var alternativeInitializationConcept in concepts.OfType<IAlternativeInitializationConcept>())
                newConcepts.AddRange(InitializeNonparsablePropertiesRecursive(alternativeInitializationConcept, [], 0, traceLogger));

            return newConcepts;
        }

        private static List<IConceptInfo> InitializeNonparsablePropertiesRecursive(IAlternativeInitializationConcept alternativeInitializationConcept, HashSet<string> alreadyCreated, int depth, ILogger traceLogger)
        {
            if (depth > 10)
                throw new DslConceptSyntaxException(alternativeInitializationConcept, "Macro concept references cannot be resolved.");

            List<IConceptInfo> result = [];

            alternativeInitializationConcept.InitializeNonparsableProperties(out var createdConceptsEnum);
            var createdConcepts = CsUtility.Materialized(createdConceptsEnum);

            if (createdConcepts != null && createdConcepts.Count != 0)
            {
                traceLogger.Trace(() => alternativeInitializationConcept.GetShortDescription() + " generated on alternative initialization: "
                    + string.Join(", ", createdConcepts.Select(c => c.GetShortDescription())) + ".");

                result.AddRange(createdConcepts);
                foreach (var concept in createdConcepts.OfType<IAlternativeInitializationConcept>())
                    if (!alreadyCreated.Contains(concept.GetFullDescription()))
                    {
                        alreadyCreated.Add(concept.GetFullDescription());
                        result.AddRange(InitializeNonparsablePropertiesRecursive(concept, alreadyCreated, depth + 1, traceLogger));
                    }
            }

            return result;
        }
    }
}
