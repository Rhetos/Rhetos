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
using System.Reflection;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Copies all properties from source data structure to the cache entity, along with the associated Required, SqlIndex, Extends and CascadeDelete concepts.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AllProperties")]
    public class PersistedAllPropertiesInfo : IMacroConcept
    {
        [ConceptKey]
#pragma warning disable CS0618 // Type or member is obsolete. Available for backward compatibility. See PersistedDataStructureInfo.
        public PersistedDataStructureInfo Persisted { get; set; }
#pragma warning restore CS0618 // Type or member is obsolete

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            return new[] { new EntityComputedFromAllPropertiesInfo { EntityComputedFrom = new EntityComputedFromInfo { Target = Persisted, Source = Persisted.Source } } };
        }
    }
}
