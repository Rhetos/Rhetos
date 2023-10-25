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
    /// Declares an IEnumerable filter on the given data structure.
    /// The <see cref="FilterInfo.Parameter"/> property is the filter parameter type; it also of represents the filter name.
    /// </summary>
    /// <remarks>
    /// This concept generates a partial method 'Filter' on the repository class, in order to specify the method's inputs and outputs.
    /// This partial method should be implemented by developer on another partial class implementation for the repository class.
    /// The filter method should return the subset of the given items.
    /// Note that in many cases it is preferred to use a QueryFilter concept instead;
    /// it builds a LINQ query and executes the filter directly in database instead of in memory,
    /// resulting with better performance.
    /// </remarks>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Filter")]
    public class FilterPrototypeInfo : FilterInfo
    {
    }
}
