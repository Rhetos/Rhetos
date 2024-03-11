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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl.Test
{
    public static class DslSyntaxHelper
    {
        public static ConceptSyntaxNode CreateConceptSyntaxNode(this DslSyntax dslSyntax, IConceptInfo ci)
        {
            var conceptInfoType = ci.GetType();
            var node = new ConceptSyntaxNode(dslSyntax.GetConceptType(conceptInfoType));
            var members = ConceptMembers.Get(conceptInfoType);

            if (node.Parameters.Length != members.Length)
                throw new InvalidOperationException(
                    $"{nameof(ConceptSyntaxNode)} parameters count ({node.Parameters.Length})" +
                    $" does not match {nameof(ConceptMembers)} count ({members.Length}).");

            for (int m = 0; m < members.Length; m++)
            {
                object value = members[m].GetValue(ci);

                node.Parameters[m] = value;

                if (value == null)
                    node.Parameters[m] = null;
                else if (value is string)
                    node.Parameters[m] = value;
                else if (value is IConceptInfo referencedConceptInfo)
                {
                    var referencedNode = CreateConceptSyntaxNode(dslSyntax, referencedConceptInfo);
                    node.Parameters[m] = referencedNode;
                }
                else
                {
                    throw new ArgumentException($"Value type {value.GetType()} is not expected in '{ci.GetUserDescription()}', parameter {members[m].Name}.");
                }
            }

            return node;
        }

        public static ConceptType GetConceptType(this DslSyntax dslSyntax, Type conceptInfoType, string overrideKeyword = null)
        {
            var conceptType = dslSyntax.ConceptTypes.Single(ct => ct.AssemblyQualifiedName == conceptInfoType.AssemblyQualifiedName);
            if (overrideKeyword != null)
                conceptType.Keyword = overrideKeyword;
            return conceptType;
        }

        /// <summary>
        /// The syntax will also include all related concepts, referenced by the provided concept's members.
        /// </summary>
        public static DslSyntax CreateDslSyntax(params IConceptInfo[] conceptInfos)
            => CreateDslSyntax(conceptInfos.Select(ci => ci.GetType()).ToArray());

        /// <summary>
        /// The syntax will also include all related concepts, referenced by the provided concept's members.
        /// </summary>
        public static DslSyntax CreateDslSyntax(params Type[] conceptInfoTypes)
        {
            var relatedConcepts = GetAllRelatedConceptInfoTypes(conceptInfoTypes);
            var relatedConceptPrototypes = relatedConcepts.Select(c => (IConceptInfo)Activator.CreateInstance(c)).ToList();
            var syntax = new DslSyntaxFromPlugins(relatedConceptPrototypes, new BuildOptions(), new DatabaseSettings());
            return syntax.CreateDslSyntax();
        }

        public static IEnumerable<Type> GetAllRelatedConceptInfoTypes(params Type[] conceptInfoTypes)
        {
            var types = new HashSet<Type>(conceptInfoTypes);
            var addReferencedTypes = new HashSet<Type>(conceptInfoTypes);

            while (addReferencedTypes.Count != 0)
            {
                var newTypes = new HashSet<Type>();

                foreach (Type type in addReferencedTypes)
                    foreach (var member in ConceptMembers.Get(type))
                        if (member.IsConceptInfo && !member.IsConceptInfoInterface)
                            if (!types.Contains(member.ValueType))
                            {
                                types.Add(member.ValueType);
                                newTypes.Add(member.ValueType);
                            }

                addReferencedTypes = newTypes;
            }

            foreach (var type in types.ToList()) // Creating a copy to allow modification within the loop.
                foreach (var baseType in DslSyntaxFromPlugins.GetBaseConceptInfoTypes(type))
                    types.Add(baseType);

            return types.ToArray();
        }
    }
}
