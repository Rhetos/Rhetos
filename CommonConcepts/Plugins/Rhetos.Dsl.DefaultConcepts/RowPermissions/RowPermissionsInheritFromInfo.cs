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
    [ConceptKeyword("InheritFrom")]
    public class RowPermissionsInheritFromInfo : IValidationConcept, IMacroConcept
    {
        [ConceptKey]
        public RowPermissionsPluginableFiltersInfo RowPermissionsFilters { get; set; }

        [ConceptKey]
        public ReferencePropertyInfo ReferenceProperty { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            List<IConceptInfo> newConcepts = new List<IConceptInfo>();
            var readInfo = GetReadConceptInfoForReference(ReferenceProperty, existingConcepts);
            if (readInfo != null) newConcepts.Add(new RowPermissionsInheritReadFromInfo() { InheritFromInfo = this });

            var writeInfo = GetWriteConceptInfoForReference(ReferenceProperty, existingConcepts);
            if (writeInfo != null) newConcepts.Add(new RowPermissionsInheritWriteFromInfo() { InheritFromInfo = this });

            return newConcepts;
        }
     
        public void CheckSemantics(IEnumerable<IConceptInfo> existingConcepts)
        {
            if (ReferenceProperty.DataStructure != RowPermissionsFilters.DataStructure)
                throw new DslSyntaxException(this, "Referenced property must belong to the same DataStructure as this concept.");

            var rowPermissionsRead = GetReadConceptInfoForReference(ReferenceProperty, existingConcepts);
            var rowPermissionsWrite = GetWriteConceptInfoForReference(ReferenceProperty, existingConcepts);

            if (rowPermissionsRead == null && rowPermissionsWrite == null)
                throw new DslSyntaxException(this, "Reference '" + ReferenceProperty.Name + "' is not a reference to entity with RowPermissions.");
        }

        private RowPermissionsReadInfo GetReadConceptInfoForReference(ReferencePropertyInfo reference, IEnumerable<IConceptInfo> concepts)
        {
            var allRp = concepts.OfType<RowPermissionsReadInfo>();
            return allRp.SingleOrDefault(a => a.Source == reference.Referenced);
        }
        private RowPermissionsWriteInfo GetWriteConceptInfoForReference(ReferencePropertyInfo reference, IEnumerable<IConceptInfo> concepts)
        {
            var allRp = concepts.OfType<RowPermissionsWriteInfo>();
            return allRp.SingleOrDefault(a => a.Source == reference.Referenced);
        }
    }
}
