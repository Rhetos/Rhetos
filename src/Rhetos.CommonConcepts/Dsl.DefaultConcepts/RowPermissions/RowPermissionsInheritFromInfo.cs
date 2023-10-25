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
    public class RowPermissionsInheritFromInfo : IValidatedConcept
    {
        [ConceptKey]
        public RowPermissionsPluginableFiltersInfo RowPermissionsFilters { get; set; }

        /// <summary>Row permissions are inherited from this data structure.</summary>
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        /// <summary>Object model property name that references the Source data structure class.</summary>
        [ConceptKey]
        public string SourceSelector { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            var rowPermissionsRead = GetRowPermissionsRead(existingConcepts);
            var rowPermissionsWrite = GetRowPermissionsWrite(existingConcepts);

            if (rowPermissionsRead == null && rowPermissionsWrite == null)
                throw new DslSyntaxException(this, "Referenced '" + Source.GetUserDescription() + "' does not have row permissions.");
        }

        public RowPermissionsReadInfo GetRowPermissionsRead(IDslModel existingConcepts)
        {
            return existingConcepts.FindByReference<RowPermissionsReadInfo>(rp => rp.Source, Source)
                .SingleOrDefault();
        }

        public RowPermissionsWriteInfo GetRowPermissionsWrite(IDslModel existingConcepts)
        {
            return existingConcepts.FindByReference<RowPermissionsWriteInfo>(rp => rp.Source, Source)
                .SingleOrDefault();
        }
    }

    [Export(typeof(IConceptMacro))]
    public class RowPermissionsInheritFromMacro : IConceptMacro<RowPermissionsInheritFromInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(RowPermissionsInheritFromInfo conceptInfo, IDslModel existingConcepts)
        {
            List<IConceptInfo> newConcepts = new List<IConceptInfo>();

            var rowPermissionsRead = conceptInfo.GetRowPermissionsRead(existingConcepts);
            var rowPermissionsWrite = conceptInfo.GetRowPermissionsWrite(existingConcepts);

            if (rowPermissionsRead != null)
                newConcepts.Add(new RowPermissionsInheritReadInfo() { InheritFromInfo = conceptInfo });
            if (rowPermissionsWrite != null)
                newConcepts.Add(new RowPermissionsInheritWriteInfo() { InheritFromInfo = conceptInfo });

            return newConcepts;
        }
    }
}
