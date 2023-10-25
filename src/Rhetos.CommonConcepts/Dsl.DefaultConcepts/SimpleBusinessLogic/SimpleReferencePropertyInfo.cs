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
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    ///  Lookup field (N:1 relationship). The generated C# property has type of the referenced class. 
    ///  Generates foreign key constraint in database. The database column has "ID" suffix.
    ///  Simplified syntax in case when the property name is same as the name of the referenced entity.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Reference")]
    public class SimpleReferencePropertyInfo : ReferencePropertyInfo, IAlternativeInitializationConcept
    {
        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Referenced" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Referenced = new DataStructureInfo { Module = DataStructure.Module, Name = Name };
            createdConcepts = null;
        }
    }
}
