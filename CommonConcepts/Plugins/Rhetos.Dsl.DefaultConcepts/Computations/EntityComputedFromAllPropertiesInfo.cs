/*
    Copyright (C) 2013 Omega software d.o.o.

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
    public class EntityComputedFromAllPropertiesInfo : IMacroConcept
    {
        [ConceptKey]
        public EntityComputedFromInfo EntityComputedFrom { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var sourceProperties = existingConcepts.OfType<PropertyInfo>().Where(p => p.DataStructure == EntityComputedFrom.Source);

            // Some computed properties might be customized, so ignore existing ones:

            var existingComputedPropertiesSource = new HashSet<string>(
                existingConcepts.OfType<PropertyComputedFromInfo>()
                    .Where(comp => comp.EntityComputedFrom == EntityComputedFrom)
                    .Select(comp => comp.Source.Name));

            var newSourceProperties = sourceProperties
                .Where(sp => !existingComputedPropertiesSource.Contains(sp.Name)).ToArray();

            // Clone source properties, including their cascade delete and extension concepts (only for automatically created propeties):

            newConcepts.AddRange(newSourceProperties.Select(sp => new PropertyFromInfo { Source = sp, Destination = EntityComputedFrom.Target }));

            AllPropertiesFromInfo.CloneExtension(EntityComputedFrom.Source, EntityComputedFrom.Target, existingConcepts, newConcepts);

            newConcepts.AddRange(existingConcepts.OfType<ReferenceCascadeDeleteInfo>()
                .Where(ci => ci.Reference.DataStructure == EntityComputedFrom.Source)
                .Where(ci => newSourceProperties.Contains(ci.Reference))
                .Select(ci => new ReferenceCascadeDeleteInfo
                {
                    Reference = new ReferencePropertyInfo
                    {
                        DataStructure = EntityComputedFrom.Target,
                        Name = ci.Reference.Name,
                        Referenced = ci.Reference.Referenced
                    }
                }));

            // Assign ComputedFrom to the target properties and extension:

            newConcepts.AddRange(newSourceProperties.Select(sp =>
                    new PropertyComputedFromInfo
                    {
                        Target = new PropertyInfo { DataStructure = EntityComputedFrom.Target, Name = sp.Name },
                        Source = sp,
                        EntityComputedFrom = EntityComputedFrom
                    }));

            IConceptInfo extensionComputedFrom = existingConcepts.OfType<DataStructureExtendsInfo>()
                .Where(extension => extension.Extension == EntityComputedFrom.Source)
                .Select(extension => new ExtensionComputedFromInfo { EntityComputedFrom = EntityComputedFrom })
                .SingleOrDefault();

            if (extensionComputedFrom != null)
                newConcepts.Add(extensionComputedFrom);

            return newConcepts;
        }
    }
}
