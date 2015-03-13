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
    [ConceptKeyword("DefaultLoadFilter")]
    public class EntityComputedFromDefaultLoadFilterInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityComputedFromInfo EntityComputedFrom { get; set; }

        /// <summary>A filter or a loader parameter that returns a subset of records that are computed.
        /// Both Source and Target data structures must have a filter or a loader with the given parameter type.</summary>
        public string LoadFilter { get; set; }
    }
}
