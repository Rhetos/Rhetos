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
    /// <summary>
    /// A wrapper around a DatabaseObject, for handling previously deployed database objects.
    /// </summary>
    public class ConceptApplication : DatabaseObject
    {
        /// <summary>
        /// The Id will be the same for the matching pairs of newly generated objects and old ones from the existing database that is being updated.
        /// It simplifies some optimizations and also simplifies loading and saving database objects metadata, especially DependsOn list.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// Ordering of the previously created database objects. The opposite ordering will be used for removing the objects.
        /// </summary>
        public int OldCreationOrder;

        public ConceptApplication()
        {
        }

        public ConceptApplication(DatabaseObject databaseObject)
        {
            ConceptInfoTypeName = databaseObject.ConceptInfoTypeName;
            ConceptInfoKey = databaseObject.ConceptInfoKey;
            ConceptImplementationTypeName = databaseObject.ConceptImplementationTypeName;
            CreateQuery = databaseObject.CreateQuery;
            RemoveQuery = databaseObject.RemoveQuery;
            DependsOn = null; // Dependencies will be set later to reference the new concept applications.
        }

        public IEnumerable<ConceptApplication> DependsOnConceptApplications => DependsOn.Cast<ConceptApplication>();

        public static List<ConceptApplication> FromDatabaseObjects(IList<DatabaseObject> databaseObjects)
        {
            var conceptApplications = databaseObjects.Select(o => new ConceptApplication(o)).ToList();

            var pairs = databaseObjects.Zip(conceptApplications, (databaseObject, conceptApplication) => new { databaseObject, conceptApplication });
            var applicationByObject = pairs.ToDictionary(pair => pair.databaseObject, pair => pair.conceptApplication);
            foreach (var pair in pairs)
                pair.conceptApplication.DependsOn = pair.databaseObject.DependsOn.Select(dbObject => applicationByObject[dbObject]).ToArray();

            return conceptApplications;
        }

        private string _conceptApplicationKey;

        /// <summary>
        /// Used for matching the newly generated objects with the old ones from the existing database that is being updated.
        /// </summary>
        public string GetConceptApplicationKey()
        {
            if (_conceptApplicationKey == null)
                _conceptApplicationKey = ConceptInfoKey + "/" + GetConceptImplementationType_FullName();
            return _conceptApplicationKey;
        }

        public static List<Tuple<ConceptApplication, ConceptApplication>> GetDependencyPairs(IEnumerable<ConceptApplication> conceptApplications)
        {
            return conceptApplications
                .SelectMany(dependent => dependent.DependsOnConceptApplications
                    .Select(dependsOn => Tuple.Create(dependsOn, dependent)))
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
