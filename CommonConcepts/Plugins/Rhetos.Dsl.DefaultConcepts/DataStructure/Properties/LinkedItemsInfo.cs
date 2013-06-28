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
            if (!(DataStructure is EntityInfo))
                throw new DslSyntaxException(
                    string.Format("LinkedItems (Parent-child) must be used inside Entity. DateStructure {0} is of type {1}, expected type is {2}.",
                        DataStructure.ToString(),
                        DataStructure.GetType().FullName,
                        typeof(EntityInfo).FullName));
            if(ReferenceProperty.Referenced != DataStructure)
                throw new DslSyntaxException(
                    string.Format("LinkedItems (Parent-child) references {0} which is reference to {1}. Expected reference is {2}.",
                        ReferenceProperty.ToString(),
                        ReferenceProperty.Referenced.ToString(),
                        DataStructure.ToString()));
        }
    }
}
