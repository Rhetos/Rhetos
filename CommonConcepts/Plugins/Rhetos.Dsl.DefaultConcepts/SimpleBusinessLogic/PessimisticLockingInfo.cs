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
    [ConceptKeyword("PessimisticLocking")]
    public class PessimisticLockingInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Resource { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            Dictionary<string, PessimisticLockingInfo> PessimisticLockingByDataStructureIndex =
                existingConcepts.OfType<PessimisticLockingInfo>()
                    .ToDictionary(locking => locking.Resource.GetKey());

            var myParentsWithPessimisticLocking = existingConcepts.OfType<ReferenceDetailInfo>()
                .Where(detailReference => detailReference.Reference.DataStructure == Resource)
                .Where(detailReference => PessimisticLockingByDataStructureIndex.ContainsKey(detailReference.Reference.Referenced.GetKey()))
                .Select(detailReference => detailReference.Reference).ToArray();

            return myParentsWithPessimisticLocking.Select(
                reference => new PessimisticLockingParentInfo
                    {
                        Detail = PessimisticLockingByDataStructureIndex[Resource.GetKey()],
                        Reference = reference
                    }).ToList();
        }

        public override string ToString()
        {
            return "PessimisticLocking: " + Resource;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
