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
    public class RowPermissionsInheritFromBaseInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public RowPermissionsPluginableFiltersInfo RowPermissionsFilters { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var baseDataStructure = GetBaseDataStructure(existingConcepts);
            if (baseDataStructure == null)
                return null; // Migth be created in a later iteration.

            return new IConceptInfo[]
            {
                new RowPermissionsInheritFromInfo
                {
                    RowPermissionsFilters = RowPermissionsFilters,
                    Source = baseDataStructure,
                    SourceSelector = "Base"
                }
            };
        }

        private DataStructureInfo GetBaseDataStructure(IEnumerable<IConceptInfo> existingConcepts)
        {
            return existingConcepts.OfType<DataStructureExtendsInfo>()
                .Where(extends => extends.Extension == RowPermissionsFilters.DataStructure)
                .Select(extends => extends.Base)
                .SingleOrDefault();
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> existingConcepts)
        {
            var baseDataStructure = GetBaseDataStructure(existingConcepts);
            if (baseDataStructure == null)
                throw new DslSyntaxException(this, "'" + this.GetKeywordOrTypeName() + "' can only be used on an extension. '"
                    + RowPermissionsFilters.DataStructure.GetUserDescription() + "' does not extend another data structure.");
        }
    }
}
