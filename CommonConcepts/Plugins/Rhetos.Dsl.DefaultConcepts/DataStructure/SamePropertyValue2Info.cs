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
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Used for internal optimizations when a property on one data structure returns the same value
    /// as a property on referenced (base or parent) data structure.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SamePropertyValue")]
    [Obsolete("Use simpler SamePropertyValue syntax with a path to the base property.")]
    public class SamePropertyValue2Info : IValidatedConcept
    {
        [ConceptKey]
        public PropertyInfo DerivedProperty { get; set; }

        /// <summary>Object model property name on the inherited data structure that references the base data structure class.</summary>
        [ConceptKey]
        public string BaseSelector { get; set; }

        public PropertyInfo BaseProperty { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.ValidateIdentifier(BaseSelector, this,
                $"BaseSelector should be set to a property name from '{DerivedProperty.DataStructure.FullName}' class.");
        }
    }

    [Export(typeof(IConceptMacro))]
    public class SamePropertyValue2Macro : IConceptMacro<SamePropertyValue2Info>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SamePropertyValue2Info conceptInfo, IDslModel existingConcepts)
        {
            return new[]
            {
                new SamePropertyValueInfo
                {
                    DerivedProperty = conceptInfo.DerivedProperty,
                    Path = conceptInfo.BaseSelector + "." + conceptInfo.BaseProperty.Name
                }
            };
        }
    }
}
