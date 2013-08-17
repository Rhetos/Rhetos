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
    [ConceptKeyword("MaxLength")]
    public class MaxLengthInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Length { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            // Expand the base entity:
            var itemFilterMinLengthProperty = new ItemFilterInfo
            {
                Expression = String.Format("item => !String.IsNullOrEmpty(item.{0}) && item.{0}.Length > {1}", Property.Name, Length),
                FilterName = Property.Name + "_MaxLengthFilter",
                Source = Property.DataStructure
            };
            var denySaveMinLengthProperty = new DenySaveForPropertyInfo
            {
                DependedProperties = Property,
                FilterType = itemFilterMinLengthProperty.FilterName,
                Title = String.Format("Maximum allowed length of {0} is {1} characters.", Property.Name, Length),
                Source = Property.DataStructure
            };
            return new IConceptInfo[] { itemFilterMinLengthProperty, denySaveMinLengthProperty };
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            int i;

            if (!(this.Property is ShortStringPropertyInfo || this.Property is LongStringPropertyInfo))
                throw new DslSyntaxException("MaxLength can only be used on ShortString or LongString.");

            if (!Int32.TryParse(this.Length, out i))
                throw new DslSyntaxException("Length is not an integer.");
        }
    }
}
