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
using System.Text;

namespace Rhetos.Dsl
{
    [DebuggerDisplay("{Concept.TypeName}")]
    public class ConceptSyntaxNode
    {
        public ConceptType Concept { get; }

        /// <summary>
        /// Elements are either <see cref="ConceptSyntaxNode"/> or <see cref="string"/>.
        /// </summary>
        public object[] Parameters { get; }

        public ConceptSyntaxNode(ConceptType concept)
        {
            Concept = concept;
            Parameters = new object[concept.Members.Count];
        }

        /// <summary>
        /// Returns a string that describes the concept instance in a user-friendly manner.
        /// The string contains concept's keyword and a list of concept's key properties.
        /// </summary>
        /// <remarks>
        /// This description in not unique because different concepts might have same keyword.
        /// </remarks>
        public string GetUserDescription()
        {
            var desc = new StringBuilder(100);
            desc.Append(Concept.GetKeywordOrTypeName());
            desc.Append(" ");
            AppendKeyMembers(desc);
            return desc.ToString();
        }

        private void AppendKeyMembers(StringBuilder text)
        {
            var members = Concept.Members;
            bool firstMember = true;
            for (int m = 0; m < members.Count; m++)
            {
                var member = members[m];
                if (member.IsKey)
                {
                    if (!firstMember)
                        text.Append(".");
                    firstMember = false;

                    AppendMember(text, member);
                }
            }
        }

        private void AppendMember(StringBuilder text, ConceptMemberSyntax member)
        {
            object memberValue = member.GetMemberValue(this);

            if (memberValue == null)
                text.Append("<null>");
            else if (member.IsConceptInfo)
            {
                var value = (ConceptSyntaxNode)memberValue;
                if (member.IsConceptInfoInterface)
                    text.Append(value.Concept.GetRootTypeName()).Append(":");
                value.AppendKeyMembers(text);
            }
            else if (member.IsStringType)
                ConceptMemberHelper.AppendWithQuotesIfNeeded(text, (string)memberValue);
            else
                throw new FrameworkException(
                    $"{nameof(ConceptSyntaxNode)} member {member.Name} of type {member.ConceptType?.TypeName} in {Concept.TypeName} is not supported.");
        }
    }
}
