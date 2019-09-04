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
    [ConceptKeyword("Unique")]
    public class UniqueProperties3Info : IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }
        [ConceptKey]
        public PropertyInfo Property1 { get; set; }
        [ConceptKey]
        public PropertyInfo Property2 { get; set; }
        [ConceptKey]
        public PropertyInfo Property3 { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property1, DataStructure, this);
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property2, DataStructure, this);
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property3, DataStructure, this);
        }
    }

    [Export(typeof(IConceptMacro))]
    public class UniqueProperties3Macro : IConceptMacro<UniqueProperties3Info>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(UniqueProperties3Info conceptInfo, IDslModel existingConcepts)
        {
            return new[]
            {
                new UniqueMultiplePropertiesInfo
                {
                    DataStructure = conceptInfo.DataStructure,
                    PropertyNames = conceptInfo.Property1.Name + " " + conceptInfo.Property2.Name + " " + conceptInfo.Property3.Name
                }
            };
        }
    }
}
