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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Simple data validation with a constant error message.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("InvalidData")]
    public class InvalidDataInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string FilterType { get; set; }

        /// <summary>
        /// Simple rule description. The error messages might be overridden by other more complex concepts.
        /// </summary>
        public string ErrorMessage { get; set; }

        public static readonly ConceptMetadataKey<bool> AllowSaveMetadata = "AllowSave";

        public string GetErrorMessageMethodName()
        {
            string filterName = null;

            if (FilterType.StartsWith(Source.Module.Name + "."))
            {
                string dataStructureName = FilterType.Substring(Source.Module.Name.Length + 1);
                if (CsUtility.GetIdentifierError(dataStructureName) == null)
                    filterName = dataStructureName;
            }

            if (filterName == null)
                filterName = CsUtility.TextToIdentifier(FilterType);

            return "GetErrorMessage_" + filterName;
        }
    }
}
