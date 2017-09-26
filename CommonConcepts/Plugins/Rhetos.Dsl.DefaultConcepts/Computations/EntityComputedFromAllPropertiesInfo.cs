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
    public class EntityComputedFromAllPropertiesInfo : IConceptInfo, IValidatedConcept
    {
        [ConceptKey]
        public EntityComputedFromInfo EntityComputedFrom { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            var computedProperties = existingConcepts.FindByType<PropertyComputedFromInfo>()
                .Where(pcf => pcf.Target.DataStructure == EntityComputedFrom.Target && pcf.Source.DataStructure == EntityComputedFrom.Source);

            var duplicates = computedProperties.GroupBy(cp => cp.Source.Name, cp => cp.Target.Name).Where(g => g.Count() >= 2).FirstOrDefault();
            if (duplicates != null)
                throw new DslSyntaxException(this, $"Source property '{duplicates.Key}' is mapped to two target properties: '{duplicates.First()}' and '{duplicates.Last()}'.");
        }
    }

    /// <summary>
    /// EntityComputedFromAllPropertiesInfo's macro is split into two classes to allow macro optimization to evaluate
    /// AllPropertiesWithCascadeDeleteFromInfo between the two. This will reduce the number of macro evaluation cycles.
    /// </summary>
    [Export(typeof(IConceptMacro))]
    public class EntityComputedFromAllPropertiesCopyPropertiesMacro : IConceptMacro<EntityComputedFromAllPropertiesInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(EntityComputedFromAllPropertiesInfo conceptInfo, IDslModel existingConcepts)
        {
            return new[]
            {
                new AllPropertiesWithCascadeDeleteFromInfo
                {
                    Destination = conceptInfo.EntityComputedFrom.Target,
                    Source = conceptInfo.EntityComputedFrom.Source
                }
            };
        }
    }

    [Export(typeof(IConceptMacro))]
    public class EntityComputedFromAllPropertiesMacro : IConceptMacro<EntityComputedFromAllPropertiesInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(EntityComputedFromAllPropertiesInfo conceptInfo, IDslModel existingConcepts)
        {
            // EntityComputedFromAllPropertiesCopyPropertiesMacro copies the properties for the source class.
            // This method adds computation mapping on the properties.

            return existingConcepts.FindByReference<PropertyInfo>(p => p.DataStructure, conceptInfo.EntityComputedFrom.Source)
                .Select(sp =>
                    new PropertyComputedFromInfo
                    {
                        Target = new PropertyInfo { DataStructure = conceptInfo.EntityComputedFrom.Target, Name = sp.Name },
                        Source = sp,
                        Dependency_EntityComputedFrom = conceptInfo.EntityComputedFrom
                    });
        }
    }
}
