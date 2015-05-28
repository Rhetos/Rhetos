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

using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Deactivatable")]
    public class DeactivatableInfo : IConceptInfo, IMacroConcept
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; }
        
        public System.Collections.Generic.IEnumerable<IConceptInfo> CreateNewConcepts(System.Collections.Generic.IEnumerable<IConceptInfo> existingConcepts)
        {
            var activePropertyInfo = new BoolPropertyInfo
            {
                DataStructure = Entity,
                Name = "Active"
            };
            
            var composableFilterActiveAndThis = new ComposableFilterByInfo
            {
                Expression = @"(items, repository, parameter) =>
                    {
                        if (parameter != null && parameter.ItemID.HasValue)
                            return items.Where(item => item.Active == null || item.Active.Value || item.ID == parameter.ItemID.Value);
                        else
                            return items.Where(item => item.Active == null || item.Active.Value);
                    }",
                Parameter = "Rhetos.Dom.DefaultConcepts.ActiveItems",
                Source = Entity
            };

            var systemRequired = new SystemRequiredInfo { Property = activePropertyInfo };

            return new IConceptInfo[] { activePropertyInfo, composableFilterActiveAndThis, systemRequired };
        }
    }
}
