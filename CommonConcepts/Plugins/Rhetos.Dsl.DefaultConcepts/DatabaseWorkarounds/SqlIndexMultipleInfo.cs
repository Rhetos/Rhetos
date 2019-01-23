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
        public DataStructureInfo DataStructure { get; set; }

        [ConceptKey]
        public string PropertyNames { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            CheckSemantics(existingConcepts);

            var names = PropertyNames.Split(' ');
            if (names.Distinct().Count() != names.Count())
                throw new DslSyntaxException(this, "Duplicate property name in index list '" + PropertyNames + "'.");
            if (names.Count() == 0)
                throw new DslSyntaxException(this, "Empty property list.");

            SqlIndexMultiplePropertyInfo lastIndexProperty = null;
            for (int i = 0; i < names.Count(); i++)
            {
                var property = new PropertyInfo { DataStructure = DataStructure, Name = names[i] };
                SqlIndexMultiplePropertyInfo indexProperty;
                if (i == 0)
                    indexProperty = new SqlIndexMultiplePropertyInfo { SqlIndex = this, Property = property };
                else
                    indexProperty = new SqlIndexMultipleFollowingPropertyInfo { SqlIndex = this, Property = property, PreviousIndexProperty = lastIndexProperty };

                newConcepts.Add(indexProperty);
                lastIndexProperty = indexProperty;
            }

            return newConcepts;
        }

        public static bool IsSupported(DataStructureInfo dataStructure)
        {
            return dataStructure is IWritableOrmDataStructure;
        }

        public static void CheckIfSupported(DataStructureInfo dataStructure, IConceptInfo errorContext)
        {
            if (!IsSupported(dataStructure))
                throw new DslSyntaxException(errorContext,
                    $"SQL index can only be used in a writable data structure." +
                    $" '{dataStructure}' is a '{dataStructure.GetType().Name}'.");
        }

        /// <summary>
        /// Returns whether the data validation will be implemented in the database (using unique index) or in the application.
        /// </summary>
        public bool SqlImplementation()
        {
            return DataStructure is EntityInfo;
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            CheckIfSupported(DataStructure, this);

            DslUtility.ValidatePropertyListSyntax(PropertyNames, this);
        }
    }
}
