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
    /// <summary>
    /// Generates default constraint on the database column. Note: This concept is used only for internal features implemented in SQL procedures and triggers.
    /// It cannot be used for default field value when writing data to Web API or in object model, 
    /// because the saved record will always have the property value set to NULL by Entity Framework, even if the value is not provided.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlDefault")]
    public class SqlDefaultPropertyInfo : IValidatedConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Definition { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!(Property.DataStructure is EntityInfo))
                throw new DslSyntaxException(string.Format(
                    "SqlDefault can only be used on entity properties. Property {0} is not entity property.",
                    Property));
        }
    }
}