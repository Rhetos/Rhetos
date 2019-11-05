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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ModificationTimeOf")]
    public class ModificationTimeOfInfo : IValidatedConcept, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public DateTimePropertyInfo Property { get; set; }

        [ConceptKey]
        public PropertyInfo ModifiedProperty { get; set; }

        public ModificationTimeOfInfrastructureInfo Dependency_Infrastructure { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.CheckIfPropertyBelongsToDataStructure(ModifiedProperty, Property.DataStructure, this);
        }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(Dependency_Infrastructure) };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_Infrastructure = new ModificationTimeOfInfrastructureInfo { DataStructure = Property.DataStructure };
            createdConcepts = new[] { Dependency_Infrastructure };
        }
    }

    [Export(typeof(IConceptMacro))]
    public class ModificationTimeOfMacro : IConceptMacro<ModificationTimeOfInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(ModificationTimeOfInfo conceptInfo, IDslModel existingConcepts)
        {
            if (!(conceptInfo.Property.DataStructure is EntityInfo))
                throw new DslSyntaxException(conceptInfo, $"{conceptInfo.GetKeywordOrTypeName()} can only be used on an Entity, " +
                    $"not on {conceptInfo.Property.DataStructure.GetUserDescription()}.");
            
            var saveMethod = new SaveMethodInfo { Entity = (EntityInfo)conceptInfo.Property.DataStructure };
            var loadOldItems = new LoadOldItemsInfo { SaveMethod = saveMethod };
            return new IConceptInfo[]
            {
                saveMethod,
                loadOldItems,
                new LoadOldItemsTakeInfo { LoadOldItems = loadOldItems, Path = conceptInfo.ModifiedProperty.GetSimplePropertyName()}
            };
        }
    }
}
