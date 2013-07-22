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
    public class EntityHistoryExInfo : IConceptInfo, IMacroConcept
   {
        // TODO: Remove this concept atfer implementing alternative constructors.

        [ConceptKey]
        public EntityInfo Entity { get; set; }

        public EntityInfo HistoryEntity { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();
            var legacyEntityForFullHistory = new LegacyEntityInfo
            {
                Module = this.Entity.Module,
                Name = this.Entity.Name + "_FullHistory",
                Table = this.Entity.Module + "." + this.Entity.Name + "_FullHistory",
                View = this.Entity.Module + "." + this.Entity.Name + "_FullHistory"
            };
            var fullHistoryActiveUntilPropertyInfo = new DateTimePropertyInfo
            {
                DataStructure = legacyEntityForFullHistory,
                Name = "ActiveUntil"
            };
            var propertiesForLegacyEntity = new AllPropertiesFromInfo
            {
                Source = this.HistoryEntity,
                Destination = legacyEntityForFullHistory
            };
            var allItemsFilter = new ItemFilterInfo
            {
                Expression = "(i => true)",
                FilterName = "AllItems",
                Source = legacyEntityForFullHistory
            };
            var lockLegacyEntityForChanges = new LockItemsInfo
            {
                FilterType = "AllItems",
                Source = legacyEntityForFullHistory,
                Title = "Full history does not allow changes."
            };

            newConcepts.AddRange(new IConceptInfo[] { legacyEntityForFullHistory, fullHistoryActiveUntilPropertyInfo, propertiesForLegacyEntity, allItemsFilter, lockLegacyEntityForChanges });

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
