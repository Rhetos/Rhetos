/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    /// <summary>
    /// Implement this interface to alter a concept's DSL syntax
	/// by ignoring some of its properties when parsing a DSL script.
    /// The ignored properties (nonparsable) must be initialized manually in the InitializeNonparsableProperties function.
    /// </summary>
    public interface IAlternativeInitializationConcept : IConceptInfo
    {
        /// <summary>
        /// The function should return the names of public member properties that will be ignored when parsing a DSL script.
        /// The properties should be initialized in InitializeNonparsableProperties() function.
        /// The list may contain properties inherited from a base class.
        /// </summary>
        IEnumerable<string> DeclareNonparsableProperties();

        /// <summary>
        /// The function may create new concepts referenced by NonparsableMembers, similar to IMacroConcept.CreateNewConcepts.
        /// </summary>
        /// <param name="createdConcepts">
        /// Created new concepts referenced by non-parsable members, set to null otherwise.
        /// Create only those concepts that could be created by IMacroConcept, but are necessary to initilize NonparsableMembers members.
        /// </param>
        /// <remarks>
        /// Member references to other concepts are not resolved before this function is called. This means that inside this function,
        /// when using the member properties that reference other concepts, only key properties of the other concepts and base concept
        /// types are available. An exception to this rule is a reference to parent concept, but only when using embedded syntax in the DSL script.
        /// </remarks>
        void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts);
    }
}
