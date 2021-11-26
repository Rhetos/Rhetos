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
    /// Base concept for IEnumerable filters.
    /// This concept assumes that there will be a <c>Filter</c> method implemented in the repository class,
    /// with parameters <c>(IEnumerable&lt;DataStructureType&gt; items, ParameterType filterParameter)</c>,
    /// returning a subset of the given <c>items</c>.
    /// The <see cref="Parameter"/> property is the filterParameter type;
    /// it also represents the filter name.
    /// Note that in many cases it is preferred to use a <see cref="QueryFilterInfo"/> implementation instead;
    /// it builds a LINQ query and executes the filter directly in database instead of in memory,
    /// resulting with better performance.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Filter")]
    public class FilterInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        /// <summary>
        /// Parameter type. It can be a DataStructure name or any C# type.
        /// It also represents the filter name.
        /// </summary>
        [ConceptKey]
        public string Parameter { get; set; }
    }
}
