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

using Autofac.Features.Indexed;
using Autofac.Features.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.DatabaseGenerator.Test
{
    [TestClass]
    public class DatabaseGeneratorTest
    {
        public DatabaseGeneratorTest()
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();
            LegacyUtilities.Initialize(configurationProvider);
        }

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

        private static NewConceptApplication CreateBaseCiApplication(string name, IConceptDatabaseDefinition implementation = null)
        {
            implementation = implementation ?? new SimpleConceptImplementation();
            var conceptInfo = new BaseCi { Name = name };
            return new NewConceptApplication(conceptInfo, implementation)
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

        private static NewConceptApplication CreateSimpleCiApplication(string name, string sql = null, string data = null, ConceptApplication dependsOn = null, IConceptDatabaseDefinition implementation = null)
        {
            return new NewConceptApplication(new SimpleCi { Name = name, Data = data ?? $"{name}Data" }, implementation ?? new SimpleConceptImplementation())
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

            public static NewConceptApplication CreateApplication(NewConceptApplication reference, string sql = null)
            {
                return new NewConceptApplication(
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

            public static NewConceptApplication CreateApplication(string sql, NewConceptApplication reference)
            {
                return new NewConceptApplication(
                    new ReferenceToReferencingCi { Reference = (ReferencingCi)(reference.ConceptInfo), Data = "data" },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }
        }

        #endregion Helper classes

        /// <summary>
        /// Report contains created and removed concept applications in the mock database.
        /// It contains metadata changes (ins, upd, del) and object creation scripts.
        /// </summary>
        private
            (string Report,
            MockSqlExecuter SqlExecuter,
            List<ConceptApplication> RemovedConcepts,
            List<NewConceptApplication> InsertedConcepts)
            DatabaseGeneratorUpdateDatabase(
                IEnumerable<ConceptApplication> oldApplications,
                IEnumerable<NewConceptApplication> newApplications,
                bool computeDependencies = true)
        {
            foreach (var ca in oldApplications)
                ca.Id = Guid.NewGuid();

            if (computeDependencies)
            {
                var duplicate = oldApplications.Cast<NewConceptApplication>().Intersect(newApplications).FirstOrDefault();
                if (duplicate != null)
                    Assert.Fail($"Incorrect test input data: newApplications contains an instance from oldApplications: {duplicate}.");

                var implementations = new PluginsMetadataList<IConceptDatabaseDefinition>();
                var implementationNames = oldApplications.Concat(newApplications).Select(ca => ca.ConceptImplementationTypeName).Distinct();
                foreach (var implementationName in implementationNames)
                    implementations.Add((IConceptDatabaseDefinition)Activator.CreateInstance(Type.GetType(implementationName)));
                implementations.Add(new NullImplementation());
                var databasePlugins = MockDatabasePluginsContainer.Create(implementations);

                var databaseModelBuilder = new DatabaseModelGeneratorAccessor(databasePlugins, null);
                databaseModelBuilder.ComputeDependsOn(oldApplications.Cast<NewConceptApplication>());
                databaseModelBuilder.ComputeDependsOn(newApplications);
                TestUtility.Dump(oldApplications, a => $"\r\n{a} DEPENDS ON:{string.Concat(a.DependsOn.Select(d => $"\r\n - {d.ConceptApplication}"))}.");
                TestUtility.Dump(newApplications, a => $"\r\n{a} DEPENDS ON:{string.Concat(a.DependsOn.Select(d => $"\r\n - {d.ConceptApplication}"))}.");
            }

            var conceptApplicationRepository = new MockConceptApplicationRepository { ConceptApplications = oldApplications.ToList() };
            var databaseModel = new DatabaseModel { ConceptApplications = newApplications.ToList<ConceptApplication>() };
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

            TestUtility.Dump(
                sqlExecuter.ExecutedScriptsWithTransaction,
                script => (script.Item2 ? "tran" : "notran")
                    + string.Concat(script.Item1.Select(sql => "\r\n  - " + sql.Replace('\r', ' ').Replace('\n', ' '))));

            return
                (Report: string.Join(", ", sqlExecuter.ExecutedScriptsWithTransaction.SelectMany(script => script.Item1)),
                SqlExecuter: sqlExecuter,
                RemovedConcepts: conceptApplicationRepository.DeletedLog,
                InsertedConcepts: conceptApplicationRepository.InsertedLog.Cast<NewConceptApplication>().ToList());
        }

        //============================================================================

        [TestMethod]
        public void NoChange()
        {
            var oldApplications = new List<ConceptApplication> { CreateSimpleCiApplication("unchanged") };
            var newApplications = new List<NewConceptApplication> { CreateSimpleCiApplication("unchanged") };

            Assert.AreEqual("", DatabaseGeneratorUpdateDatabase(oldApplications, newApplications).Report);
        }

        [TestMethod]
        public void SimpleChange()
        {
            var oldApplications = new List<ConceptApplication> { CreateSimpleCiApplication("A", "old"), CreateSimpleCiApplication("B", "unchanged") };
            var newApplications = new List<NewConceptApplication> { CreateSimpleCiApplication("B", "unchanged"), CreateSimpleCiApplication("A", "newASql") };

            Assert.AreEqual(
                "del BaseCi A, newASql, ins BaseCi A",
                DatabaseGeneratorUpdateDatabase(oldApplications, newApplications).Report);
        }

        [TestMethod]
        public void MustRecreateDependentConcept()
        {
            var simpleV1 = CreateSimpleCiApplication("simple", "simpleSql1");
            var simpleV2 = CreateSimpleCiApplication("simple", "simpleSql2");
            var dependentV1Unchanged = ReferencingCi.CreateApplication(simpleV1);
            var dependentV2Unchanged = ReferencingCi.CreateApplication(simpleV2);

            var oldApplications = new List<ConceptApplication> { simpleV1, dependentV1Unchanged };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependentV2Unchanged };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            Assert.AreEqual(2, dbUpdate.RemovedConcepts.Count);
            Assert.IsTrue(dbUpdate.RemovedConcepts.Contains(simpleV1));
            Assert.IsTrue(dbUpdate.RemovedConcepts.Contains(dependentV1Unchanged));
            Assert.AreEqual(2, dbUpdate.InsertedConcepts.Count);
            Assert.IsTrue(dbUpdate.InsertedConcepts.Contains(simpleV2));
            Assert.IsTrue(dbUpdate.InsertedConcepts.Contains(dependentV2Unchanged));
        }

        [TestMethod]
        public void RecreatedDependentConceptReferencesNewVersion()
        {
            var simpleV1 = CreateSimpleCiApplication("simple", "simpleSqlV1", "data1");
            var simpleV2 = CreateSimpleCiApplication("simple", "simpleSqlV2", "data2");
            var dependentV1Unchanged = ReferencingCi.CreateApplication(simpleV1);
            var dependentV2Unchanged = ReferencingCi.CreateApplication(simpleV2);
            Assert.IsTrue(dependentV1Unchanged.GetConceptApplicationKey().Equals(dependentV2Unchanged.GetConceptApplicationKey()), "Test initialization: Dependent concept has not changed, the referenced concept has changed (different concept key).");

            var oldApplications = new List<ConceptApplication> { simpleV1, dependentV1Unchanged };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependentV2Unchanged };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            var removedReferencing = (ReferencingCi)dbUpdate.RemovedConcepts.Select(ca => ((NewConceptApplication)ca).ConceptInfo).First(ci => ci is ReferencingCi);
            var insertedReferencing = (ReferencingCi)dbUpdate.InsertedConcepts.Select(ca => ca.ConceptInfo).First(ci => ci is ReferencingCi);

            Assert.AreEqual("data1", removedReferencing.Reference.Data, "Removed reference should point to old version of changed concept.");
            Assert.AreEqual("data2", insertedReferencing.Reference.Data, "Inserted reference should point to new version of changed concept.");
        }

        [TestMethod]
        public void MustRecreateDependentConceptWhereBaseIsCreatedAndDeletedWithSameKey()
        {
            var simpleC1 = CreateSimpleCiApplication("same", implementation: new SimpleConceptImplementation());
            var simpleC2 = CreateSimpleCiApplication("same", implementation: new DependentConceptImplementation());
            var dependentC1Unchanged = ReferencingCi.CreateApplication(simpleC1);
            var dependentC2Unchanged = ReferencingCi.CreateApplication(simpleC2);
            Assert.IsTrue(dependentC1Unchanged.GetConceptApplicationKey().Equals(dependentC2Unchanged.GetConceptApplicationKey()), "Test initialization: Dependent concept has not changed, the referenced concept has changed (different implementation, same concept key).");

            var oldApplications = new List<ConceptApplication> { simpleC1, dependentC1Unchanged };
            var newApplications = new List<NewConceptApplication> { simpleC2, dependentC2Unchanged };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            Assert.AreEqual(2, dbUpdate.RemovedConcepts.Count);
            Assert.IsTrue(dbUpdate.RemovedConcepts.Contains(simpleC1));
            Assert.IsTrue(dbUpdate.RemovedConcepts.Contains(dependentC1Unchanged));
            Assert.AreEqual(2, dbUpdate.InsertedConcepts.Count);
            Assert.IsTrue(dbUpdate.InsertedConcepts.Contains(simpleC2));
            Assert.IsTrue(dbUpdate.InsertedConcepts.Contains(dependentC2Unchanged));

            var removedReferencing = dbUpdate.RemovedConcepts.Select(ca => ((NewConceptApplication)ca).ConceptInfo).OfType<ReferencingCi>().Single();
            var insertedReferencing = dbUpdate.InsertedConcepts.Select(ca => ca.ConceptInfo).OfType<ReferencingCi>().Single();
            Assert.AreNotSame(simpleC1.ConceptInfo, simpleC2.ConceptInfo);
            Assert.AreSame(simpleC1.ConceptInfo, removedReferencing.Reference, "Removed reference should point to old version of changed concept.");
            Assert.AreSame(simpleC2.ConceptInfo, insertedReferencing.Reference, "Inserted reference should point to new version of changed concept.");
        }

        [TestMethod]
        public void MustRecreateDependentConceptWithNewConceptInfoReference()
        {
            var simple1a = CreateSimpleCiApplication("a", "sqla1");
            var simple1b = CreateSimpleCiApplication("b", "sqlb");
            var simple2a = CreateSimpleCiApplication("a", "sqla2"); // Object modified in version 2.
            var simple2b = CreateSimpleCiApplication("b", "sqlb", dependsOn: simple2a); // Adding a dependency that did not exist in the version 1.

            var oldApplications = new List<ConceptApplication> { simple1a, simple1b };
            var newApplications = new List<NewConceptApplication> { simple2a, simple2b };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications, computeDependencies: false);

            Assert.AreEqual(2, dbUpdate.RemovedConcepts.Count);
            Assert.AreEqual(2, dbUpdate.InsertedConcepts.Count);
        }

        [TestMethod]
        public void MustRecreateDependentConceptInCorrectOrder()
        {
            var simpleV1 = CreateSimpleCiApplication("simple", "v1");
            var simpleV2 = CreateSimpleCiApplication("simple", "v2");
            var dependentUnchanged1 = ReferencingCi.CreateApplication(simpleV1);
            var dependentUnchanged2 = ReferencingCi.CreateApplication(simpleV2);
            var secondReference1 = ReferenceToReferencingCi.CreateApplication("1", dependentUnchanged1);
            var secondReference2 = ReferenceToReferencingCi.CreateApplication("2", dependentUnchanged2);

            var oldApplications = new List<ConceptApplication> { simpleV1, dependentUnchanged1, secondReference1 };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependentUnchanged2, secondReference2 };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            Assert.AreEqual(3, dbUpdate.RemovedConcepts.Count);
            Assert.IsTrue(dbUpdate.RemovedConcepts.IndexOf(dependentUnchanged1) < dbUpdate.RemovedConcepts.IndexOf(simpleV1));
            Assert.IsTrue(dbUpdate.RemovedConcepts.IndexOf(secondReference1) < dbUpdate.RemovedConcepts.IndexOf(dependentUnchanged1));
            Assert.AreEqual(3, dbUpdate.InsertedConcepts.Count);
            Assert.IsTrue(dbUpdate.InsertedConcepts.IndexOf(dependentUnchanged2) > dbUpdate.InsertedConcepts.IndexOf(simpleV2));
            Assert.IsTrue(dbUpdate.InsertedConcepts.IndexOf(secondReference2) > dbUpdate.InsertedConcepts.IndexOf(dependentUnchanged2));
        }
        
        [TestMethod]
        public void UnchangedMiddleReference()
        {
            var aold = CreateSimpleCiApplication("a", "old");
            var b1 = ReferencingCi.CreateApplication(aold, "b");
            var c1 = ReferenceToReferencingCi.CreateApplication("c", b1);
            var anew = CreateSimpleCiApplication("a", "new");
            var b2 = ReferencingCi.CreateApplication(anew, "b");
            var c2 = ReferenceToReferencingCi.CreateApplication("c", b2);

            var oldApplications = new List<ConceptApplication> { aold, c1 }; // A and C have database generator implementations, but B doesn't.
            var newApplications = new List<NewConceptApplication> { c2, anew };

            var dbUpdate = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);

            Assert.AreEqual(2, dbUpdate.RemovedConcepts.Count);
            Assert.IsTrue(dbUpdate.RemovedConcepts.IndexOf(c1) < dbUpdate.RemovedConcepts.IndexOf(aold));
            Assert.AreEqual(2, dbUpdate.InsertedConcepts.Count);
            Assert.IsTrue(dbUpdate.InsertedConcepts.IndexOf(anew) < dbUpdate.InsertedConcepts.IndexOf(c2));
        }

        [TestMethod]
        public void NoTransaction()
        {
            var oldApplications = new List<ConceptApplication> {
                    CreateBaseCiApplication("A", new SimpleConceptImplementation()),
                    CreateBaseCiApplication("B", new NoTransactionConceptImplementation()),
                    CreateBaseCiApplication("C", new SimpleConceptImplementation()) };
            var newApplications = new List<NewConceptApplication> {
                    CreateBaseCiApplication("D", new SimpleConceptImplementation()),
                    CreateBaseCiApplication("E", new NoTransactionConceptImplementation()),
                    CreateBaseCiApplication("F", new SimpleConceptImplementation()) };

            var dbUpgrade = DatabaseGeneratorUpdateDatabase(oldApplications, newApplications);
            string executedSqlReport = string.Concat(
                dbUpgrade.SqlExecuter.ExecutedScriptsWithTransaction
                    .Select(scripts => (scripts.Item2 ? "TRAN" : "NOTRAN") + ": " + string.Join(", ", scripts.Item1) + ". "))
                    .Replace(SqlUtility.NoTransactionTag, "")
                    .Trim();

            string expected = @"
                TRAN: remove C, del BaseCi C.
                NOTRAN: remove B. TRAN: del BaseCi B.
                TRAN: remove A, del BaseCi A,
                create D, ins BaseCi D.
                NOTRAN: create E. TRAN: ins BaseCi E.
                TRAN: create F, ins BaseCi F.";
            string expectedReport = whitespaces.Replace(expected, " ").Trim();

            Assert.AreEqual(expectedReport, executedSqlReport);
        }

        private static readonly Regex whitespaces = new Regex(@"\s+");
    }
}
