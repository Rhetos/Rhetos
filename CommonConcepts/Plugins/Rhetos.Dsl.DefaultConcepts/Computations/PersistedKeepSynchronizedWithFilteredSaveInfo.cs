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
    public class PersistedKeepSynchronizedWithFilteredSaveInfo : IMacroConcept
    {
        [ConceptKey]
        public PersistedDataStructureInfo Persisted { get; set; }

        public string FilterSaveExpression { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            return new[] { new KeepSynchronizedInfo
                {
                    EntityComputedFrom = new EntityComputedFromInfo { Target = Persisted, Source = Persisted.Source },
                    FilterSaveExpression = FilterSaveExpression
                } };
        }
    }
}
