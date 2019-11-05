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

using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("PessimisticLockingParent")]
    public class PessimisticLockingParentInfo : IValidatedConcept
    {
        [ConceptKey]
        public ReferencePropertyInfo Reference { get; set; }
        
        public PessimisticLockingInfo Detail { get; set; } // This redundant property is to ensure dependencies: 1. the detail entity must have pessimistic locking, and 2. this concept's code generator is executed after that one.

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (Reference.DataStructure != Detail.Resource)
                throw new DslSyntaxException("Invalid PessimisticLockingParent: reference detail '" + Reference.DataStructure.GetShortDescription()
                    + "' does not match resource '" + Detail.Resource.GetShortDescription() + "'.");
        }
    }
}
