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

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Copies all properties from another data structure, along with the associated Required, SqlIndex, Extends and CascadeDelete concepts.
    /// </summary>
    /// <remarks>
    /// It will not copy the Extends concept (UniqueReference) if the source is an extension of the destination.
    /// </remarks>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AllPropertiesWithCascadeDeleteFrom")]
    public class AllPropertiesWithCascadeDeleteFromInfo : AllPropertiesFromInfo
    {
    }

    [Export(typeof(IConceptMacro))]
    [ConceptKeyword("AllPropertiesWithCascadeDeleteFrom")]
    public class AllPropertiesWithCascadeDeleteFromMacro : IConceptMacro<AllPropertiesWithCascadeDeleteFromInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(AllPropertiesWithCascadeDeleteFromInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();
            
            newConcepts.AddRange(existingConcepts.FindByType<ReferenceCascadeDeleteInfo>()
                .Where(ci => ci.Reference.DataStructure == conceptInfo.Source)
                .Select(ci => new ReferenceCascadeDeleteInfo
                {
                    Reference = new ReferencePropertyInfo
                    {
                        DataStructure = conceptInfo.Destination,
                        Name = ci.Reference.Name
                    }
                }));

            var sourceUniqueReference = existingConcepts.FindByReference<UniqueReferenceInfo>(ex => ex.Extension, conceptInfo.Source).SingleOrDefault();
            if (sourceUniqueReference != null)
            {
                var destinationUniqueReference = existingConcepts.FindByReference<UniqueReferenceInfo>(ex => ex.Extension, conceptInfo.Destination).SingleOrDefault();
                if (destinationUniqueReference != null && sourceUniqueReference.Base == destinationUniqueReference.Base)
                {
                    // AllPropertiesFromInfo does not always copy the UniqueReference. CascadeDelete is copied here only if the UniqueReference have been copied (same Base reference).
                    var sourceCascadeDelete = existingConcepts.FindByReference<UniqueReferenceCascadeDeleteInfo>(cd => cd.UniqueReference, sourceUniqueReference).SingleOrDefault();
                    if (sourceCascadeDelete != null)
                    {
                        newConcepts.Add(new UniqueReferenceCascadeDeleteInfo
                        {
                            UniqueReference = destinationUniqueReference
                        });
                    }
                }
            }

            return newConcepts;
        }
    }
}
