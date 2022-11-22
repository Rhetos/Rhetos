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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Sets the default property values when inserting a new record.
    /// The value is specified by a lambda expression with the inserted item as a parameter,
    /// for example 'item => "DefaultName"'.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("DefaultValue")]
    public class DefaultValueInfo : IConceptInfo, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Expression { get; set; }

        public DefaultValuesInfo Dependency_DefaultValues { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_DefaultValues" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_DefaultValues = new DefaultValuesInfo { DataStructure = Property.DataStructure };
            createdConcepts = new IConceptInfo[] { Dependency_DefaultValues };
        }
    }
}
