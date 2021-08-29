﻿/*
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
    /// <summary>
    /// Automatically updates cache when the source data is changed. This feature requires ChangesOnChangedItems defined on the source.
    /// The "save filter" is a lambda expression (IEnumerable&lt;Entity&gt; items, repository) => IEnumerable&lt;Entity&gt;, 
    /// that returns subset of items which are allowed to be updated by the KeepSynchronized mechanism.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("KeepSynchronized")]
    public class KeepSynchronizedInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityComputedFromInfo EntityComputedFrom { get; set; }

        /// <summary>May be empty.</summary>
        public string FilterSaveExpression { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class KeepSynchronizedMacro : IConceptMacro<KeepSynchronizedInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(KeepSynchronizedInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var changesOnChangesItems = existingConcepts.FindByReference<ChangesOnChangedItemsInfo>(change => change.Computation, conceptInfo.EntityComputedFrom.Source)
                .ToArray();

            newConcepts.AddRange(changesOnChangesItems.Select(change =>
                new KeepSynchronizedOnChangedItemsInfo { KeepSynchronized = conceptInfo, UpdateOnChange = change }));

            // If the computed data source is an extension, but its value does not depend on changes in its base data structure,
            // it should still be computed every time the base data structure data is inserted.

            var dataSourceExtension = existingConcepts.FindByReference<UniqueReferenceInfo>(ex => ex.Extension, conceptInfo.EntityComputedFrom.Source)
                .SingleOrDefault();
            
            if (dataSourceExtension != null
                && dataSourceExtension.Base is IWritableOrmDataStructure
                && dataSourceExtension.Base != conceptInfo.EntityComputedFrom.Target
                && !changesOnChangesItems.Any(c => c.DependsOn == dataSourceExtension.Base)
                && !existingConcepts.FindByReference<ChangesOnBaseItemInfo>(c => c.Computation, conceptInfo.EntityComputedFrom.Source).Any())
                newConcepts.Add(new ComputeForNewBaseItemsInfo { EntityComputedFrom = conceptInfo.EntityComputedFrom, FilterSaveExpression = conceptInfo.FilterSaveExpression });

            return newConcepts;
        }
    }
}
