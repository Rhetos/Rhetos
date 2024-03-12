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

using Rhetos.DatabaseGenerator.DefaultConcepts;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Filter used when creating a unique index that disregards NULL values.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("UniqueWhereNotNull")]
    public class UniqueWhereNotNullInfo : UniquePropertyInfo
    {
    }

    [Export(typeof(IConceptMacro))]
    public class UniqueWhereNotNullMacro : IConceptMacro<UniqueWhereNotNullInfo>
    {
        private readonly ConceptMetadata _conceptMetadata;

        public UniqueWhereNotNullMacro(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(UniqueWhereNotNullInfo conceptInfo, IDslModel existingConcepts)
        {
            return new IConceptInfo[] {
                new SqlIndexWhereInfo {
                    SqlIndex = conceptInfo.Dependency_SqlIndex,
                    SqlFilter = _conceptMetadata.GetColumnName(conceptInfo.Property) + " IS NOT NULL"
                }
            };
        }
    }
}
