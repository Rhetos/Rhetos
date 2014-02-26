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

using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("From")]
    public class BrowseFromPropertyInfo : IValidationConcept
    {
        [ConceptKey]
        public PropertyInfo PropertyInfo { get; set; }
        public string Path { get; set; }

        public override string ToString()
        {
            return "From: " + PropertyInfo;
        }

        public override int GetHashCode()
        {
            return PropertyInfo.GetHashCode();
        }

        public void CheckSemantics(System.Collections.Generic.IEnumerable<IConceptInfo> concepts)
        {
            if (!(PropertyInfo.DataStructure is BrowseDataStructureInfo))
                throw new DslSyntaxException(string.Format(
                    "'{0}' cannot be use on {1} ({2}). It may only be used on {3}.",
                    this.GetKeywordOrTypeName(),
                    PropertyInfo.DataStructure.GetKeywordOrTypeName(),
                    PropertyInfo.GetUserDescription(),
                    ConceptInfoHelper.GetKeywordOrTypeName(typeof(BrowseDataStructureInfo))));
        }
    }
}
