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

using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// A read method that returns a LINQ query for the given parameter value.
    /// The code snippet should be a lambda expression that returns the query:
    /// <c>parameter => IQueryable&lt;DataStructureType&gt;</c>.
    /// The parameter type also represents the filter name.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Query")]
    public class QueryExpressionInfo : QueryInfo, IValidatedConcept
    {
        public string QueryImplementation { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!Parameter.Contains('.'))
                throw new DslSyntaxException(this, "ParameterType must be full type name, including Module name for a data structure, or C# namespace for other parameter types.");
        }
    }
}
