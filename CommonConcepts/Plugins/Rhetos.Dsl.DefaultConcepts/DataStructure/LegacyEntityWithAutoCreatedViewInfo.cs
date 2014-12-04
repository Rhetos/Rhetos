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
using Rhetos.Utilities;
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("LegacyEntity")]
    public class LegacyEntityWithAutoCreatedViewInfo : DataStructureInfo, IWritableOrmDataStructure
    {
        public string Table { get; set; }

        string IOrmDataStructure.GetOrmSchema()
        {
            return Module.Name;
        }

        string IOrmDataStructure.GetOrmDatabaseObject()
        {
            return Name;
        }
    }

    [Export(typeof(IConceptMacro))]
    public class LegacyEntityWithAutoCreatedViewMacro : IConceptMacro<LegacyEntityWithAutoCreatedViewInfo>
    {    /// <summary>
        /// For each property that does not have it's LegacyProperty defined, this function creates a default LegacyProperty
        /// assuming that corresponding legacy tables's column has the same name as the property.
        /// </summary>
        public IEnumerable<IConceptInfo> CreateNewConcepts(LegacyEntityWithAutoCreatedViewInfo conceptInfo, IDslModel existingConcepts)
        {
            var properties = existingConcepts.FindByType<PropertyInfo>().Where(prop => prop.DataStructure == conceptInfo).ToArray();
            var propertiesWithLegacyProperty = existingConcepts.FindByType<LegacyPropertyInfo>().Where(lp => lp.Property.DataStructure == conceptInfo).Select(lp => lp.Property).ToArray();

            var legacyIndex = new HashSet<PropertyInfo>(propertiesWithLegacyProperty);
            var propertiesWithoutLegacyPropertyInfo = properties.Where(p => !legacyIndex.Contains(p)).ToArray();

            var errorReference = propertiesWithoutLegacyPropertyInfo.OfType<ReferencePropertyInfo>().FirstOrDefault();
            if (errorReference != null)
                throw new DslSyntaxException("Legacy reference property '" + errorReference.GetKeyProperties() +"' must have explicitly defined LegacyProperty with source columns (comma separated), referenced table and referenced columns.");

            return propertiesWithoutLegacyPropertyInfo.Select(p => new LegacyPropertySimpleInfo { Property = p, Column = p.Name }).ToList();
        }
    }
}
