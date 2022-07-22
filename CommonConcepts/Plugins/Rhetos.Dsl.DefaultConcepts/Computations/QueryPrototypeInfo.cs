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
    /// Declares a method for querying data on the given data structure.
    /// The <see cref="QueryInfo.Parameter"/> property is the input parameter type; it also of represents the filter name.
    /// </summary>
    /// <remarks>
    /// This concept generates a partial method 'Query' on the repository class, in order to specify the method's inputs and outputs.
    /// This partial method should be implemented by developer on another partial class implementation for the repository class.
    /// The method must return the IQueryable of the <see cref="QueryInfo.DataStructure"/> type.
    /// </remarks>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Query")]
    public class QueryPrototypeInfo : QueryInfo
    {
    }
}
