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
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// A read method that returns a filtered query for the given source query and the parameter value. 
    /// The code snippet should be a lambda expression that returns a subset of the given query:
    /// <c>(IQueryable&lt;DataStructureType&gt; query, repository, parameter) => filtered IQueryable&lt;DataStructureType&gt;</c>.
    /// The parameter type also represents the filter name.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ComposableFilterBy")]
    [Obsolete("Use the QueryFilter concept instead (base class QueryFilterInfo).")] // When finding filters in DSL model, the base filter concept should be used.
    public class ComposableFilterByInfo : QueryFilterInfo
    {
        [Obsolete("Create a QueryFilter concept instead (class QueryFilterExpressionInfo).")] // New filter in code should be created with QueryFilterExpressionInfo.
        public ComposableFilterByInfo()
        {
            // The constructor is here just to add the Obsolete attribute with a messages different from type definition.
        }

        /// <summary>
        /// A lambda expression that returns a subset of a given query with parameter, for example:
        /// <c>(query, repository, parameter) => query.Where(...)</c>.
        /// </summary>
        public string Expression { get; set; }
    }
}
