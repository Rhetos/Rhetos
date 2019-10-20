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
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Automatically deletes detail records when a master record is deleted.
    /// </summary>
    /// <remarks>
    /// This feature does not create "on delete cascade" in database unless CommonConcepts.Legacy.CascadeDeleteInDatabase
    /// is enabled (since Rhetos v2.11).
    /// It is implemented in the application layer, because a database implementation would not execute
    /// any business logic that is implemented on the child entity.
    /// </remarks> 
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("CascadeDelete")]
    public class ReferenceCascadeDeleteInfo : IConceptInfo
    {
        [ConceptKey]
        public ReferencePropertyInfo Reference { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class ReferenceCascadeDeleteMacro : IConceptMacro<ReferenceCascadeDeleteInfo>
    {
        private readonly RhetosAppOptions _rhetosAppOptions;

        public ReferenceCascadeDeleteMacro(RhetosAppOptions rhetosAppOptions)
        {
            _rhetosAppOptions = rhetosAppOptions;
        }
        public IEnumerable<IConceptInfo> CreateNewConcepts(ReferenceCascadeDeleteInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            if (conceptInfo.Reference.Referenced is PolymorphicInfo && conceptInfo.Reference.DataStructure is IWritableOrmDataStructure)
                newConcepts.Add(new ReferenceCascadeDeletePolymorphicInfo
                {
                    Reference = conceptInfo.Reference,
                    Parent = ((PolymorphicInfo)conceptInfo.Reference.Referenced).GetMaterializedEntity()
                });

            // Cascade delete FK in database is not needed because the server application will explicitly delete the referencing data (to ensure server-side validations and recomputations).
            // Cascade delete in database is just a legacy feature, a convenience for development and testing.
            // It is turned off by default because if a record is deleted by cascade delete directly in the database, then the business logic implemented in application layer will not be executed.
            if (_rhetosAppOptions.CommonConcepts__Legacy__CascadeDeleteInDatabase)
            {
                var dbConstraint = (ReferencePropertyDbConstraintInfo)existingConcepts.FindByKey($"ReferencePropertyDbConstraintInfo {conceptInfo.Reference}");
                if (dbConstraint != null)
                    newConcepts.Add(new ReferenceCascadeDeleteDbInfo
                    {
                        ReferenceDbConstraint = dbConstraint
                    });
            }

            return newConcepts;
        }
    }
}
