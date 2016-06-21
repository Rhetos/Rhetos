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
using System.Reflection;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AllProperties")]
    public class EntityComputedFromAllPropertiesInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityComputedFromInfo EntityComputedFrom { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class EntityComputedFromAllPropertiesMacro : IConceptMacro<EntityComputedFromAllPropertiesInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(EntityComputedFromAllPropertiesInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var sourceProperties = existingConcepts.FindByType<PropertyInfo>().Where(p => p.DataStructure == conceptInfo.EntityComputedFrom.Source);

            // Some computed properties might be customized, so ignore existing ones:

            var existingComputedPropertiesSource = new HashSet<string>(
                existingConcepts.FindByType<PropertyComputedFromInfo>()
                    .Where(comp => comp.Dependency_EntityComputedFrom == conceptInfo.EntityComputedFrom)
                    .Select(comp => comp.Source.Name));

            var newSourceProperties = sourceProperties
                .Where(sp => !existingComputedPropertiesSource.Contains(sp.Name)).ToArray();

            // Clone source properties, including their cascade delete and extension concepts (only for automatically created properties):

            newConcepts.AddRange(newSourceProperties.Select(sp => new PropertyFromInfo { Source = sp, Destination = conceptInfo.EntityComputedFrom.Target }));

            AllPropertiesFromMacro.CloneExtension(conceptInfo.EntityComputedFrom.Source, conceptInfo.EntityComputedFrom.Target, existingConcepts, newConcepts);

            newConcepts.AddRange(existingConcepts.FindByType<ReferenceCascadeDeleteInfo>()
                .Where(ci => ci.Reference.DataStructure == conceptInfo.EntityComputedFrom.Source)
                .Where(ci => newSourceProperties.Contains(ci.Reference))
                .Select(ci => new ReferenceCascadeDeleteInfo
                {
                    Reference = new ReferencePropertyInfo
                    {
                        DataStructure = conceptInfo.EntityComputedFrom.Target,
                        Name = ci.Reference.Name,
                        Referenced = ci.Reference.Referenced
                    }
                }));

            // Assign ComputedFrom to the target properties:

            newConcepts.AddRange(newSourceProperties.Select(sp =>
                    new PropertyComputedFromInfo
                    {
                        Target = new PropertyInfo { DataStructure = conceptInfo.EntityComputedFrom.Target, Name = sp.Name },
                        Source = sp,
                        Dependency_EntityComputedFrom = conceptInfo.EntityComputedFrom
                    }));

            return newConcepts;
        }
    }
}
