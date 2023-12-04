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

namespace Rhetos.Dsl
{
    public class ConceptMemberSyntax : ConceptMemberBase
    {
        /// <summary>
        /// If a member references a concrete concept, this property describes type of that concept.
        /// It is set only if <see cref="ConceptMemberBase.IsConceptInfo"/> is true and <see cref="ConceptMemberBase.IsConceptInfoInterface"/> is false, null otherwise.
        /// </summary>
        public ConceptType ConceptType { get; set; }

        public void SetMemberValue(ConceptSyntaxNode node, object value)
        {
            node.Parameters[Index] = value;
        }

        public object GetMemberValue(ConceptSyntaxNode node)
        {
            return node.Parameters[Index];
        }
    }
}
