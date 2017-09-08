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
    [ConceptKeyword("InheritFromBase")]
    public class RowPermissionsInheritFromBaseInfo : IValidatedConcept
    {
        [ConceptKey]
        public RowPermissionsPluginableFiltersInfo RowPermissionsFilters { get; set; }

        public DataStructureInfo GetBaseDataStructure(IDslModel existingConcepts)
        {
            return existingConcepts
                .FindByReference<UniqueReferenceInfo>(extends => extends.Extension, RowPermissionsFilters.DataStructure)
                .Select(extends => extends.Base)
                .SingleOrDefault();
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            var baseDataStructure = GetBaseDataStructure(existingConcepts);
            if (baseDataStructure == null)
                throw new DslSyntaxException(this, "'" + this.GetKeywordOrTypeName() + "' can only be used on an extension. '"
                    + RowPermissionsFilters.DataStructure.GetUserDescription() + "' does not extend another data structure.");

        }
    }

    [Export(typeof(IConceptMacro))]
    public class RowPermissionsInheritFromBaseMacro : IConceptMacro<RowPermissionsInheritFromBaseInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(RowPermissionsInheritFromBaseInfo conceptInfo, IDslModel existingConcepts)
        {
            var baseDataStructure = conceptInfo.GetBaseDataStructure(existingConcepts);
            if (baseDataStructure == null)
                return null; // Might be created in a later iteration.

            return new IConceptInfo[]
            {
                new RowPermissionsInheritFromInfo
                {
                    RowPermissionsFilters = conceptInfo.RowPermissionsFilters,
                    Source = baseDataStructure,
                    SourceSelector = "Base"
                }
            };
        }
    }
}
