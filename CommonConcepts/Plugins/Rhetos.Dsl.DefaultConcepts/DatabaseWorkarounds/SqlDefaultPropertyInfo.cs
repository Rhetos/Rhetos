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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlDefault")]
    public class SqlDefaultPropertyInfo : IConceptInfo, IValidationConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Definition { get; set; }

        public override string ToString()
        {
            return Property.ToString() + " SqlDefault";
        }

        public override int GetHashCode()
        {
            return Property.GetHashCode();
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (!(Property.DataStructure is EntityInfo))
                throw new DslSyntaxException(string.Format(
                    "SqlDefault can only be used on entity properties. Property {0} is not entity property.",
                    Property));
        }
    }
}