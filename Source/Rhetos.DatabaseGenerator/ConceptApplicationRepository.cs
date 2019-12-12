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
    public class ConceptApplicationRepository : IConceptApplicationRepository
    {
        private readonly ISqlExecuter _sqlExecuter;

        public ConceptApplicationRepository(
            ISqlExecuter sqlExecuter)
        {
            _sqlExecuter = sqlExecuter;
        }

        public List<ConceptApplication> Load()
        {
            var previoslyAppliedConcepts = LoadOldConceptApplicationsFromDatabase();
            ConceptApplication.CheckKeyUniqueness(previoslyAppliedConcepts, "loaded");

            var dependencies = LoadDependenciesFromDatabase();
            EvaluateDependencies(previoslyAppliedConcepts, dependencies); // Replace GUIDs with actual ConceptApplication instances.

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

        private List<DependencyGuids> LoadDependenciesFromDatabase()
        {
            var dependencies = new List<DependencyGuids>();
            _sqlExecuter.ExecuteReader(
                "SELECT DependentID, DependsOnID FROM Rhetos.AppliedConceptDependsOn",
                dataReader =>
                {
                    dependencies.Add(new DependencyGuids
                    {
                        Dependent = SqlUtility.ReadGuid(dataReader, 0),
                        DependsOn = SqlUtility.ReadGuid(dataReader, 1),
                    });
                });
            return dependencies;
        }

        private static void EvaluateDependencies(List<ConceptApplication> previoslyAppliedConcepts, List<DependencyGuids> dependencies)
        {
            var dependenciesByDependent = dependencies.ToMultiDictionary(d => d.Dependent, d => d.DependsOn);
            var conceptApplicationsById = previoslyAppliedConcepts.ToDictionary(ca => ca.Id);

            foreach (var dependent in previoslyAppliedConcepts)
            {
                var dependsOns = dependenciesByDependent.GetValueOrDefault(dependent.Id);
                if (dependsOns != null)
                    dependent.DependsOn = dependsOns
                        .Select(dependsOn => conceptApplicationsById.GetValue(dependsOn,
                            "Nonexistent dependency on Rhetos.AppliedConcept, ID={0}, referenced from table Rhetos.AppliedConceptDependsOn column DependsOn."))
                        .ToArray();
                else
                    dependent.DependsOn = Array.Empty<ConceptApplication>();
            }

            var invalidDependentId = dependenciesByDependent.Keys.Except(previoslyAppliedConcepts.Select(ca => ca.Id)).ToArray();
            if (invalidDependentId.Length > 0)
                throw new FrameworkException(string.Format("Nonexistent dependency on Rhetos.AppliedConcept, ID={0}, referenced from table Rhetos.AppliedConceptDependsOn column DependentID.",
                    invalidDependentId.First()));
        }

        private class DependencyGuids
        {
            public Guid Dependent;
            public Guid DependsOn;
        }

        public static string DeleteAllMetadataSql()
        {
            return Sql.Get("ConceptApplicationRepository_DeleteAll");
        }

        public List<string> DeleteMetadataSql(ConceptApplication oldCA)
        {
            return new List<string>
            {
                Sql.Format("ConceptApplicationRepository_Delete", SqlUtility.QuoteGuid(oldCA.Id))
            };
        }

        public List<string> InsertMetadataSql(ConceptApplication newCA)
        {
            var sql = new List<string>();

            sql.Add(Sql.Format("ConceptApplicationRepository_Insert",
                SqlUtility.QuoteGuid(newCA.Id),
                SqlUtility.QuoteText(newCA.ConceptInfoTypeName),
                SqlUtility.QuoteText(newCA.ConceptInfoKey),
                SqlUtility.QuoteText(newCA.ConceptImplementationTypeName),
                SqlUtility.QuoteText(newCA.CreateQuery),
                SqlUtility.QuoteText(newCA.RemoveQuery)));

            foreach (var dependsOnId in newCA.DependsOn.Select(d => d.Id).Distinct())
                sql.Add(Sql.Format("ConceptApplicationRepository_InsertDependency",
                    SqlUtility.QuoteGuid(newCA.Id),
                    SqlUtility.QuoteGuid(dependsOnId)));

            return sql;
        }

        public List<string> UpdateMetadataSql(ConceptApplication newCA, ConceptApplication oldApp)
        {
            var sql = new List<string>();
            if (oldApp.RemoveQuery != newCA.RemoveQuery)
                sql.Add(Sql.Format("ConceptApplicationRepository_Update",
                    SqlUtility.QuoteGuid(newCA.Id),
                    SqlUtility.QuoteText(newCA.ConceptInfoTypeName),
                    SqlUtility.QuoteText(newCA.ConceptInfoKey),
                    SqlUtility.QuoteText(newCA.ConceptImplementationTypeName),
                    SqlUtility.QuoteText(newCA.CreateQuery),
                    SqlUtility.QuoteText(newCA.RemoveQuery)));

            HashSet<Guid> oldDependsOn = new HashSet<Guid>(oldApp.DependsOn.Select(depOn => depOn.Id));
            HashSet<Guid> newDependsOn = new HashSet<Guid>(newCA.DependsOn.Select(depOn => depOn.Id));

            foreach (var dependsOnId in newCA.DependsOn.Select(d => d.Id).Distinct())
                if (!oldDependsOn.Contains(dependsOnId))
                    sql.Add(Sql.Format("ConceptApplicationRepository_InsertDependency",
                        SqlUtility.QuoteGuid(newCA.Id),
                        SqlUtility.QuoteGuid(dependsOnId)));

            foreach (var dependsOnId in oldApp.DependsOn.Select(d => d.Id).Distinct())
                if (!newDependsOn.Contains(dependsOnId))
                    sql.Add(Sql.Format("ConceptApplicationRepository_DeleteDependency",
                        SqlUtility.QuoteGuid(newCA.Id),
                        SqlUtility.QuoteGuid(dependsOnId)));

            return sql;
        }
    }
}
