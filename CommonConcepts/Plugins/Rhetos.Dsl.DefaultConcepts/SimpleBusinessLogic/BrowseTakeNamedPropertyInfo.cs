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
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Take")]
    public class BrowseTakeNamedPropertyInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public BrowseDataStructureInfo Browse { get; set; }

        [ConceptKey]
        public string Name { get; set; }

        public string Path { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return BrowseTakePropertyInfo.CreateNewConcepts(Browse, Path, Name, existingConcepts, this);
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            BrowseTakePropertyInfo.CheckSemantics(Browse, Path, concepts, this);
        }
    }
}
