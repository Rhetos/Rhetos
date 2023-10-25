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

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// A read method that filters the given items, with a filter parameter.
    /// The lambda expression returns a subset of a given items,
    /// for example: <c>(items, parameter) => items.Where(...).ToList()</c>.
    /// The parameter type also represents the filter name.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Filter")]
    public class FilterExpressionInfo : FilterInfo
    {
        /// <summary>
        /// A lambda expression that returns a subset of the given items, for example:
        /// <c>(items, parameter) => items.Where(...).ToList()</c>.
        /// </summary>
        public string Expression { get; set; }
    }
}
