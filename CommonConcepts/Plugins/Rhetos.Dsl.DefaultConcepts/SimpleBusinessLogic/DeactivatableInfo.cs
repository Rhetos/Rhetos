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
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Deactivatable")]
    public class DeactivateableInfo : IConceptInfo, IMacroConcept
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
            var requiredPropertyInfo = new RequiredPropertyInfo
            {
                Property = activePropertyInfo
            };
            var justActiveItems = new ItemFilterInfo
            {
                Expression = "item => item.Active.Value",
                FilterName = "ActiveItems",
                Source = Entity
            };
            var parameterForActiveOrThisFilter = new ParameterInfo
            {
                Module = Entity.Module,
                Name = Entity.Name + "_ThisAndActiveItems"
            };
            var parameterIdPropertyInfo = new ReferencePropertyInfo
            {
                Name = "Item",
                Referenced = Entity,
                DataStructure = parameterForActiveOrThisFilter
            };
            var composableFilterActiveOrThis = new ComposableFilterByInfo
            {
                Expression = @"(items, repository, filterParameter) => items.Where(item => (!item.Active.HasValue || item.Active.Value) || item == filterParameter.Item)",
                Parameter = Entity.Name + "_ThisAndActiveItems",
                Source = Entity
            };

            var concepts = new IConceptInfo[] { activePropertyInfo, requiredPropertyInfo, justActiveItems, parameterForActiveOrThisFilter, parameterIdPropertyInfo, composableFilterActiveOrThis };
            return concepts;
        }
    }
}
