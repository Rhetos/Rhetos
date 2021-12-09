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
    /// A unique constraint over multiple properties: Two records cannot have same combination of values.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("UniqueMultiple")]
    public class UniqueMultiplePropertiesInfo : IAlternativeInitializationConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        [ConceptKey]
        public string PropertyNames { get; set; }

        public SqlIndexMultipleInfo Dependency_SqlIndex { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(Dependency_SqlIndex) };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_SqlIndex = new SqlIndexMultipleInfo { DataStructure = this.DataStructure, PropertyNames = this.PropertyNames };
            createdConcepts = new[] { Dependency_SqlIndex };
        }
    }

    [Export(typeof(IConceptMacro))]
    public class UniqueMultiplePropertiesMacro : IConceptMacro<UniqueMultiplePropertiesInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(UniqueMultiplePropertiesInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            if (conceptInfo.DataStructure is IWritableOrmDataStructure)
                newConcepts.Add(new DataStructureLocalizerInfo { DataStructure = conceptInfo.DataStructure });

            var uniquePropertis = conceptInfo.PropertyNames.Split(' ')
                .Select(name => new PropertyInfo { DataStructure = conceptInfo.DataStructure, Name = name })
                .Select(property => new UniqueMultiplePropertyInfo { Unique = conceptInfo, Property = property });
            newConcepts.AddRange(uniquePropertis);

            return newConcepts;
        }
    }
}
