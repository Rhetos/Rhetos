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
using System.Globalization;
using Rhetos.Utilities;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using System.Text;

namespace Rhetos.DatabaseGenerator
{
    public class ConceptApplicationRepository
    {
        private readonly ISqlExecuter _sqlExecuter;
        private readonly ILogger _logger;

        public ConceptApplicationRepository(
            ISqlExecuter sqlExecuter,
            ILogProvider logProvider)
        {
            _sqlExecuter = sqlExecuter;
            _logger = logProvider.GetLogger("ConceptApplicationRepository");
        }

        public List<ConceptApplication> Load()
        {
            var previoslyAppliedConcepts = LoadOldConceptApplicationsFromDatabase();
            CheckKeyUniqueness(previoslyAppliedConcepts, "loaded");

            var dependencies = LoadDependenciesFromDatabase();
            EvaluateDependencies(previoslyAppliedConcepts, dependencies); // Replace guids with actual ConceptApplication instances.

            return previoslyAppliedConcepts;
        }

        private List<ConceptApplication> LoadOldConceptApplicationsFromDatabase()
        {
            var previoslyAppliedConcepts = new List<ConceptApplication>();

            _sqlExecuter.ExecuteReader(
                "SELECT ID, InfoType, ConceptInfoKey, ImplementationType, CreateQuery, RemoveQuery, ModificationOrder FROM Rhetos.AppliedConcept ORDER BY ModificationOrder",
                dataReader =>
                {
                    previoslyAppliedConcepts.Add(new ConceptApplication
                        {
                            Id = SqlUtility.ReadGuid(dataReader, 0),
                            ConceptInfoTypeName = dataReader.GetString(1),
                            ConceptInfoKey = dataReader.GetString(2),
                            ConceptImplementationTypeName = dataReader.GetString(3),
                            CreateQuery = SqlUtility.EmptyNullString(dataReader, 4),
                            RemoveQuery = SqlUtility.EmptyNullString(dataReader, 5),
                            OldCreationOrder = SqlUtility.ReadInt(dataReader, 6)
                        });
                });

            var invalidCa = previoslyAppliedConcepts.FirstOrDefault(ca => ca.ConceptInfoKey == ObsoleteConceptApplicationMark);
            if (invalidCa != null)
                throw new FrameworkException("Obsolete concept application loaded from database (Rhetos.ConceptApplication, ID = " + SqlUtility.GuidToString(invalidCa.Id)
                    + "). Upgrade procedure for old version of the system should include deployment with empty DslScritps folder (using old version of Rhetos server and packages) to remove old database structure and keep the data.");

            return previoslyAppliedConcepts;
        }

        private const string ObsoleteConceptApplicationMark = "/*UNKNOWN*/";

        private List<Dependency> LoadDependenciesFromDatabase()
        {
            var dependencies = new List<Dependency>();
            _sqlExecuter.ExecuteReader(
                "SELECT DependentID, DependsOnID FROM Rhetos.AppliedConceptDependsOn",
                dataReader =>
                {
                    dependencies.Add(new Dependency
                    {
                        Dependent = SqlUtility.ReadGuid(dataReader, 0),
                        DependsOn = SqlUtility.ReadGuid(dataReader, 1),
                    });
                });
            return dependencies;
        }

        public static void CheckKeyUniqueness(IEnumerable<ConceptApplication> appliedConcepts, string errorContext)
        {
            var firstError = appliedConcepts.GroupBy(pca => pca.GetConceptApplicationKey()).Where(g => g.Count() > 1).FirstOrDefault();
            if (firstError != null)
                throw new FrameworkException(String.Format("More than one concept application with same key {2} ('{0}') loaded in repository. Concept application IDs: {1}.",
                    firstError.Key, string.Join(", ", firstError.Select(ca => SqlUtility.QuoteGuid(ca.Id))), errorContext));
        }

        private static void EvaluateDependencies(List<ConceptApplication> previoslyAppliedConcepts, List<Dependency> dependencies)
        {
            Dictionary<Guid, Guid[]> dependenciesByDependent = dependencies.GroupBy(d => d.Dependent).ToDictionary(g => g.Key, g => g.Select(item => item.DependsOn).ToArray());
            var conceptApplicationsById = previoslyAppliedConcepts.ToDictionary(ca => ca.Id);

            foreach (var dependent in previoslyAppliedConcepts)
            {
                Guid[] dependsOnIds;
                if (dependenciesByDependent.TryGetValue(dependent.Id, out dependsOnIds))
                    dependent.DependsOn = dependsOnIds.Select(
                        dependsOnId => conceptApplicationsById.GetValue(dependsOnId,
                            "Nonexistent dependency on Rhetos.AppliedConcept, ID={0}, referenced from table Rhetos.AppliedConceptDependsOn column DependsOn."))
                        .ToArray();
                else
                    dependent.DependsOn = new ConceptApplication[] { };
            }

            var invalidDependentId = dependenciesByDependent.Keys.Except(previoslyAppliedConcepts.Select(ca => ca.Id)).ToArray();
            if (invalidDependentId.Length > 0)
                throw new FrameworkException(string.Format("Nonexistent dependency on Rhetos.AppliedConcept, ID={0}, referenced from table Rhetos.AppliedConceptDependsOn column DependentID.",
                    invalidDependentId.First()));
        }

        private class Dependency { public Guid Dependent; public Guid DependsOn; }

        public static string DeleteAllMetadataSql()
        {
            return Sql.Get("ConceptApplicationRepository_DeleteAll");
        }

        public static string DeleteMetadataSql(ConceptApplication ca)
        {
            return Sql.Format("ConceptApplicationRepository_Delete", SqlUtility.QuoteGuid(ca.Id));
        }

        public static IEnumerable<string> InsertMetadataSql(NewConceptApplication ca)
        {
            var sql = new List<string>();

            sql.Add(Sql.Format("ConceptApplicationRepository_Insert",
                SqlUtility.QuoteGuid(ca.Id),
                SqlUtility.QuoteText(ca.ConceptInfoTypeName),
                SqlUtility.QuoteText(ca.ConceptInfoKey),
                SqlUtility.QuoteText(ca.ConceptImplementationTypeName),
                SqlUtility.QuoteText(XmlUtility.SerializeToXml(ca.ConceptInfo)),
                SqlUtility.QuoteText(ca.CreateQuery),
                SqlUtility.QuoteText(ca.RemoveQuery),
                SqlUtility.QuoteText(ca.ConceptImplementationVersion.ToString())));

            foreach (var dependsOn in ca.DependsOn)
                sql.Add(Sql.Format("ConceptApplicationRepository_InsertDependency",
                    SqlUtility.QuoteGuid(ca.Id),
                    SqlUtility.QuoteGuid(dependsOn.Id)));

            return sql;
        }

        public static List<string> UpdateMetadataSql(NewConceptApplication ca, ConceptApplication oldApp)
        {
            var sql = new List<string>();
            if (oldApp.RemoveQuery != ca.RemoveQuery)
                sql.Add(Sql.Format("ConceptApplicationRepository_Update",
                    SqlUtility.QuoteGuid(ca.Id),
                    SqlUtility.QuoteText(ca.ConceptInfoTypeName),
                    SqlUtility.QuoteText(ca.ConceptInfoKey),
                    SqlUtility.QuoteText(ca.ConceptImplementationTypeName),
                    SqlUtility.QuoteText(XmlUtility.SerializeToXml(ca.ConceptInfo)),
                    SqlUtility.QuoteText(ca.CreateQuery),
                    SqlUtility.QuoteText(ca.RemoveQuery),
                    SqlUtility.QuoteText(ca.ConceptImplementationVersion.ToString())));

            HashSet<Guid> oldDependsOn = new HashSet<Guid>(oldApp.DependsOn.Select(depOn => depOn.Id));
            HashSet<Guid> newDependsOn = new HashSet<Guid>(ca.DependsOn.Select(depOn => depOn.Id));

            foreach (var dependsOn in ca.DependsOn)
                if (!oldDependsOn.Contains(dependsOn.Id))
                    sql.Add(Sql.Format("ConceptApplicationRepository_InsertDependency",
                        SqlUtility.QuoteGuid(ca.Id),
                        SqlUtility.QuoteGuid(dependsOn.Id)));

            foreach (var dependsOn in oldApp.DependsOn)
                if (!newDependsOn.Contains(dependsOn.Id))
                    sql.Add(Sql.Format("ConceptApplicationRepository_DeleteDependency",
                        SqlUtility.QuoteGuid(ca.Id),
                        SqlUtility.QuoteGuid(dependsOn.Id)));

            return sql;
        }
    }
}
