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
    [ConceptKeyword("SamePropertyValue")]
    public class SamePropertyValue2Info : IConceptInfo
    {
        [ConceptKey]
        public PropertyInfo DerivedProperty { get; set; }

        [ConceptKey]
        public string Path { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class SamePropertyValueMacro : IConceptMacro<SamePropertyValue2Info>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SamePropertyValue2Info conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();
            var baseSelector = conceptInfo.Path.Substring(0, conceptInfo.Path.LastIndexOf('.'));
            var result = DslUtility.GetPropertyByPath(conceptInfo.DerivedProperty.DataStructure, conceptInfo.Path, existingConcepts);
            if (result.Error != null)
                throw new DslSyntaxException(result.Error);
            newConcepts.Add(new SamePropertyValueInfo
            {
                DerivedProperty = conceptInfo.DerivedProperty,
                BaseSelector = baseSelector,
                BaseProperty = result.Value
            });
            return newConcepts;
        }
    }
}
