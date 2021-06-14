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

using Rhetos.Dom.DefaultConcepts;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Automatically deletes the extension records when a master record is deleted.
    /// </summary>
    /// <remarks>
    /// If referencing polymorphic concept, cascade delete will occur when the _Materialized record is automatically deleted.
    /// Cascade delete is implemented in the application layer, because a database implementation would not execute any business logic that is implemented on the extension entity.
    /// For cascade delete in database see CascadeDeleteInDatabase concept or legacy option CommonConcepts.Legacy.CascadeDeleteInDatabase.
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
        private readonly CommonConceptsOptions _commonConceptsOptions;

        public UniqueReferenceCascadeDeleteMacro(CommonConceptsOptions commonConceptsOptions)
        {
            _commonConceptsOptions = commonConceptsOptions;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(UniqueReferenceCascadeDeleteInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            if (conceptInfo.UniqueReference.Base is PolymorphicInfo && conceptInfo.UniqueReference.Extension is IWritableOrmDataStructure)
                newConcepts.Add(new UniqueReferenceCascadeDeletePolymorphicInfo
                {
                    Extension = conceptInfo.UniqueReference.Extension,
                    Base = ((PolymorphicInfo)conceptInfo.UniqueReference.Base).GetMaterializedEntity()
                });

            // Cascade delete FK in database is usually not needed because the server application will explicitly delete the referencing data (to ensure server-side validations and recomputations).
            // Cascade delete in database is just a legacy feature, a convenience for development and testing.
            // It is turned off by default because if a record is deleted by cascade delete directly in the database, then the business logic implemented in application layer will not be executed.
            if (_commonConceptsOptions.CascadeDeleteInDatabase)
                newConcepts.Add(new UniqueReferenceCascadeDeleteDbInfo
                {
                    UniqueReference = conceptInfo.UniqueReference
                });

            return newConcepts;
        }
    }
}
