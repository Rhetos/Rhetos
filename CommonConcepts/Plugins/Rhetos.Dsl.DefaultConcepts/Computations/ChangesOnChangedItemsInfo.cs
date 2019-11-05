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
    [ConceptKeyword("ChangesOnChangedItems")]
    public class ChangesOnChangedItemsInfo : IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo Computation { get; set; }

        [ConceptKey]
        public DataStructureInfo DependsOn { get; set; }

        [ConceptKey]
        public string FilterType { get; set; }

        [ConceptKey]
        public string FilterFormula { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!(DependsOn is IWritableOrmDataStructure))
                throw new DslSyntaxException(this, "DependsOn data structure must be IWritableOrmDataStructure (" + DependsOn.GetUserDescription() + ").");
        }

        private static int _nextKey = 1;

        private int _alternativeKey = 0;

        public int GetAlternativeKey()
        {
            if (_alternativeKey == 0)
                _alternativeKey = _nextKey++;
            return _alternativeKey;
        }
    }
}
