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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("UniqueMultiple")]
    public class UniqueMultiplePropertiesInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }
        [ConceptKey]
        public string PropertyNames { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            DslUtility.ValidatePropertyListSyntax(PropertyNames, this);
        }

        public static bool SqlImplementation(UniqueMultiplePropertiesInfo info)
        {
            return info.DataStructure is EntityInfo;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            CheckSemantics(existingConcepts);

            var sqlIndex = new SqlIndexMultipleInfo { Entity = DataStructure, PropertyNames = PropertyNames };
            newConcepts.Add(sqlIndex);

            if (SqlImplementation(this))
            {
                var sqlUnique = new SqlUniqueMultipleInfo { SqlIndex = sqlIndex };
                newConcepts.Add(sqlUnique);
            }

            var properties = PropertyNames.Split(' ')
                .Select(name => new PropertyInfo { DataStructure = DataStructure, Name = name })
                .Select(property => new UniqueMultiplePropertyInfo { Unique = this, Property = property });
            newConcepts.AddRange(properties);

            return newConcepts;
        }
    }
}
