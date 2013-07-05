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
    [ConceptKeyword("Range")]
    public class RangeInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public PropertyInfo PropertyFrom { get; set; }

        [ConceptKey]
        public PropertyInfo PropertyTo { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            // Expand the base entity:
            var itemFilterRange = new ItemFilterInfo
            {
                Expression = String.Format("item => item.{0} != null && item.{1} != null && item.{0} > item.{1}", PropertyFrom.Name, PropertyTo.Name),
                FilterName = PropertyFrom.Name + "_" + PropertyTo.Name + "_RangeFilter",
                Source = PropertyFrom.DataStructure
            };
            var denySaveRange = new DenySaveForPropertyInfo
            {
                DependedProperties = PropertyFrom,
                FilterType = itemFilterRange.FilterName,
                Title = String.Format("Value of {0} has to be less than or equal to {1}.", PropertyFrom.Name, PropertyTo.Name),
                Source = PropertyFrom.DataStructure
            };
            return new IConceptInfo[] { itemFilterRange, denySaveRange };
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (!(this.PropertyFrom.GetType() == this.PropertyTo.GetType()))
                throw new DslSyntaxException(this, "Range can only be used on two properties of same type.");
        }
    }
}
