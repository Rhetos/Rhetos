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
    /// <remark>
    /// This is a generic version of the Clustered concept syntax, that helps with DSL parser disambiguation.
    /// This concept will be used if the Clustered keyword is placed flat in the Entity.
    /// Other concepts with Clustered keyword will be used if the Clustered keyword is nested in a specific indexing concept.
    /// </remark>
    /// <summary>
    /// Marks the index as clustered.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Clustered")]
    public class SqlIndexClusteredFlatInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        [ConceptKey]
        public string PropertyNames { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class SqlIndexClusteredFlatMacro : IConceptMacro<SqlIndexClusteredFlatInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SqlIndexClusteredFlatInfo conceptInfo, IDslModel existingConcepts)
        {
            return new[]
            {
                new SqlIndexClusteredInfo
                {
                    SqlIndex = new SqlIndexMultipleInfo
                    {
                        DataStructure = conceptInfo.DataStructure,
                        PropertyNames = conceptInfo.PropertyNames
                    }
                }
            };
        }
    }
}
