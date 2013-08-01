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
    public class EntityHistoryInfo : IMacroConcept, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; }
        
        public EntityInfo HistoryEntity { get; set; }
        public LegacyEntityInfo FullHistoryEntity { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new string[] { "HistoryEntity", "FullHistoryEntity" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            HistoryEntity = new EntityInfo { Module = Entity.Module, Name = Entity.Name + "_History" };
            FullHistoryEntity = new LegacyEntityInfo
            {
                Module = this.Entity.Module,
                Name = this.Entity.Name + "_FullHistory",
                Table = this.Entity.Module + "." + this.Entity.Name + "_FullHistory",
                View = this.Entity.Module + "." + this.Entity.Name + "_FullHistory"
            };
            createdConcepts = new IConceptInfo[] { HistoryEntity, FullHistoryEntity };
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Expand the base entity:
            var activeSinceProperty = new DateTimePropertyInfo { DataStructure = Entity, Name = "ActiveSince" }; // TODO: SystemRequired, Default 1.1.1900.
            var activeSinceHistory = new EntityHistoryPropertyInfo { Property = activeSinceProperty };
            newConcepts.AddRange(new IConceptInfo[] { activeSinceProperty, activeSinceHistory });

            // DenySave for base entity: it is not allowed to save with ActiveSince older than last one used in History
            var denyFilter = new ItemFilterInfo { 
                FilterName = Entity.Name + "OlderThanHistoryEntries", 
                Source = Entity,
                Expression = String.Format("item => repository.{0}.{1}_History.Query().Where(his => his.ActiveSince > item.ActiveSince && his.Entity == item).Count() > 0", 
                                Entity.Module.Name, 
                                Entity.Name) 
            };
            var denySaveValidation = new DenySaveForPropertyInfo { 
                FilterType = Entity.Name + "OlderThanHistoryEntries", 
                Source = Entity, 
                Title = "ActiveSince is not allowed to be older than last entry in history.", 
                DependedProperties = activeSinceProperty 
            };
            newConcepts.AddRange(new IConceptInfo[] { denyFilter, denySaveValidation });

            // Create a new entity for history data:
            var currentProperty = new ReferencePropertyInfo { DataStructure = HistoryEntity, Name = "Entity", Referenced = Entity };
            var currentDetail = new ReferenceDetailInfo { Reference = currentProperty };
            var currentRequired = new RequiredPropertyInfo { Property = currentProperty }; // TODO: SystemRequired
            var cloneActiveSincePropertyWithRelatedFeatures = new PropertyFromInfo { Destination = HistoryEntity, Source = activeSinceProperty };
            var historyActiveSinceProperty = new DateTimePropertyInfo { DataStructure = HistoryEntity, Name = activeSinceProperty.Name };
            var unique = new UniquePropertiesInfo { DataStructure = HistoryEntity, Property1 = currentProperty, Property2 = historyActiveSinceProperty };
            newConcepts.AddRange(new IConceptInfo[] { currentProperty, currentDetail, currentRequired, historyActiveSinceProperty, unique, cloneActiveSincePropertyWithRelatedFeatures });

            // Creates properties of FullHistoryEntity
            var fullHistoryActiveUntilPropertyInfo = new DateTimePropertyInfo
            {
                DataStructure = FullHistoryEntity,
                Name = "ActiveUntil"
            };
            var propertiesForLegacyEntity = new AllPropertiesFromInfo
            {
                Source = this.HistoryEntity,
                Destination = FullHistoryEntity
            };

            newConcepts.AddRange(new IConceptInfo[] { fullHistoryActiveUntilPropertyInfo, propertiesForLegacyEntity });

            // Creates extension on history data (for ActiveUntil):
            var legacyEntityForActiveUntil = new LegacyEntityInfo
            {
                Module = this.Entity.Module,
                Name = this.Entity.Name + "_History_ActiveUntil",
                Table = this.Entity.Module + "." + this.Entity.Name + "_History_ActiveUntil",
                View = this.Entity.Module + "." + this.Entity.Name + "_History_ActiveUntil"
            };
            var historyActiveUntilProperty = new DateTimePropertyInfo
            {
                DataStructure = legacyEntityForActiveUntil,
                Name = "ActiveUntil"
            };
            var historyActiveUntilEx = new DataStructureExtendsInfo
            {
                Base = (DataStructureInfo)existingConcepts.Where<IConceptInfo>(t => t is DataStructureInfo).Where(t => ((DataStructureInfo)t).Module.Name == this.Entity.Module.Name && ((DataStructureInfo)t).Name == this.Entity.Name + "_History").Single(),
                Extension = legacyEntityForActiveUntil
            };
            newConcepts.AddRange(new IConceptInfo[] { legacyEntityForActiveUntil, historyActiveUntilProperty, historyActiveUntilEx });

            return newConcepts;
        }

    }
}
