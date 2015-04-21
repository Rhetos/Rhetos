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
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("LinkedItems")]
    public class LinkedItemsInfo : PropertyInfo, IConceptInfo, IValidationConcept
    {
        public ReferencePropertyInfo ReferenceProperty { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (!DslUtility.IsQueryable(DataStructure))
                throw new DslSyntaxException(this, this.GetKeywordOrTypeName() + " can only be used on a queryable data structure, such as Entity. " + DataStructure.GetKeywordOrTypeName() + " is not queryable.");

            if (!DslUtility.IsQueryable(ReferenceProperty.DataStructure))
                throw new DslSyntaxException(this, this.GetKeywordOrTypeName() + " must reference a queryable data structure, such as Entity. " + ReferenceProperty.DataStructure.GetKeywordOrTypeName() + " is not queryable.");

            if (ReferenceProperty.Referenced != DataStructure)
                throw new DslSyntaxException(this, string.Format(
                    "{0} references '{1}' which is a reference to '{2}'. Expected is a reference back to '{3}'.",
                    this.GetKeywordOrTypeName(),
                    ReferenceProperty.GetUserDescription(),
                    ReferenceProperty.Referenced.GetUserDescription(),
                    DataStructure.GetUserDescription()));
        }
    }
}
