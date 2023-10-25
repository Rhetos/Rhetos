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
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Compiler;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Manually creates a materialized table, when there are no references or extensions to the polymorphic
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Materialized")]
    public class PolymorphicMaterializedInfo : IConceptInfo, IMacroConcept
    {
        [ConceptKey]
        public PolymorphicInfo Polymorphic { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            var newConcepts = new List<IConceptInfo>();

            var persisted = Polymorphic.GetMaterializedEntity();
            newConcepts.Add(persisted);

            var computedFrom = new EntityComputedFromInfo { Target = persisted, Source = Polymorphic };
            newConcepts.Add(computedFrom);

            newConcepts.Add(new KeepSynchronizedInfo
            {
                EntityComputedFrom = computedFrom,
                FilterSaveExpression = ""
            });

            // Optimized filter by subtype allows efficient queries on the polymorphic's view,
            // but it does not need to use the subtype name (and persist it) when querying the materialized data.
            newConcepts.Add(new FilterByInfo
            {
                Source = persisted,
                Parameter = "Rhetos.Dom.DefaultConcepts.FilterSubtype",
                Expression = @"(repository, parameter) => Filter(parameter.Ids)"
            });

            return newConcepts;
        }
    }
}
