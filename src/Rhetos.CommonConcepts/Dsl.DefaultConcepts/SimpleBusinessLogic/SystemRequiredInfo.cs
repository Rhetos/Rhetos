﻿/*
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
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Subvariant of Required. The value must be entered by system internally.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SystemRequired")]
    public class SystemRequiredInfo : IMacroConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            string filterName = "SystemRequired" + Property.Name;

            var filter = new ItemFilterInfo
            {
                Source = Property.DataStructure,
                FilterName = filterName,
                Expression = "item => item." + Property.Name + " == null"
            };
            var invalidData = new InvalidDataInfo
            {
                Source = Property.DataStructure,
                FilterType = filterName,
                ErrorMessage = "System required property {0} is not set."
            };
            var messageParameters = new InvalidDataMessageParametersConstantInfo
            {
                InvalidData = invalidData,
                MessageParameters = CsUtility.QuotedString(Property.GetUserDescription())
            };
            var invalidProperty = new InvalidDataMarkProperty2Info
            {
                InvalidData = invalidData,
                MarkProperty = Property
            };

            return new IConceptInfo[] { filter, invalidData, messageParameters, invalidProperty };
        }
    }
}
