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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Dsl
{
    /// <summary>
    /// Represents a distinct concept type (implementation of <see cref="IConceptInfo"/>),
    /// and describes the DSL syntax of the concept.
    /// </summary>
    /// <remarks>
    /// It provides separation from <see cref="IConceptInfo"/> types, to allow serialization
    /// of DSL syntax and use of the syntax in external DSL analysis, such as DSL IntelliSense plugin.
    /// </remarks>
    [DebuggerDisplay("{TypeName}")]
    public class ConceptType
    {
        /// <summary>
        /// <see cref="Type.AssemblyQualifiedName"/> of <see cref="IConceptInfo"/> implementation class for this concept.
        /// </summary>
        public string AssemblyQualifiedName { get; set; }

        /// <summary>
        /// Base concept types, excluding the current concept type itself.
        /// The array is sorted by inheritance depth, each element is a derivation of the previous one.
        /// </summary>
        public List<ConceptType> BaseTypes { get; set; }

        /// <summary>
        /// Short type name of the root concept type, used for unique concept key definition.
        /// </summary>
        /// <remarks>
        /// It is same as <see cref="TypeName"/> if the <see cref="IConceptInfo"/> implementation is not a derivation of another concept.
        /// </remarks>
        public string GetRootTypeName() => (BaseTypes.FirstOrDefault() ?? this).TypeName;

        /// <summary>
        /// Short type name of <see cref="IConceptInfo"/> implementation class for this concept.
        /// </summary>
        public string TypeName { get; set; }

        public string Keyword { get; set; }

        public List<ConceptMemberSyntax> Members { get; set; }

        public string GetKeywordOrTypeName() => Keyword ?? TypeName;

        /// <summary>
        /// Determines whether the specified syntax node is an instance of the current (base property) concept type.
        /// </summary>
        /// <remarks>
        /// Before calling this method, check if the base property has <see cref="ConceptMemberSyntax.ConceptType"/> set,
        /// to avoid null reference exception.
        /// In case of the null value, consider including the base properties with <see cref="ConceptMemberBase.IsConceptInfoInterface"/> set,
        /// because any derived concept type can also be assigned to the base property of type <see cref="IConceptInfo"/>.
        /// </remarks>
        public bool IsInstanceOfType(ConceptSyntaxNode derivedTypeNode)
        {
            return IsAssignableFrom(derivedTypeNode.Concept);
        }

        /// <summary>
        /// Determines whether an instance of a specified concept type can be referenced by a base property of the current concept type.
        /// </summary>
        /// <remarks>
        /// Before calling this method, check if the base property has <see cref="ConceptMemberSyntax.ConceptType"/> set,
        /// to avoid null reference exception.
        /// In case of the null value, consider including the base properties with <see cref="ConceptMemberBase.IsConceptInfoInterface"/> set,
        /// because any derived concept type can also be assigned to the base property of type <see cref="IConceptInfo"/>.
        /// </remarks>
        public bool IsAssignableFrom(ConceptType derivedConceptType)
        {
            return derivedConceptType == this || derivedConceptType.BaseTypes.Contains(this);
        }
    }
}
