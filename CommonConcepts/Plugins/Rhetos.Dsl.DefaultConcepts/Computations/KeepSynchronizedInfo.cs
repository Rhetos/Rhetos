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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("KeepSynchronized")]
    public class KeepSynchronizedInfo : IMacroConcept
    {
        [ConceptKey]
        public EntityComputedFromInfo EntityComputedFrom { get; set; }

        /// <summary>May be empty.</summary>
        public string FilterSaveExpression { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var changesOnChangesItems = existingConcepts.OfType<ChangesOnChangedItemsInfo>()
                .Where(change => change.Computation == EntityComputedFrom.Source)
                .ToArray();

            newConcepts.AddRange(changesOnChangesItems.Select(change =>
                new KeepSynchronizedOnChangedItemsInfo { EntityComputedFrom = EntityComputedFrom, UpdateOnChange = change, FilterSaveExpression = FilterSaveExpression }));

            // If the computed data source is an extension, but its value does not depend on changes in its base data structure,
            // it should still be computed every time the base data structure data is inserted.

            var dataSourceExtension = existingConcepts.OfType<DataStructureExtendsInfo>()
                .Where(ex => ex.Extension == EntityComputedFrom.Source)
                .SingleOrDefault();
            
            if (dataSourceExtension != null
                && dataSourceExtension.Base is IWritableOrmDataStructure
                && !changesOnChangesItems.Any(c => c.DependsOn == dataSourceExtension.Base)
                && !existingConcepts.OfType<ChangesOnBaseItemInfo>().Where(c => c.Computation == EntityComputedFrom.Source).Any())
                newConcepts.Add(new ComputeForNewBaseItemsInfo { EntityComputedFrom = EntityComputedFrom, FilterSaveExpression = FilterSaveExpression });

            return newConcepts;
        }
    }
}
