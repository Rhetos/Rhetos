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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dsl;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.DatabaseGenerator.Test
{
    [TestClass]
    public partial class DatabaseGeneratorTest
    {
        public DatabaseGeneratorTest()
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();
            LegacyUtilities.Initialize(configurationProvider);
        }

        /*
        #region Helper classes

        private class SimpleConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "create " + ((BaseCi)conceptInfo).Name; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return "remove " + ((BaseCi)conceptInfo).Name; }
        }

        private class DependentConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "CREATE DependentConceptImplementation"; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        }

        private class NoTransactionConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return SqlUtility.NoTransactionTag + "create " + ((BaseCi)conceptInfo).Name; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return SqlUtility.NoTransactionTag + "remove " + ((BaseCi)conceptInfo).Name; }
        }

        private class BaseCi : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        private static CodeGenerator CreateBaseCiApplication(string name, IConceptDatabaseDefinition implementation = null)
        {
            implementation = implementation ?? new SimpleConceptImplementation();
            var conceptInfo = new BaseCi { Name = name };
            return new CodeGenerator(conceptInfo, implementation)
            {
                CreateQuery = implementation.CreateDatabaseStructure(conceptInfo),
                RemoveQuery = implementation.RemoveDatabaseStructure(conceptInfo),
                DependsOn = new ConceptApplicationDependency[] { },
                ConceptImplementationType = implementation.GetType(),
            };
        }

        private class SimpleCi : BaseCi
        {
            public string Data { get; set; }
        }

        private static CodeGenerator CreateSimpleCiApplication(string name, string sql = null, string data = null, ConceptApplication dependsOn = null, IConceptDatabaseDefinition implementation = null)
        {
            return new CodeGenerator(new SimpleCi { Name = name, Data = data ?? $"{name}Data" }, implementation ?? new SimpleConceptImplementation())
            {
                CreateQuery = sql ?? $"{name}Sql",
                DependsOn = dependsOn == null ? new ConceptApplicationDependency[] { } : new[] { new ConceptApplicationDependency { ConceptApplication = dependsOn } }
            };
        }

        private class ReferencingCi : IConceptInfo
        {
            [ConceptKey]
            public SimpleCi Reference { get; set; }
            public string Data { get; set; }

            public static CodeGenerator CreateApplication(CodeGenerator reference, string sql = null)
            {
                return new CodeGenerator(
                    new ReferencingCi { Reference = (SimpleCi)(reference.ConceptInfo), Data = "data" },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = sql ?? "dependentSql",
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }
        }

        private class ReferenceToReferencingCi : IConceptInfo
        {
            [ConceptKey]
            public ReferencingCi Reference { get; set; }
            public string Data { get; set; }

            public static CodeGenerator CreateApplication(string sql, CodeGenerator reference)
            {
                return new CodeGenerator(
                    new ReferenceToReferencingCi { Reference = (ReferencingCi)(reference.ConceptInfo), Data = "data" },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }
        }

        #endregion Helper classes
        */
        #region Helper methods
        /*
    /// <summary>
    /// Report contains created and removed concept applications in the mock database.
    /// It contains metadata changes (ins, upd, del) and object creation scripts.
    /// </summary>
    private
        (string Report,
        MockSqlExecuter SqlExecuter,
        List<ConceptApplication> RemovedConcepts,
        List<ConceptApplication> InsertedConcepts)
        DatabaseGeneratorUpdateDatabase(
            IEnumerable<CodeGenerator> oldCodeGenerators,
            IEnumerable<CodeGenerator> newCodeGenerators)
    {
        // Create concept applications from code generators:

        var duplicate = oldCodeGenerators.Cast<CodeGenerator>().Intersect(newCodeGenerators).FirstOrDefault();
        if (duplicate != null)
            Assert.Fail($"Incorrect test input data: newApplications contains an instance from oldApplications: {duplicate}.");

        var oldConceptApplications = CreateConceptApplications(oldCodeGenerators);
        var newConceptApplications = CreateConceptApplications(newCodeGenerators);

        // Update mock database (based on difference between old and new concept applications):

        var conceptApplicationRepository = new MockConceptApplicationRepository { ConceptApplications = oldConceptApplications };
        var databaseModel = new DatabaseModel { ConceptApplications = newConceptApplications };
        var testConfig = new MockConfiguration { { "SqlExecuter.MaxJoinedScriptCount", 1 } };
        var sqlExecuter = new MockSqlExecuter();
        var sqlTransactionBatches = new SqlTransactionBatches(sqlExecuter, testConfig, new ConsoleLogProvider());

        IDatabaseGenerator databaseGenerator = new DatabaseGenerator(
            sqlTransactionBatches,
            conceptApplicationRepository,
            new ConsoleLogProvider(),
            new DatabaseGeneratorOptions { ShortTransactions = false },
            databaseModel);

        databaseGenerator.UpdateDatabaseStructure();

        // Report changes in mock database:

        TestUtility.Dump(
            sqlExecuter.ExecutedScriptsWithTransaction,
            script => (script.Item2 ? "tran" : "notran")
                + string.Concat(script.Item1.Select(sql => "\r\n  - " + sql.Replace('\r', ' ').Replace('\n', ' '))));

        return
            (Report: string.Join(", ", sqlExecuter.ExecutedScriptsWithTransaction.SelectMany(script => script.Item1)),
            SqlExecuter: sqlExecuter,
            RemovedConcepts: conceptApplicationRepository.DeletedLog,
            InsertedConcepts: conceptApplicationRepository.InsertedLog.ToList());
    }

    private List<ConceptApplication> CreateConceptApplications(IEnumerable<CodeGenerator> codeGenerators)
    {
        var implementations = new PluginsMetadataList<IConceptDatabaseDefinition>();
        var implementationNames = oldCodeGenerators.Concat(newCodeGenerators).Select(ca => ca.ConceptImplementationTypeName).Distinct();
        foreach (var implementationName in implementationNames)
            implementations.Add((IConceptDatabaseDefinition)Activator.CreateInstance(Type.GetType(implementationName)));
        implementations.Add(new NullImplementation());
        var databasePlugins = MockDatabasePluginsContainer.Create(implementations);

        var databaseModelBuilder = new DatabaseModelBuilderAccessor(databasePlugins, null);
        databaseModelBuilder.CreateDatabaseModel;

        databaseModelBuilder.ComputeDependsOn(oldCodeGenerators.Cast<CodeGenerator>());
        databaseModelBuilder.ComputeDependsOn(newCodeGenerators);
        TestUtility.Dump(oldCodeGenerators, a => $"\r\n{a} DEPENDS ON:{string.Concat(a.DependsOn.Select(d => $"\r\n - {d.ConceptApplication}"))}.");
        TestUtility.Dump(newCodeGenerators, a => $"\r\n{a} DEPENDS ON:{string.Concat(a.DependsOn.Select(d => $"\r\n - {d.ConceptApplication}"))}.");
    }

    */

        private List<ConceptApplication> CreateConceptApplications(params IConceptInfo[] concepts)
        {
            var implementations = new PluginsMetadataList<IConceptDatabaseDefinition>()
            {
                new NullImplementation(),
                { new SimpleImplementation(), typeof(SimpleConcept) },
                { new ReferenceImplementation(), typeof(ReferenceConcept) },
                { new ReferenceReferenceImplementation(), typeof(ReferenceReferenceConcept) },
            };

            var databaseModelBuilder = new DatabaseModelBuilder(
                MockDatabasePluginsContainer.Create(implementations),
                new MockDslModel(concepts),
                new ConsoleLogProvider(),
                new DatabaseModelDependencies(new ConsoleLogProvider()));

            var conceptApplications = databaseModelBuilder.CreateDatabaseModel().ConceptApplications;
            Console.WriteLine("ConceptApplications:"
                + string.Concat(conceptApplications.Select(ca => $"\r\n- {ca}, depends on: {string.Join(", ", ca.DependsOn.Select(d => d.ToString()))}.")));
            return conceptApplications;
        }

        /// <summary>
        /// Report contains created and removed concept applications in the mock database.
        /// It contains metadata changes (ins, upd, del) and object creation scripts.
        /// </summary>
        private
            (string Report,
            MockSqlExecuter SqlExecuter,
            List<ConceptApplication> RemovedConcepts,
            List<ConceptApplication> InsertedConcepts)
            DatabaseGeneratorUpdateDatabase(
                IEnumerable<ConceptApplication> oldConceptApplications,
                IEnumerable<ConceptApplication> newConceptApplications)
        {
            // Update mock database (based on difference between old and new concept applications):

            var conceptApplicationRepository = new MockConceptApplicationRepository(oldConceptApplications);
            var databaseModel = new DatabaseModel { ConceptApplications = newConceptApplications.ToList() };
            var testConfig = new MockConfiguration { { "SqlExecuter.MaxJoinedScriptCount", 1 } };
            var sqlExecuter = new MockSqlExecuter();
            var sqlTransactionBatches = new SqlTransactionBatches(sqlExecuter, testConfig, new ConsoleLogProvider());

            IDatabaseGenerator databaseGenerator = new DatabaseGenerator(
                sqlTransactionBatches,
                conceptApplicationRepository,
                new ConsoleLogProvider(),
                new DatabaseGeneratorOptions { ShortTransactions = false },
                databaseModel);

            databaseGenerator.UpdateDatabaseStructure();

            // Report changes in mock database:

            TestUtility.Dump(
                sqlExecuter.ExecutedScriptsWithTransaction,
                script => (script.Item2 ? "tran" : "notran")
                    + string.Concat(script.Item1.Select(sql => "\r\n  - " + sql.Replace('\r', ' ').Replace('\n', ' '))));

            return
                (Report: string.Join(", ", sqlExecuter.ExecutedScriptsWithTransaction.SelectMany(script => script.Item1)),
                SqlExecuter: sqlExecuter,
                RemovedConcepts: conceptApplicationRepository.DeletedLog,
                InsertedConcepts: conceptApplicationRepository.InsertedLog.ToList());
        }

        #endregion Helper methods

        //============================================================================

        [TestMethod]
        public void NoChange()
        {
            var oldApplications = CreateConceptApplications(
                new SimpleConcept("A", "sqlA", "dataA1"),
                new SimpleConcept("B", "sqlB", "dataB1"));

            var newApplications = CreateConceptApplications(
                new SimpleConcept("A", "sqlA", "dataA2"),
                new SimpleConcept("B", "sqlB", "dataB2"));

            string expected = "";

            Assert.AreEqual(expected, DatabaseGeneratorUpdateDatabase(oldApplications, newApplications).Report);
        }

        [TestMethod]
        public void SimpleChange()
        {
            var oldApplications = CreateConceptApplications(
                new SimpleConcept("A", "sqlA1"),
                new SimpleConcept("B", "sqlB"));

            var newApplications = CreateConceptApplications(
                new SimpleConcept("A", "sqlA2"),
                new SimpleConcept("B", "sqlB"));

            string expected = "drop-sqlA1, del SimpleConcept A, sqlA2, ins SimpleConcept A";

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);
            Assert.AreEqual(expected, dbUpdate.Report);
        }

        [TestMethod]
        public void MustRecreateDependentConcept()
        {
            var oldSimple = new SimpleConcept("S", "sqlS1");
            var newSimple = new SimpleConcept("S", "sqlS2");

            var oldReferenceUnchanged = new ReferenceConcept("R", oldSimple, "sqlR");
            var newReferenceUnchanged = new ReferenceConcept("R", newSimple, "sqlR"); // Reference is not changed.

            Assert.AreEqual(oldReferenceUnchanged.Name, newReferenceUnchanged.Name);
            Assert.AreEqual(oldReferenceUnchanged.Sql, newReferenceUnchanged.Sql);
            var oldApplications = CreateConceptApplications(oldSimple, oldReferenceUnchanged);
            var newApplications = CreateConceptApplications(newSimple, newReferenceUnchanged);

            string expected = "drop-sqlR, del ReferenceConcept R, drop-sqlS1, del SimpleConcept S, sqlS2, ins SimpleConcept S, sqlR, ins ReferenceConcept R";

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);
            Assert.AreEqual(expected, dbUpdate.Report);
        }

        [TestMethod]
        public void RecreatedDependentConceptReferencesNewVersion()
        {
            var oldSimple = new SimpleConcept("S", "sqlS1");
            var newSimple = new SimpleConcept("S", "sqlS2");

            var oldReferenceUnchanged = new ReferenceConcept("R", oldSimple, "sqlR");
            var newReferenceUnchanged = new ReferenceConcept("R", newSimple, "sqlR"); // Reference is not changed.

            Assert.AreEqual(oldReferenceUnchanged.Name, newReferenceUnchanged.Name);
            Assert.AreEqual(oldReferenceUnchanged.Sql, newReferenceUnchanged.Sql);
            var oldApplications = CreateConceptApplications(oldSimple, oldReferenceUnchanged);
            var newApplications = CreateConceptApplications(newSimple, newReferenceUnchanged);

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            Assert.AreEqual("sqlS1", dbUpdate.RemovedConcepts.Find<ReferenceConcept>().DependsOn.Single().CreateQuery,
                "Removed reference should point to old version of changed concept.");
            Assert.AreEqual("sqlS2", dbUpdate.InsertedConcepts.Find<ReferenceConcept>().DependsOn.Single().CreateQuery,
                "Inserted reference should point to new version of changed concept.");
        }

        [TestMethod]
        public void MustRecreateDependentConceptWhereBaseIsCreatedAndDeletedWithSameKey()
        {
            var oldSimple = new SimpleConcept("S", "sqlS");
            var newSimple = new SimpleConcept("S", "sqlS"); // S is not changed, but its implementation type will be changes later.

            var oldReferenceUnchanged = new ReferenceConcept("R", oldSimple, "sqlR");
            var newReferenceUnchanged = new ReferenceConcept("R", newSimple, "sqlR"); // R is not changed.

            Assert.AreEqual(oldReferenceUnchanged.Name, newReferenceUnchanged.Name);
            Assert.AreEqual(oldReferenceUnchanged.Sql, newReferenceUnchanged.Sql);
            var oldApplications = CreateConceptApplications(oldSimple, oldReferenceUnchanged);
            var newApplications = CreateConceptApplications(newSimple, newReferenceUnchanged);

            // Changing ConceptImplementationTypeName will affect ConceptApplication's key (see GetConceptApplicationKey), and result with dropping and creating it in database.
            // This behavior might change in future if some optimizations are implemented in DatabaseGenerator.
            oldApplications.Find<SimpleConcept>().ConceptImplementationTypeName = typeof(SimpleImplementation).AssemblyQualifiedName.Replace(nameof(SimpleImplementation), "OldImplementationThatNoLongerExists");

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            string expected = "drop-sqlR, del ReferenceConcept R, drop-sqlS, del SimpleConcept S, sqlS, ins SimpleConcept S, sqlR, ins ReferenceConcept R";
            Assert.AreEqual(expected, dbUpdate.Report);

            var removedSimple = dbUpdate.RemovedConcepts.Find<SimpleConcept>();
            var removedReference = dbUpdate.RemovedConcepts.Find<ReferenceConcept>();
            var insertedSimple = dbUpdate.InsertedConcepts.Find<SimpleConcept>();
            var insertedReference = dbUpdate.InsertedConcepts.Find<ReferenceConcept>();
            Assert.AreNotSame(removedSimple, insertedSimple);
            Assert.AreSame(removedSimple, removedReference.DependsOn.Single(), "Removed reference should point to old version of changed concept.");
            Assert.AreEqual(insertedSimple, insertedReference.DependsOn.Single(), "Inserted reference should point to new version of changed concept.");
        }

        [TestMethod]
        public void RecreateDependentConceptWithNewConceptInfoReference()
        {
            var oldSimpleA = new SimpleConcept("A", "sqlA1");
            var newSimpleA = new SimpleConcept("A", "sqlA2"); // A has been modified.

            var oldSimpleB = new SimpleConcept("B", "sqlB");
            var newSimpleB = new SimpleConcept("B", "sqlB");

            var oldApplications = CreateConceptApplications(oldSimpleA, oldSimpleB);
            var newApplications = CreateConceptApplications(newSimpleA, newSimpleB);

            // Adding a dependency that did not exist in the version 1.
            var newSimpleAApplication = newApplications.Where(ca => ca.ConceptInfoKey == "SimpleConcept A").Single();
            var newSimpleBApplication = newApplications.Where(ca => ca.ConceptInfoKey == "SimpleConcept B").Single();
            newSimpleBApplication.DependsOn = new[] { newSimpleAApplication };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            // Event if B did not depend on A previously, it depends now, so it should be refreshed when A has been modified.
            Assert.AreEqual(2, dbUpdate.RemovedConcepts.Count);
            Assert.AreEqual(2, dbUpdate.InsertedConcepts.Count);
        }

        [TestMethod]
        public void RecreateDependentConceptWithOldConceptInfoReference()
        {
            var oldSimpleA = new SimpleConcept("A", "sqlA1");
            var newSimpleA = new SimpleConcept("A", "sqlA2"); // A has been modified.

            var oldSimpleB = new SimpleConcept("B", "sqlB");
            var newSimpleB = new SimpleConcept("B", "sqlB");

            var oldApplications = CreateConceptApplications(oldSimpleA, oldSimpleB);
            var newApplications = CreateConceptApplications(newSimpleA, newSimpleB);

            // B previously depended on A, but it does not in new version.
            var oldSimpleAApplication = oldApplications.Where(ca => ca.ConceptInfoKey == "SimpleConcept A").Single();
            var oldSimpleBApplication = oldApplications.Where(ca => ca.ConceptInfoKey == "SimpleConcept B").Single();
            oldSimpleBApplication.DependsOn = new[] { oldSimpleAApplication };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            // Event if B does no longer depend on A, it depended previously, so it should be refreshed when A has been modified.
            // **Note**: This test is just a snapshot of current DatabaseGenerator behavior. I am not sure if this behavior should be preserved.
            Assert.AreEqual(2, dbUpdate.RemovedConcepts.Count);
            Assert.AreEqual(2, dbUpdate.InsertedConcepts.Count);
        }

        [TestMethod]
        public void MustRecreateDependentConceptInCorrectOrder()
        {
            // A <- B <- C;
            var oldA = new SimpleConcept("A", "sqlA1");
            var newA = new SimpleConcept("A", "sqlA2"); // Changed.

            var oldB = new ReferenceConcept("B", oldA, "sqlB");
            var newB = new ReferenceConcept("B", newA, "sqlB"); // Reference is not changed.

            var oldC = new ReferenceReferenceConcept("C", oldB, "sqlC1");
            var newC = new ReferenceReferenceConcept("C", newB, "sqlC2"); // Second-level reference is changed.

            var oldApplications = CreateConceptApplications(oldA, oldB, oldC);
            var newApplications = CreateConceptApplications(newA, newB, newC);

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            string expected = "drop-sqlC1, del ReferenceReferenceConcept C, drop-sqlB, del ReferenceConcept B, drop-sqlA1, del SimpleConcept A,"
                + " sqlA2, ins SimpleConcept A, sqlB, ins ReferenceConcept B, sqlC2, ins ReferenceReferenceConcept C";
            Assert.AreEqual(expected, dbUpdate.Report);
        }
        
        [TestMethod]
        public void UnchangedMiddleReferenceWithoutDatabaseGenerator()
        {
            // A <- B <- C;
            var oldA = new SimpleConcept("A", "sqlA1");
            var newA = new SimpleConcept("A", "sqlA2"); // Changed.

            var oldB = new NoImplementationConcept("B", oldA);
            var newB = new NoImplementationConcept("B", newA); // A and C have database generator implementations, but B doesn't.

            var oldC = new ReferenceConcept("C", oldB, "sqlC1");
            var newC = new ReferenceConcept("C", newB, "sqlC2"); // Second-level reference is changed.

            var oldApplications = CreateConceptApplications(oldA, oldB, oldC);
            var newApplications = CreateConceptApplications(newA, newB, newC);

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            string expected = "drop-sqlC1, del ReferenceConcept C, del NoImplementationConcept B, drop-sqlA1, del SimpleConcept A,"
            + " sqlA2, ins SimpleConcept A, ins NoImplementationConcept B, sqlC2, ins ReferenceConcept C";
            Assert.AreEqual(expected, dbUpdate.Report);
        }

        [TestMethod]
        public void NoTransaction()
        {
            var oldA = new SimpleConcept("A", "sqlA1");
            var newA = new SimpleConcept("A", "sqlA2");

            var oldB = new SimpleConcept("B", SqlUtility.NoTransactionTag + "sqlB1");
            var newB = new SimpleConcept("B", SqlUtility.NoTransactionTag + "sqlB2");

            var oldC = new SimpleConcept("C", "sqlC1");
            var newC = new SimpleConcept("C", "sqlC2");

            var oldApplications = CreateConceptApplications(oldA, oldB, oldC);
            var newApplications = CreateConceptApplications(newA, newB, newC);

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            string executedSqlReport = string.Concat(
                dbUpdate.SqlExecuter.ExecutedScriptsWithTransaction
                    .Select(scripts => (scripts.Item2 ? "TRAN" : "NOTRAN") + ": " + string.Join(", ", scripts.Item1) + ". "))
                    .Replace(SqlUtility.NoTransactionTag, "")
                    .Trim();

            string expected = @"
                TRAN: drop-sqlC1, del SimpleConcept C.
                NOTRAN: drop-sqlB1. TRAN: del SimpleConcept B.
                TRAN: drop-sqlA1, del SimpleConcept A,
                sqlA2, ins SimpleConcept A.
                NOTRAN: sqlB2. TRAN: ins SimpleConcept B.
                TRAN: sqlC2, ins SimpleConcept C.";

            Assert.AreEqual(FormatLines(expected), FormatLines(executedSqlReport));
        }

        private static string FormatLines(string s) => _whitespaces.Replace(s, " ").Trim().Replace(". ", ".\r\n");

        private static readonly Regex _whitespaces = new Regex(@"\s+");
    }
}
