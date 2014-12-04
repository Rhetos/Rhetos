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
    [ConceptKeyword("ComputedFrom")]
    public class EntityComputedFromInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityInfo Target { get; set; }

        [ConceptKey]
        public DataStructureInfo Source { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class EntityComputedFromMacro : IConceptMacro<EntityComputedFromInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(EntityComputedFromInfo conceptInfo, IDslModel existingConcepts)
        {
            if (!existingConcepts.FindByType<KeyPropertyComputedFromInfo>().Any(kp => kp.PropertyComputedFrom.Dependency_EntityComputedFrom == conceptInfo)
                && !existingConcepts.FindByType<KeyPropertyIDComputedFromInfo>().Any(kp => kp.EntityComputedFrom == conceptInfo)
                && !existingConcepts.FindByType<PersistedKeyPropertiesInfo>().Any(kp => kp.Persisted == conceptInfo.Target && kp.Persisted.Source == conceptInfo.Source))
                return new[] { new KeyPropertyIDComputedFromInfo { EntityComputedFrom = conceptInfo } };
            return null;
        }
    }
}
