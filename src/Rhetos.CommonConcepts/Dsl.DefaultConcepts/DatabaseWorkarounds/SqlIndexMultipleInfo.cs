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
    /// Index on one on more columns in database.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlIndexMultiple")]
    public class SqlIndexMultipleInfo : IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        [ConceptKey]
        public string PropertyNames { get; set; }

        public static bool IsSupported(DataStructureInfo dataStructure)
        {
            return dataStructure is IWritableOrmDataStructure;
        }

        /// <summary>
        /// Returns whether the data validation will be implemented in the database (using unique index) or in the application.
        /// </summary>
        public bool SqlImplementation()
        {
            return DataStructure is EntityInfo;
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!IsSupported(DataStructure))
                throw new DslConceptSyntaxException(this,
                    $"SQL index can only be used in a writable data structure." +
                    $" '{DataStructure.FullName}' is a '{DataStructure.GetKeywordOrTypeName()}'.");

            DslUtility.ValidatePropertyListSyntax(PropertyNames, this);
        }
    }

    [Export(typeof(IConceptMacro))]
    public class SqlIndexMultipleMacro : IConceptMacro<SqlIndexMultipleInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SqlIndexMultipleInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            conceptInfo.CheckSemantics(existingConcepts);

            var names = conceptInfo.PropertyNames.Split(' ');
            if (names.Distinct().Count() != names.Length)
                throw new DslConceptSyntaxException(conceptInfo, "Duplicate property name in index list '" + conceptInfo.PropertyNames + "'.");
            if (names.Length == 0)
                throw new DslConceptSyntaxException(conceptInfo, "Empty property list.");

            SqlIndexMultiplePropertyInfo lastIndexProperty = null;
            for (int i = 0; i < names.Length; i++)
            {
                var property = new PropertyInfo { DataStructure = conceptInfo.DataStructure, Name = names[i] };
                SqlIndexMultiplePropertyInfo indexProperty;
                if (i == 0)
                    indexProperty = new SqlIndexMultiplePropertyInfo { SqlIndex = conceptInfo, Property = property };
                else
                    indexProperty = new SqlIndexMultipleFollowingPropertyInfo { SqlIndex = conceptInfo, Property = property, PreviousIndexProperty = lastIndexProperty };

                newConcepts.Add(indexProperty);
                lastIndexProperty = indexProperty;
            }

            return newConcepts;
        }
    }
}