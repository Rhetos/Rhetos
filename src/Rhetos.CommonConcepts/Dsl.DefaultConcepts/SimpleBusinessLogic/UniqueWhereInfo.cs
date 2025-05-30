﻿/*
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
    /// Filter used when creating a unique index.
    /// Check official documentation for adding conditions when creating an index.
    /// https://learn.microsoft.com/en-us/sql/t-sql/statements/create-index-transact-sql?view=sql-server-ver16#syntax
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Where")]
    public class UniqueWhereInfo : SqlIndexWhereInfo, IAlternativeInitializationConcept
    {
        public UniquePropertyInfo Unique { get; set; }

        public string UniqueSqlFilter { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { 
                nameof(SqlIndex),
                nameof(SqlFilter)
            };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            SqlFilter = UniqueSqlFilter;
            SqlIndex = new SqlIndexMultipleInfo
            {
                DataStructure = Unique.Property.DataStructure,
                PropertyNames = Unique.Property.Name
            };
            createdConcepts = null;
        }
    }
}
