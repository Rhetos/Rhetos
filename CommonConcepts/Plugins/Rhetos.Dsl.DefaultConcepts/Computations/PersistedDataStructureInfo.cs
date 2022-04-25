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
    /// Creates entity which will contain cached data from the given source. 
    /// The source can be any readable data structure (Browse, SqlQueryable, Computed and similar).
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Persisted")]
    [Obsolete("Use Entity with ComputedFrom instead. The Persisted concept is available for backward compatibility, but it may cause performance issues on deployment by sometimes unnecessarily refreshing the persisted table.")]
    public class PersistedDataStructureInfo : EntityInfo, IMacroConcept
    {
        public DataStructureInfo Source { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            return new[] { CreateEntityComputedFrom() };
        }

        public EntityComputedFromInfo CreateEntityComputedFrom()
        {
            return new EntityComputedFromInfo { Target = this, Source = Source };
        }
    }
}
