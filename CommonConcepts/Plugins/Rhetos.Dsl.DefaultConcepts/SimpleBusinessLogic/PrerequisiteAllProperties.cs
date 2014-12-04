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
    /// <summary>
    /// This concept is used as a placeholder when all properties of an entity are required as a prerequisite for another concept.
    /// Dependent concept can reference this concept as a dependency.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("PrerequisiteAllProperties")]
    public class PrerequisiteAllProperties : IConceptInfo
    {
        [ConceptKey]
        public EntityInfo DependsOn { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class PrerequisiteAllPropertiesMacro : IConceptMacro<PrerequisiteAllProperties>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(PrerequisiteAllProperties conceptInfo, IDslModel existingConcepts)
        {
            var dependsOnProperties = existingConcepts.FindByType<PropertyInfo>().Where(p => p.DataStructure == conceptInfo.DependsOn);
            return dependsOnProperties
                .Select(dependsOnProperty => new SqlDependsOnPropertyInfo { Dependent = conceptInfo, DependsOn = dependsOnProperty })
                .ToList();
        }
    }
}
