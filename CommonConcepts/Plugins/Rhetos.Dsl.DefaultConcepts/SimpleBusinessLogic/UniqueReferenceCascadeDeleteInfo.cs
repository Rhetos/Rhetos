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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Automatically deletes the extension records when a master record is deleted.
    /// </summary>
    /// <remarks>
    /// This feature does not create "on delete cascade" in database
    /// (since Rhetos v2.11, unless CommonConcepts.Legacy.CascadeDeleteInDatabase is enabled).
    /// It is implemented in the application layer, because a database implementation would not execute
    /// any business logic that is implemented on the extension entity.
    /// </remarks> 
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("CascadeDelete")]
    public class UniqueReferenceCascadeDeleteInfo : IConceptInfo
    {
        [ConceptKey]
        public UniqueReferenceInfo UniqueReference { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class UniqueReferenceCascadeDeleteMacro : IConceptMacro<UniqueReferenceCascadeDeleteInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(UniqueReferenceCascadeDeleteInfo conceptInfo, IDslModel existingConcepts)
        {
            if (conceptInfo.UniqueReference.Base is PolymorphicInfo && conceptInfo.UniqueReference.Extension is IWritableOrmDataStructure)
                return new[]
                {
                    new UniqueReferenceCascadeDeletePolymorphicInfo
                    {
                        Extension = conceptInfo.UniqueReference.Extension,
                        Base = ((PolymorphicInfo)conceptInfo.UniqueReference.Base).GetMaterializedEntity()
                    }
                };
            else
                return null;
        }
    }
}
