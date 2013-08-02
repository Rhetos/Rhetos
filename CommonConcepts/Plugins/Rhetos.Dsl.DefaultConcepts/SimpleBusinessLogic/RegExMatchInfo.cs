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
    [ConceptKeyword("RegExMatch")]
    public class RegExMatchInfo : IMacroConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Regex2 { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            // Expand the base entity:
            var itemFilterRegExMatchProperty = new ItemFilterInfo
            {
                Expression = String.Format(@"item => !String.IsNullOrEmpty(item.{0}) && !(new System.Text.RegularExpressions.Regex(""{1}"")).IsMatch(item.{0})", Property.Name, Regex2),
                FilterName = Property.Name + "_RegExMatchFilter",
                Source = Property.DataStructure
            };
            var denySaveRegExMatchProperty = new DenySaveForPropertyInfo
            {
                DependedProperties = Property,
                FilterType = itemFilterRegExMatchProperty.FilterName,
                Title = String.Format("{0} has to match {1}.", Property.Name, Regex2),
                Source = Property.DataStructure
            };
            return new IConceptInfo[] { itemFilterRegExMatchProperty, denySaveRegExMatchProperty };
        }
    }
}
