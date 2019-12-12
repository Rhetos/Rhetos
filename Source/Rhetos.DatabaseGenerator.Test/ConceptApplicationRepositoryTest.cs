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
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.DatabaseGenerator.Test
{
    class DomainObjectModelMock : IDomainObjectModel
    {
        public IEnumerable<System.Reflection.Assembly> Assemblies
        {
            get { return new[] { GetType().Assembly }; }
        }
    }

    public class TestConceptInfo : IConceptInfo
    {
        [ConceptKey]
        public string Name { get; set; }
    }

    class TestConceptImplementation : IConceptDatabaseDefinition
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
    }

    [TestClass]
    public class ConceptApplicationRepositoryTest
    {
        private static ConceptApplication NewConceptApplication(
            IConceptInfo conceptInfo,
            IConceptDatabaseDefinition conceptImplementation,
            Guid Id,
            string CreateQuery,
            ConceptApplication[] DependsOn,
            int OldCreationOrder)
        {
            return new ConceptApplication
            {
                //ConceptInfo = conceptInfo,
                ConceptInfoTypeName = conceptInfo.GetType().AssemblyQualifiedName,
                ConceptInfoKey = conceptInfo.GetKey(),
                //ConceptImplementation = conceptImplementation,
                //ConceptImplementationType = conceptImplementation.GetType(),
                ConceptImplementationTypeName = conceptImplementation.GetType().AssemblyQualifiedName,
                //ConceptImplementationVersion = GetVersionFromAttribute(conceptImplementation.GetType()),
                Id = Id,
                CreateQuery = CreateQuery,
                DependsOn = DependsOn,
                OldCreationOrder = OldCreationOrder
            };
        }

        public ConceptApplicationRepositoryTest()
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();
            LegacyUtilities.Initialize(configurationProvider);
        }

        private class MockSqlExecuter : ISqlExecuter
        {
            public static readonly ConceptApplication DependencyCa1 = NewConceptApplication(
                new TestConceptInfo { Name = "dep1" }, new TestConceptImplementation(),
                Id: Guid.Parse("88CAD02E-5869-4028-B528-4A4723B47C85"),
                CreateQuery: "dep 1 create query",
                DependsOn: new ConceptApplication[] {},
                OldCreationOrder: 1
            );
            public static readonly ConceptApplication DependencyCa2 = NewConceptApplication(
                new TestConceptInfo { Name = "dep2" }, new TestConceptImplementation(),
                Id: Guid.Parse("2567A911-4DA4-4737-B68B-3A51364E667B"),
                CreateQuery: "dep 2 create query",
                DependsOn: new ConceptApplication[] { },
                OldCreationOrder: 2
            );

            public static readonly ConceptApplication ConceptApplication = NewConceptApplication(
                new TestConceptInfo { Name = "abc" }, new TestConceptImplementation(),
                Id: Guid.Parse("E687F635-E5B4-4DEA-8079-F9F17B7237D6"),
                CreateQuery: "create query",
                DependsOn: new ConceptApplication[] { DependencyCa1, DependencyCa2 },
                OldCreationOrder: 3
            );

            public static readonly ConceptApplication ConceptApplicationCopy = NewConceptApplication(
                new TestConceptInfo { Name = "abc" }, new TestConceptImplementation(),
                Id: Guid.Parse("30703370-A222-4467-B199-3E8F7E74609D"),
                CreateQuery: ConceptApplication.CreateQuery,
                DependsOn: ConceptApplication.DependsOn,
                OldCreationOrder: 4
            );

            public static readonly ConceptApplication ConceptApplication3 = NewConceptApplication(
                new TestConceptInfo { Name = "ca3" }, new TestConceptImplementation(),
                Id: Guid.Parse("FCAC6CA0-5A8F-4848-980B-573C32710374"),
                CreateQuery: "create query",
                DependsOn: new ConceptApplication[] { DependencyCa2 },
                OldCreationOrder: 5
            );

            private readonly IEnumerable<ConceptApplication> Expected;

            public MockSqlExecuter(IEnumerable<ConceptApplication> expected)
            {
                Expected = expected;
            }

            public void ExecuteReader(string command, Action<DbDataReader> action)
            {
                var table = new DataTable();
                var sqlSplit = new Regex(@"\W+").Split(command);

                if (sqlSplit.Contains("AppliedConcept"))
                {
                    // SELECT ID, InfoType, ConceptInfoKey, ImplementationType, CreateQuery, RemoveQuery, ModificationOrder
                    table.Columns.Add(new DataColumn("ID", typeof(Guid)));
                    table.Columns.Add();
                    table.Columns.Add();
                    table.Columns.Add();
                    table.Columns.Add();
                    table.Columns.Add();
                    table.Columns.Add(new DataColumn("ModificationOrder", typeof(int)));

                    foreach (var ca in Expected)
                        AddRow(table, ca);
                }
                else if (sqlSplit.Contains("AppliedConceptDependsOn"))
                {
                    //"SELECT DependentID, DependsOnID FROM Rhetos.AppliedConceptDependsOn"
                    table.Columns.Add(new DataColumn("DependentID", typeof(Guid)));
                    table.Columns.Add(new DataColumn("DependsOnID", typeof(Guid)));

                    foreach (var ca in Expected)
                        foreach (var dependsOn in ca.DependsOn)
                            table.Rows.Add(ca.Id, dependsOn.Id);
                }
                else
                    throw new NotImplementedException();

                var reader = new DataTableReader(table);
                while (reader.Read())
                    action(reader);
            }

            private static void AddRow(DataTable table, ConceptApplication ca)
            {
                // SELECT ID, InfoType, ConceptInfoKey, ImplementationType, CreateQuery, RemoveQuery, ModificationOrder
                table.Rows.Add(
                    ca.Id,
                    ca.ConceptInfoTypeName,
                    ca.ConceptInfoKey,
                    ca.ConceptImplementationTypeName,
                    ca.CreateQuery,
                    ca.RemoveQuery ?? "",
                    ca.OldCreationOrder);
            }

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction)
            {
                throw new NotImplementedException();
            }

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction, Action<int> beforeExecute, Action<int> afterExecute)
            {
                throw new NotImplementedException();
            }
        }

        private static string Dump(IEnumerable<ConceptApplication> appliedConcepts)
        {
            return string.Join(Environment.NewLine, appliedConcepts.Select(Dump));
        }

        private static string Dump(ConceptApplication appliedConcept)
        {
            var report = string.Format(
                "{0},{1},{2},{3},{4}",
                appliedConcept.Id,
                appliedConcept.ConceptInfoKey,
                appliedConcept.ConceptImplementationTypeName,
                appliedConcept.CreateQuery, 
                appliedConcept.OldCreationOrder);
            Console.WriteLine("[Dump] " + report);
            return report;
        }

        private ConceptApplicationRepository TestConceptApplicationRepository(IEnumerable<ConceptApplication> conceptApplications)
        {
            return new ConceptApplicationRepository(new MockSqlExecuter(conceptApplications));
        }

        [TestMethod]
        public void LoadPreviouslyAppliedConceptsTest()
        {
            var expected = new[] { MockSqlExecuter.ConceptApplication, MockSqlExecuter.DependencyCa1, MockSqlExecuter.DependencyCa2 };

            var conceptApplicationRepository = TestConceptApplicationRepository(expected);
            var appliedConcepts = conceptApplicationRepository.Load();

            Assert.AreEqual(Dump(expected), Dump(appliedConcepts));
        }

        [TestMethod]
        public void LoadPreviouslyAppliedConceptsTest_InvalidDependency()
        {
            var concepts = new[] { MockSqlExecuter.ConceptApplication3 };

            // ConceptApplication3 references DependencyCa2, which is not provided to the repository
            TestUtility.ShouldFail(() =>
                {
                    var conceptApplicationRepository = TestConceptApplicationRepository(concepts);
                    var appliedConcepts = conceptApplicationRepository.Load();
                    Assert.IsNotNull(appliedConcepts);
                },
                MockSqlExecuter.DependencyCa2.Id.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void LoadPreviouslyAppliedConceptsTest_DuplicateAppliedConcepts()
        {
            try
            {
                var ca1 = MockSqlExecuter.ConceptApplication;
                var ca2 = MockSqlExecuter.ConceptApplicationCopy;
                Assert.AreEqual(ca1.GetConceptApplicationKey(), ca2.GetConceptApplicationKey());
                var expected = new[] {ca1, ca2};

                var conceptApplicationRepository = TestConceptApplicationRepository(expected);
                var appliedConcepts = conceptApplicationRepository.Load();
                Assert.IsNotNull(appliedConcepts);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TestUtility.AssertContains(ex.Message, MockSqlExecuter.ConceptApplication.GetConceptApplicationKey());
                TestUtility.AssertContains(ex.Message, new[] {
                    SqlUtility.GuidToString(MockSqlExecuter.ConceptApplication.Id),
                    SqlUtility.GuidToString(MockSqlExecuter.ConceptApplicationCopy.Id) });
                
                throw;
            }
        }
    }
}
