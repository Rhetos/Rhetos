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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public static class AlternativeInitialization
    {
        public static IEnumerable<IConceptInfo> InitializeNonparsablePropertiesRecursive(IAlternativeInitializationConcept alternativeInitializationConcept)
        {
            return InitializeNonparsablePropertiesRecursive(alternativeInitializationConcept, new HashSet<string>(), 0);
        }

        private static IEnumerable<IConceptInfo> InitializeNonparsablePropertiesRecursive(IAlternativeInitializationConcept alternativeInitializationConcept, HashSet<string> alreadyCreated, int depth)
        {
            if (depth > 10)
                throw new DslSyntaxException("Macro concept references cannot be resolved.");

            IEnumerable<IConceptInfo> createdConcepts;
            List<IConceptInfo> result = new List<IConceptInfo>();
            alternativeInitializationConcept.InitializeNonparsableProperties(out createdConcepts);
            if (createdConcepts != null && createdConcepts.Count() > 0)
            {
                result.AddRange(createdConcepts);
                foreach (var concept in createdConcepts.OfType<IAlternativeInitializationConcept>())
                    if (!alreadyCreated.Contains(concept.GetFullDescription()))
                    {
                        alreadyCreated.Add(concept.GetFullDescription());
                        result.AddRange(InitializeNonparsablePropertiesRecursive(concept, alreadyCreated, depth + 1));
                    }
            }
            return result;
        }
    }
}
