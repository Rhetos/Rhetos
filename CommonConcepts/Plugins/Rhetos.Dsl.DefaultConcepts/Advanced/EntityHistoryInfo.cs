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
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("History")]
    public class EntityHistoryInfo : IMacroConcept
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Expand the base entity:
            var activeSinceProperty = new DateTimePropertyInfo { DataStructure = Entity, Name = "ActiveSince" }; // TODO: SystemRequired, Default 1.1.1900.
            var activeSinceHistory = new EntityHistoryPropertyInfo { Property = activeSinceProperty };
            newConcepts.AddRange(new IConceptInfo[] { activeSinceProperty, activeSinceHistory });

            // Create a new entity for history data:
            var historyEntity = new EntityInfo { Module = Entity.Module, Name = Entity.Name + "_History" };
            var currentProperty = new ReferencePropertyInfo { DataStructure = historyEntity, Name = "Entity", Referenced = Entity };
            var currentDetail = new ReferenceDetailInfo { Reference = currentProperty };
            var currentRequired = new RequiredPropertyInfo { Property = currentProperty }; // TODO: SystemRequired
            var cloneActiveSincePropertyWithRelatedFeatures = new PropertyFromInfo { Destination = historyEntity, Source = activeSinceProperty };
            var historyActiveSinceProperty = new DateTimePropertyInfo { DataStructure = historyEntity, Name = activeSinceProperty.Name };
            var unique = new UniquePropertiesInfo { DataStructure = historyEntity, Property1 = currentProperty, Property2 = historyActiveSinceProperty };
            newConcepts.AddRange(new IConceptInfo[] { historyEntity, currentProperty, currentDetail, currentRequired, historyActiveSinceProperty, unique, cloneActiveSincePropertyWithRelatedFeatures });

            var entityHistoryEx = new EntityHistoryExInfo { Entity = Entity, HistoryEntity = historyEntity }; // TODO: Remove this concept atfer implementing alternative constructors.
            newConcepts.Add(entityHistoryEx);

            return newConcepts;
        }
    }
}
