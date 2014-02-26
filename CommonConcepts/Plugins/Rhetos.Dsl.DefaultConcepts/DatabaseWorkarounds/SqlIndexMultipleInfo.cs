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
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlIndexMultiple")]
    public class SqlIndexMultipleInfo : IValidationConcept, IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Entity { get; set; } // TODO: Rename to DataStructure.
        [ConceptKey]
        public string PropertyNames { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            CheckSemantics(existingConcepts);

            newConcepts.AddRange(PropertyNames.Split(' ')
                .Select(name => new PropertyInfo { DataStructure = Entity, Name = name })
                .Select(property => new SqlIndexMultiplePropertyInfo { SqlIndex = this, Property = property }));

            return newConcepts;
        }

        public static bool IsSupported(DataStructureInfo dataStructure)
        {
            return dataStructure is IWritableOrmDataStructure;
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (!IsSupported(Entity))
                throw new DslSyntaxException(
                    string.Format("{0} must be used inside writable data structure. DateStructure {1} is of type {2}.",
                        this.GetUserDescription(),
                        Entity,
                        Entity.GetType().FullName));

            DslUtility.ValidatePropertyListSyntax(PropertyNames, this);
        }
    }
}
