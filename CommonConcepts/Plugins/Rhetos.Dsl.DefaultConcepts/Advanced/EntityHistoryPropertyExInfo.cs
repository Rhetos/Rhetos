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
    public class EntityHistoryPropertyExInfo : IValidationConcept, IMacroConcept
    {
        // TODO: Remove this concept after implementing alternative constructors for DSL concepts. EntityHistoryPropertyInfo should have its own EntityHistory property (its dependency), initialized in the constructor.

        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public EntityHistoryExInfo EntityHistory { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property, EntityHistory.Entity, this);
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var historyEntity = new EntityInfo { Module = Property.DataStructure.Module, Name = Property.DataStructure.Name + "_History" };

            return new IConceptInfo[]
            {
                new SqlDependsOnPropertyInfo { Dependent = EntityHistory, DependsOn = Property },
                new SqlDependsOnDataStructureInfo { Dependent = EntityHistory, DependsOn = historyEntity },
                new PropertyFromInfo { Destination = historyEntity, Source = Property }
            };
        }
    }
}
