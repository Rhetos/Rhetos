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
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;

namespace Rhetos.Dsl
{
    public static class ConceptMembers
    {
        private static readonly Dictionary<Type, ConceptMember[]> _cache = new Dictionary<Type, ConceptMember[]>();

        public static ConceptMember[] Get(IConceptInfo conceptInfo)
        {
            return Get(conceptInfo.GetType(), conceptInfo);
        }

        public static ConceptMember[] Get(Type conceptInfoType)
        {
            return Get(conceptInfoType, null);
        }

        private static ConceptMember[] Get(Type conceptInfoType, IConceptInfo instance)
        {
            ConceptMember[] cached;
            if (_cache.TryGetValue(conceptInfoType, out cached))
                return cached;

            HashSet<string> nonParsableMembers = null;
            if (typeof(IAlternativeInitializationConcept).IsAssignableFrom(conceptInfoType))
            {
                var alternativeInitializationConcept = instance != null
                    ? (IAlternativeInitializationConcept)instance
                    : (IAlternativeInitializationConcept)CreateInstanceEx(conceptInfoType);
                nonParsableMembers = new HashSet<string>(alternativeInitializationConcept.DeclareNonparsableProperties());
            }

            var conceptMembers = conceptInfoType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite)
                .Select(memberInfo => new ConceptMember(memberInfo, nonParsableMembers))
                .ToArray();

            if (conceptInfoType.GetFields(BindingFlags.Instance | BindingFlags.Public).Length > 0)
                throw new FrameworkException(
                    string.Format(CultureInfo.InvariantCulture,
                        "IConceptInfo does not support public fields. Use public properties instead. Class: \"{0}\".",
                            conceptInfoType.Name));

            Array.Sort(conceptMembers, (a, b) =>
                {
                    int diff = a.SortOrder1 - b.SortOrder1;
                    if (diff != 0)
                        return diff;
                    return a.SortOrder2 - b.SortOrder2;
                });

            for (int i = 0; i < conceptMembers.Length; i++)
                conceptMembers[i].Index = i;

            if (!conceptMembers.Any(m => m.IsKey))
                throw new FrameworkException(
                    string.Format(CultureInfo.InvariantCulture,
                        "One or more members of concept-info class must have ConceptKey attribute. Class: \"{0}\".",
                            conceptInfoType.Name));

            if (typeof(IConceptInfo).IsAssignableFrom(conceptInfoType.BaseType) && conceptInfoType.BaseType.IsClass)
            {
                string derivedKeyMember = conceptMembers.Where(m => m.IsKey && !m.IsDerived).Select(m => m.Name).FirstOrDefault();
                if (derivedKeyMember != null)
                    throw new FrameworkException(string.Format(
                        "Derived concept must not contain members with ConceptKey attribute. Class: {0}, member: {1}.",
                        conceptInfoType.Name, derivedKeyMember));
            }

            if (nonParsableMembers != null)
            {
                string nonexistentMember = nonParsableMembers.Except(conceptMembers.Select(m => m.Name)).FirstOrDefault();
                if (nonexistentMember != null)
                    throw new FrameworkException(string.Format(
                        "Invalid implementation of the concept info function {0}.DeclareNonparsableProperties: it returned a property name '{1}' that does not exist.",
                        conceptInfoType.Name, nonexistentMember));
            }

            _cache.Add(conceptInfoType, conceptMembers);
            return conceptMembers;
        }

        private static object CreateInstanceEx(Type conceptInfoType)
        {
            try
            {
                return Activator.CreateInstance(conceptInfoType);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot create instance of {conceptInfoType}.", e);
            }
        }
    }
}
