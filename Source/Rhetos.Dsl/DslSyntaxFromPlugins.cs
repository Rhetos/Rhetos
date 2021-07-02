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

namespace Rhetos.Dsl
{
    public class DslSyntaxFromPlugins
    {
        private readonly IEnumerable<IConceptInfo> _conceptInfoPlugins;
        private readonly BuildOptions _buildOptions;
        private readonly DatabaseSettings _databaseSettings;

        public DslSyntaxFromPlugins(IEnumerable<IConceptInfo> conceptInfoPlugins, BuildOptions buildOptions, DatabaseSettings databaseSettings)
        {
            _conceptInfoPlugins = conceptInfoPlugins;
            _buildOptions = buildOptions;
            _databaseSettings = databaseSettings;
        }

        public DslSyntax CreateDslSyntax()
        {
            return new DslSyntax
            {
                ConceptTypes = CreateConceptTypesAndMembers(_conceptInfoPlugins.Select(ci => ci.GetType())),
                Version = SystemUtility.GetRhetosVersion(),
                ExcessDotInKey = _buildOptions.DslSyntaxExcessDotInKey,
                DatabaseLanguage = _databaseSettings.DatabaseLanguage,
            };
        }

        private static List<ConceptType> CreateConceptTypesAndMembers(IEnumerable<Type> conceptInfoTypes)
        {
            IncludeBaseConceptTypes(ref conceptInfoTypes);

            var types = conceptInfoTypes
                .Distinct()
                .ToDictionary(
                    conceptInfoType => conceptInfoType,
                    conceptInfoType => CreateConceptTypePartial(conceptInfoType));

            foreach (var type in types)
            {
                type.Value.Members = ConceptMembers.Get(type.Key)
                    .Select(conceptMember =>
                    {
                        var memberSyntax = new ConceptMemberSyntax();
                        ConceptMemberBase.Copy(conceptMember, memberSyntax);
                        memberSyntax.ConceptType = memberSyntax.IsConceptInfo && !memberSyntax.IsConceptInfoInterface
                            ? types.GetValue(conceptMember.ValueType,
                                $"{nameof(DslSyntaxFromPlugins)} does not contain concept type '{conceptMember.ValueType}'," +
                                $" referenced by {type.Key}.{conceptMember.Name}.")
                            : null;
                        return memberSyntax;
                    })
                    .ToList();

                type.Value.BaseTypes = GetBaseConceptInfoTypes(type.Key)
                    .Select(baseType => types[baseType])
                    .ToList();
            }

            return types.Values.OrderBy(conceptType => conceptType.AssemblyQualifiedName).ToList();
        }

        /// <summary>
        /// Abstract base concept classes cannot be resolved from DI as IEnumerable of IConceptInfo instances,
        /// so all base classes are automatically added here even if they are not registered directly as an IConceptInfo plugin.
        /// </summary>
        private static void IncludeBaseConceptTypes(ref IEnumerable<Type> conceptInfoTypes)
        {
            var conceptInfoTypesIndex = new HashSet<Type>(conceptInfoTypes);
            foreach (Type conceptInfoType in conceptInfoTypes)
                foreach (Type baseType in GetBaseConceptInfoTypes(conceptInfoType))
                    conceptInfoTypesIndex.Add(baseType);
            conceptInfoTypes = conceptInfoTypesIndex;
        }

        private static ConceptType CreateConceptTypePartial(Type conceptInfoType)
        {
            if (!typeof(IConceptInfo).IsAssignableFrom(conceptInfoType))
                throw new ArgumentException($"Type '{conceptInfoType}' is not an implementation of '{typeof(IConceptInfo)}'.");
            if (typeof(IConceptInfo) == conceptInfoType)
                throw new ArgumentException($"{nameof(ConceptType)} cannot be created from {nameof(IConceptInfo)} interface. An implementation class is required.");

            return new ConceptType
            {
                AssemblyQualifiedName = conceptInfoType.AssemblyQualifiedName,
                BaseTypes = null, // Will be set later, because it needs to reference other ConceptTypes, after all are created.
                TypeName = conceptInfoType.Name,
                Keyword = ConceptInfoHelper.GetKeyword(conceptInfoType),
                Members = null, // Will be set later, to avoid recursive dependencies when creating these objects.
            };
        }

        public static List<Type> GetBaseConceptInfoTypes(Type t)
        {
            var baseTypes = new List<Type>();

            while (true)
            {
                Type baseType = t.BaseType;
                if (typeof(IConceptInfo).IsAssignableFrom(baseType))
                {
                    baseTypes.Add(baseType);
                    t = baseType;
                }
                else
                    break;
            }

            baseTypes.Reverse();
            return baseTypes;
        }
    }
}