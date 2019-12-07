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

namespace Rhetos.DatabaseGenerator
{
    public class ConceptApplication
    {
        public Guid Id;

        public string ConceptInfoTypeName;
        public string ConceptInfoKey;
        public string ConceptImplementationTypeName;

        public string CreateQuery; // SQL query that creates the concept in database.
        public string RemoveQuery; // SQL query that removes the concept from database.
        public ConceptApplicationDependency[] DependsOn;
        public int OldCreationOrder;

        private string _conceptApplicationKey;
        public string GetConceptApplicationKey()
        {
            if (_conceptApplicationKey == null)
                _conceptApplicationKey = ConceptInfoKey + "/" + GetTypeNameWithoutVersion(ConceptImplementationTypeName);
            return _conceptApplicationKey;
        }

        private string _toString;
        public override string ToString()
        {
            if (_toString == null)
                _toString = ConceptInfoKey + "/" + GetUserFriendlyTypeName(ConceptImplementationTypeName);
            return _toString;
        }

        private static string GetTypeNameWithoutVersion(string assemblyQualifiedName)
        {
            return assemblyQualifiedName.Substring(0, assemblyQualifiedName.IndexOf(','));
        }

        private static string GetUserFriendlyTypeName(string assemblyQualifiedName)
        {
            var fullTypeName = GetTypeNameWithoutVersion(assemblyQualifiedName);
            return fullTypeName.Substring(fullTypeName.LastIndexOf('.') + 1); // Works even for type without namespace.
        }

        public static List<Tuple<ConceptApplication, ConceptApplication>> GetDependencyPairs(IEnumerable<ConceptApplication> conceptApplications)
        {
            return conceptApplications
                .SelectMany(dependent => dependent.DependsOn
                    .Select(dependsOn => Tuple.Create(dependsOn.ConceptApplication, dependent)))
                .Where(dependency => dependency.Item1 != dependency.Item2)
                .ToList();
        }

        public static void CheckKeyUniqueness(IEnumerable<ConceptApplication> appliedConcepts, string errorContext)
        {
            var firstError = appliedConcepts.GroupBy(pca => pca.GetConceptApplicationKey()).Where(g => g.Count() > 1).FirstOrDefault();
            if (firstError != null)
                throw new FrameworkException(String.Format("More than one concept application with same key {2} ('{0}') loaded in repository. Concept application IDs: {1}.",
                    firstError.Key, string.Join(", ", firstError.Select(ca => SqlUtility.QuoteGuid(ca.Id))), errorContext));
        }
    }
}
