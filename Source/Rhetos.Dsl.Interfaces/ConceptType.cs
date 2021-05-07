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
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Dsl
{
    /// <summary>
    /// Represents a distinct concept type (implementation of <see cref="IConceptInfo"/>),
    /// and describes the DSL syntax of the concept.
    /// </summary>
    /// <remarks>
    /// It provides separation from <see cref="IConceptInfo"/> types, to allows serialization
    /// of DSL grammar and use of the grammar in external processes, such as DSL IntelliSense plugin.
    /// </remarks>
    [DebuggerDisplay("{TypeName}")]
    public class ConceptType
    {
        /// <summary>
        /// <see cref="Type.AssemblyQualifiedName"/> of <see cref="IConceptInfo"/> implementation class for this concept.
        /// </summary>
        public string AssemblyQualifiedName { get; set; }

        /// <summary>
        /// Base types, excluding the current concept type itself (<see cref="AssemblyQualifiedName"/>).
        /// The array is sorted by inheritance depth, each element is a derivation of the previous one.
        /// </summary>
        public string[] BaseTypesAssemblyQualifiedName { get; set; }

        /// <summary>
        /// Short type name of the root concept type, used for unique concept key definition.
        /// It is same as <see cref="TypeName"/> if the <see cref="IConceptInfo"/> implementation is not a derivation of another concept.
        /// </summary>
        public string BaseTypeName { get; set; }

        /// <summary>
        /// Short type name of <see cref="IConceptInfo"/> implementation class for this concept.
        /// </summary>
        public string TypeName { get; set; }

        public string Keyword { get; set; }

        public ConceptMemberSyntax[] Members { get; set; }

        public string KeywordOrTypeName => Keyword ?? TypeName;

        public bool IsInstanceOfType(ConceptSyntaxNode derivedTypeNode)
        {
            return
                string.Equals(derivedTypeNode.Concept.AssemblyQualifiedName, AssemblyQualifiedName)
                || derivedTypeNode.Concept.BaseTypesAssemblyQualifiedName.Contains(AssemblyQualifiedName, StringComparer.Ordinal);
        }
    }
}
