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

        public class SimpleConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "create " + ((BaseCi)conceptInfo).Name; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return "remove " + ((BaseCi)conceptInfo).Name; }
        }

        public class DependentConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "CREATE DependentConceptImplementation"; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        }

        public class NoTransactionConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return SqlUtility.NoTransactionTag + "create " + ((BaseCi)conceptInfo).Name; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return SqlUtility.NoTransactionTag + "remove " + ((BaseCi)conceptInfo).Name; }
        }

        public class BaseCi : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        public static NewConceptApplication CreateBaseCiApplication(string name, IConceptDatabaseDefinition implementation = null)
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

        public class SimpleCi : BaseCi
        {
            public string Data { get; set; }
        }

        public static NewConceptApplication CreateSimpleCiApplication(string name, string sql = null, string data = null, ConceptApplication dependsOn = null, IConceptDatabaseDefinition implementation = null)
        {
            return new NewConceptApplication(new SimpleCi { Name = name, Data = data ?? $"{name}Data" }, implementation ?? new SimpleConceptImplementation())
            {
                CreateQuery = sql ?? $"{name}Sql",
                DependsOn = dependsOn == null ? new ConceptApplicationDependency[] { } : new[] { new ConceptApplicationDependency { ConceptApplication = dependsOn } }
            };
        }

        public class SimpleCi2 : BaseCi
        {
            public string Data { get; set; }
        }

        public class SimpleCi3 : BaseCi
        {
            public string Data { get; set; }
        }

        public class ReferencingCi : IConceptInfo
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

        public class ReferenceToReferencingCi : IConceptInfo
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

        public class MultipleReferencingCi : BaseCi
        {
            public BaseCi Reference1 { get; set; }
            public BaseCi Reference2 { get; set; }

            public static NewConceptApplication CreateApplication(string name, NewConceptApplication reference1, NewConceptApplication reference2)
            {
                return new NewConceptApplication(
                    new MultipleReferencingCi { Name = name, Reference1 = (BaseCi)reference1.ConceptInfo, Reference2 = (BaseCi)reference2.ConceptInfo },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = "sql",
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }
        }

        public class PluginsMetadataList<TPlugin> : List<Tuple<TPlugin, Dictionary<string, object>>>
        {
            public void Add(TPlugin plugin, Dictionary<string, object> metadata)
            {
                this.Add(Tuple.Create(plugin, metadata));
            }

            public void Add(TPlugin plugin)
            {
                this.Add(Tuple.Create(plugin, new Dictionary<string, object> { }));
            }
        }

        public class StubIndex<TPlugin> : IIndex<Type, IEnumerable<TPlugin>>
        {
            private PluginsMetadataList<TPlugin> _pluginsWithMedata;

            public StubIndex(PluginsMetadataList<TPlugin> pluginsWithMedata)
            {
                _pluginsWithMedata = pluginsWithMedata;
            }
            public StubIndex()
            {
                _pluginsWithMedata = new PluginsMetadataList<TPlugin>();
            }
            public bool TryGetValue(Type key, out IEnumerable<TPlugin> value)
            {
                value = this[key];
                return true;
            }
            public IEnumerable<TPlugin> this[Type key]
            {
                get
                {
                    return _pluginsWithMedata
                        .Where(pm => pm.Item2.Any(metadata => metadata.Key == MefProvider.Implements && (Type)metadata.Value == key))
                        .Select(pm => pm.Item1)
                        .ToArray();
                }
            }
        }

        public static PluginsContainer<IConceptDatabaseDefinition> CreatePluginsContainer(PluginsMetadataList<IConceptDatabaseDefinition> conceptImplementations)
        {
            Lazy<IEnumerable<IConceptDatabaseDefinition>> plugins = new Lazy<IEnumerable<IConceptDatabaseDefinition>>(() =>
                conceptImplementations.Select(pm => pm.Item1));
            Lazy<IEnumerable<Meta<IConceptDatabaseDefinition>>> pluginsWithMetadata = new Lazy<IEnumerable<Meta<IConceptDatabaseDefinition>>>(() =>
                conceptImplementations.Select(pm => new Meta<IConceptDatabaseDefinition>(pm.Item1, pm.Item2)));
            Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>> pluginsByImplementation = new Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>>(() =>
                new StubIndex<IConceptDatabaseDefinition>(conceptImplementations));

            return new PluginsContainer<IConceptDatabaseDefinition>(plugins, pluginsByImplementation, new PluginsMetadataCache<IConceptDatabaseDefinition>(pluginsWithMetadata, new StubIndex<SuppressPlugin>()));
        }

        #endregion Helper classes

        private void TestDatabaseGenerator(
            IEnumerable<ConceptApplication> oldApplications,
            IEnumerable<NewConceptApplication> newApplications,
            out List<ConceptApplication> toBeRemoved,
            out List<NewConceptApplication> toBeInserted)
        {
            (_, _, toBeRemoved, toBeInserted) = TestDatabaseGenerator(oldApplications, newApplications);
        }

        /// <summary>
        /// Report contains created and removed concept applications in the mock database.
        /// It contains metadata changes (ins, upd, del) and object creation scripts.
        /// </summary>
        private
            (string Report,
            MockSqlExecuter SqlExecuter,
            List<ConceptApplication> RemovedConcepts,
            List<NewConceptApplication> InsertedConcepts)
            TestDatabaseGenerator(
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

                var databaseModelBuilder = new DatabaseModelBuilderAccessor(CreatePluginsContainer(implementations), null);
                databaseModelBuilder.ComputeDependsOn(oldApplications.Cast<NewConceptApplication>());
                databaseModelBuilder.ComputeDependsOn(newApplications);
                TestUtility.Dump(oldApplications, a => $"\r\n{a} DEPENDS ON:{string.Concat(a.DependsOn.Select(d => $"\r\n - {d.ConceptApplication}"))}.");
                TestUtility.Dump(newApplications, a => $"\r\n{a} DEPENDS ON:{string.Concat(a.DependsOn.Select(d => $"\r\n - {d.ConceptApplication}"))}.");
            }

            var conceptApplicationRepository = new MockConceptApplicationRepository { ConceptApplications = oldApplications.ToList() };
            var databaseModel = new MockDatabaseModel { ConceptApplications = newApplications.ToList() };
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
                InsertedConcepts: conceptApplicationRepository.InsertedLog);
        }

        //============================================================================

        [TestMethod]
        public void DatabaseGenerator_NoChange()
        {
            var oldApplications = new List<ConceptApplication> { CreateSimpleCiApplication("unchanged") };
            var newApplications = new List<NewConceptApplication> { CreateSimpleCiApplication("unchanged") };

            Assert.AreEqual("", TestDatabaseGenerator(oldApplications, newApplications).Report);
        }

        [TestMethod]
        public void DatabaseGenerator_SimpleChange()
        {
            var oldApplications = new List<ConceptApplication> { CreateSimpleCiApplication("A", "old"), CreateSimpleCiApplication("B", "unchanged") };
            var newApplications = new List<NewConceptApplication> { CreateSimpleCiApplication("B", "unchanged"), CreateSimpleCiApplication("A", "newASql") };

            Assert.AreEqual(
                "del BaseCi A, newASql, ins BaseCi A",
                TestDatabaseGenerator(oldApplications, newApplications).Report);
        }

        [TestMethod]
        public void DatabaseGenerator_MustRecreateDependentConcept()
        {
            var simpleV1 = CreateSimpleCiApplication("simple", "simpleSql1");
            var simpleV2 = CreateSimpleCiApplication("simple", "simpleSql2");
            var dependentV1Unchanged = ReferencingCi.CreateApplication(simpleV1);
            var dependentV2Unchanged = ReferencingCi.CreateApplication(simpleV2);

            var oldApplications = new List<ConceptApplication> { simpleV1, dependentV1Unchanged };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependentV2Unchanged };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(2, toBeRemoved.Count);
            Assert.IsTrue(toBeRemoved.Contains(simpleV1));
            Assert.IsTrue(toBeRemoved.Contains(dependentV1Unchanged));
            Assert.AreEqual(2, toBeInserted.Count);
            Assert.IsTrue(toBeInserted.Contains(simpleV2));
            Assert.IsTrue(toBeInserted.Contains(dependentV2Unchanged));
        }

        [TestMethod]
        public void DatabaseGenerator_RecreatedDependentConceptReferencesNewVersion()
        {
            var simpleV1 = CreateSimpleCiApplication("simple", "simpleSqlV1", "data1");
            var simpleV2 = CreateSimpleCiApplication("simple", "simpleSqlV2", "data2");
            var dependentV1Unchanged = ReferencingCi.CreateApplication(simpleV1);
            var dependentV2Unchanged = ReferencingCi.CreateApplication(simpleV2);
            Assert.IsTrue(dependentV1Unchanged.GetConceptApplicationKey().Equals(dependentV2Unchanged.GetConceptApplicationKey()), "Test initialization: Dependent concept has not changed, the referenced concept has changed (different concept key).");

            var oldApplications = new List<ConceptApplication> { simpleV1, dependentV1Unchanged };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependentV2Unchanged };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            var removedReferencing = (ReferencingCi)toBeRemoved.Select(ca => ((NewConceptApplication)ca).ConceptInfo).First(ci => ci is ReferencingCi);
            var insertedReferencing = (ReferencingCi)toBeInserted.Select(ca => ca.ConceptInfo).First(ci => ci is ReferencingCi);

            Assert.AreEqual("data1", removedReferencing.Reference.Data, "Removed reference should point to old version of changed concept.");
            Assert.AreEqual("data2", insertedReferencing.Reference.Data, "Inserted reference should point to new version of changed concept.");
        }

        [TestMethod]
        public void DatabaseGenerator_MustRecreateDependentConceptWhereBaseIsCreatedAndDeletedWithSameKey()
        {
            var simpleC1 = CreateSimpleCiApplication("same", implementation: new SimpleConceptImplementation());
            var simpleC2 = CreateSimpleCiApplication("same", implementation: new DependentConceptImplementation());
            var dependentC1Unchanged = ReferencingCi.CreateApplication(simpleC1);
            var dependentC2Unchanged = ReferencingCi.CreateApplication(simpleC2);
            Assert.IsTrue(dependentC1Unchanged.GetConceptApplicationKey().Equals(dependentC2Unchanged.GetConceptApplicationKey()), "Test initialization: Dependent concept has not changed, the referenced concept has changed (different implementation, same concept key).");

            var oldApplications = new List<ConceptApplication> { simpleC1, dependentC1Unchanged };
            var newApplications = new List<NewConceptApplication> { simpleC2, dependentC2Unchanged };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(2, toBeRemoved.Count);
            Assert.IsTrue(toBeRemoved.Contains(simpleC1));
            Assert.IsTrue(toBeRemoved.Contains(dependentC1Unchanged));
            Assert.AreEqual(2, toBeInserted.Count);
            Assert.IsTrue(toBeInserted.Contains(simpleC2));
            Assert.IsTrue(toBeInserted.Contains(dependentC2Unchanged));

            var removedReferencing = toBeRemoved.Select(ca => ((NewConceptApplication)ca).ConceptInfo).OfType<ReferencingCi>().Single();
            var insertedReferencing = toBeInserted.Select(ca => ca.ConceptInfo).OfType<ReferencingCi>().Single();
            Assert.AreNotSame(simpleC1.ConceptInfo, simpleC2.ConceptInfo);
            Assert.AreSame(simpleC1.ConceptInfo, removedReferencing.Reference, "Removed reference should point to old version of changed concept.");
            Assert.AreSame(simpleC2.ConceptInfo, insertedReferencing.Reference, "Inserted reference should point to new version of changed concept.");
        }

        [TestMethod]
        public void DatabaseGenerator_MustRecreateDependentConceptWithNewConceptInfoReference()
        {
            var simple1a = CreateSimpleCiApplication("a", "sqla1");
            var simple1b = CreateSimpleCiApplication("b", "sqlb");
            var simple2a = CreateSimpleCiApplication("a", "sqla2"); // Object modified in version 2.
            var simple2b = CreateSimpleCiApplication("b", "sqlb", dependsOn: simple2a); // Adding a dependency that did not exist in the version 1.

            var oldApplications = new List<ConceptApplication> { simple1a, simple1b };
            var newApplications = new List<NewConceptApplication> { simple2a, simple2b };

            var result = TestDatabaseGenerator(oldApplications, newApplications, computeDependencies: false);

            Assert.AreEqual(2, result.RemovedConcepts.Count);
            Assert.AreEqual(2, result.InsertedConcepts.Count);
        }

        [TestMethod]
        public void DatabaseGenerator_MustRecreateDependentConceptInCorrectOrder()
        {
            var simpleV1 = CreateSimpleCiApplication("simple", "v1");
            var simpleV2 = CreateSimpleCiApplication("simple", "v2");
            var dependentUnchanged1 = ReferencingCi.CreateApplication(simpleV1);
            var dependentUnchanged2 = ReferencingCi.CreateApplication(simpleV2);
            var secondReference1 = ReferenceToReferencingCi.CreateApplication("1", dependentUnchanged1);
            var secondReference2 = ReferenceToReferencingCi.CreateApplication("2", dependentUnchanged2);

            var oldApplications = new List<ConceptApplication> { simpleV1, dependentUnchanged1, secondReference1 };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependentUnchanged2, secondReference2 };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(3, toBeRemoved.Count);
            Assert.IsTrue(toBeRemoved.IndexOf(dependentUnchanged1) < toBeRemoved.IndexOf(simpleV1));
            Assert.IsTrue(toBeRemoved.IndexOf(secondReference1) < toBeRemoved.IndexOf(dependentUnchanged1));
            Assert.AreEqual(3, toBeInserted.Count);
            Assert.IsTrue(toBeInserted.IndexOf(dependentUnchanged2) > toBeInserted.IndexOf(simpleV2));
            Assert.IsTrue(toBeInserted.IndexOf(secondReference2) > toBeInserted.IndexOf(dependentUnchanged2));
        }

        [TestMethod]
        public void ExtractDependenciesFromConceptInfosTest()
        {
            var a = CreateBaseCiApplication("A");
            var b = CreateBaseCiApplication("B");
            var c = CreateBaseCiApplication("C");
            var r1 = MultipleReferencingCi.CreateApplication("1", a, b);
            var r2 = MultipleReferencingCi.CreateApplication("2", b, c);
            var r3 = MultipleReferencingCi.CreateApplication("3", r1, r2);
            var r5 = MultipleReferencingCi.CreateApplication("5", c, c);
            var r4 = MultipleReferencingCi.CreateApplication("4", c, r5);

            var all = new List<NewConceptApplication> { a, b, c, r1, r2, r3, r4, r5 };
            var dependencies = DatabaseModelBuilderAccessor.ExtractDependenciesFromConceptInfos(all);

            string result = string.Join(" ", dependencies
                .Select(d => ((dynamic)d).DependsOn.ConceptInfo.Name + "<" + ((dynamic)d).Dependent.ConceptInfo.Name)
                .OrderBy(str => str));
            Console.WriteLine(result);

            Assert.AreEqual("1<3 2<3 5<4 A<1 A<3 B<1 B<2 B<3 C<2 C<3 C<4 C<5", result);
        }

        [TestMethod]
        public void DatabaseGenerator_UnchangedMiddleReference()
        {
            var aold = CreateSimpleCiApplication("a", "old");
            var b1 = ReferencingCi.CreateApplication(aold, "b");
            var c1 = ReferenceToReferencingCi.CreateApplication("c", b1);
            var anew = CreateSimpleCiApplication("a", "new");
            var b2 = ReferencingCi.CreateApplication(anew, "b");
            var c2 = ReferenceToReferencingCi.CreateApplication("c", b2);

            var oldApplications = new List<ConceptApplication> { aold, c1 }; // A and C have database generator implementations, but B doesn't.
            var newApplications = new List<NewConceptApplication> { c2, anew };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(2, toBeRemoved.Count);
            Assert.IsTrue(toBeRemoved.IndexOf(c1) < toBeRemoved.IndexOf(aold));
            Assert.AreEqual(2, toBeInserted.Count);
            Assert.IsTrue(toBeInserted.IndexOf(anew) < toBeInserted.IndexOf(c2));
        }

        [TestMethod]
        public void ExtractCreateQueries()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetConceptApplicationSeparator(0));
            const string createQuery1 = "create query 1";
            sqlCodeBuilder.InsertCode(createQuery1);

            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetConceptApplicationSeparator(1));
            const string createQuery2 = "create query 2";
            sqlCodeBuilder.InsertCode(createQuery2);

            var ca1 = new NewConceptApplication(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation());
            var ca2 = new NewConceptApplication(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation());
            var newConceptApplications = new List<NewConceptApplication> { ca1, ca2 };
            DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications);

            Assert.AreEqual(createQuery1, ca1.CreateQuery);
            Assert.AreEqual(createQuery2, ca2.CreateQuery);
        }

        [TestMethod]
        public void ExtractCreateQueries_BeforeFirst1()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            sqlCodeBuilder.InsertCode("before first");

            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetConceptApplicationSeparator(0));
            const string createQuery1 = "create query 1";
            sqlCodeBuilder.InsertCode(createQuery1);

            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetConceptApplicationSeparator(1));
            const string createQuery2 = "create query 2";
            sqlCodeBuilder.InsertCode(createQuery2);

            var ca1 = new NewConceptApplication(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation());
            var ca2 = new NewConceptApplication(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation());
            var newConceptApplications = new List<NewConceptApplication> { ca1, ca2 };

            TestUtility.ShouldFail<FrameworkException>(
                () => DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications),
                "The first segment should be empty");
        }

        [TestMethod]
        public void ExtractCreateQueries_Empty()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            var newConceptApplications = new List<NewConceptApplication>();
            DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications);
        }

        [TestMethod]
        public void ExtractCreateQueries_BeforeFirst2()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            sqlCodeBuilder.InsertCode("before first");

            var newConceptApplications = new List<NewConceptApplication>();
            TestUtility.ShouldFail<FrameworkException>(
                () => DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications),
                "The first segment should be empty");
        }

        [TestMethod]
        public void GetConceptApplicationDependencies_SimpleTest()
        {
            IConceptInfo ci1 = new SimpleCi { Name = "1" };
            IConceptInfo ci2 = new SimpleCi { Name = "2" };
            ConceptApplication ca1a = new NewConceptApplication(ci1, new SimpleConceptImplementation()) { CreateQuery = "1a" };
            ConceptApplication ca1b = new NewConceptApplication(ci1, new SimpleConceptImplementation()) { CreateQuery = "1b" };
            ConceptApplication ca2a = new NewConceptApplication(ci2, new SimpleConceptImplementation()) { CreateQuery = "2a" };
            ConceptApplication ca2b = new NewConceptApplication(ci2, new SimpleConceptImplementation()) { CreateQuery = "2b" };

            IEnumerable<Tuple<IConceptInfo, IConceptInfo, string>> conceptInfoDependencies = new[] { Tuple.Create(ci2, ci1, "") };

            IEnumerable<Dependency> actual = DatabaseModelBuilderAccessor.GetConceptApplicationDependencies(conceptInfoDependencies, new[] { ca1a, ca1b, ca2a, ca2b });

            Assert.AreEqual("2a-1a, 2a-1b, 2b-1a, 2b-1b", String.Join(", ", actual.Select(Dump).OrderBy(s => s)));
        }

        private static string Dump(Dependency dependency)
        {
            return dependency.DependsOn.CreateQuery + "-" + dependency.Dependent.CreateQuery;
        }

        private class ExtendingConceptImplementation : IConceptDatabaseDefinition, IConceptDatabaseDefinitionExtension
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
            public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
            {
                createdDependencies = tempConceptInfoDependencies;
            }
        }

        private static IEnumerable<Tuple<IConceptInfo, IConceptInfo>> tempConceptInfoDependencies;

        [TestMethod]
        public void CreateNewApplications_MissingMiddleApplications()
        {
            IConceptInfo ci1 = new SimpleCi { Name = "1" }; // Concept application SimpleConceptImplementation will generate SQL script "create 1".
            IConceptInfo ci2 = new SimpleCi2 { Name = "2" }; // No concept application in database.
            IConceptInfo ci3 = new SimpleCi3 { Name = "3" }; // Concept application ExtendingConceptImplementation does not generate SQL script.

            var conceptImplementations = new PluginsMetadataList<IConceptDatabaseDefinition>
            {
                new NullImplementation(),
                { new SimpleConceptImplementation(), new Dictionary<string, object> { { "Implements", typeof(SimpleCi) } } },
                { new ExtendingConceptImplementation(), new Dictionary<string, object> { { "Implements", typeof(SimpleCi3) } } },
            };

            tempConceptInfoDependencies = new[] { Tuple.Create(ci2, ci1), Tuple.Create(ci3, ci2) };
            // Concept application that implements ci3 should (indirectly) depend on concept application that implements ci1.
            // This test is specific because there is no concept application for ci2, so there is possibility of error when calculating dependencies between concept applications.

            var dslModel = new MockDslModel(new[] { ci1, ci2, ci3 });
            IDatabaseModel databaseModel = new DatabaseModelBuilder(CreatePluginsContainer(conceptImplementations), dslModel, new ConsoleLogProvider());
            var conceptApplications = databaseModel.ConceptApplications;
            tempConceptInfoDependencies = null;

            var ca1 = conceptApplications.Where(ca => ca.ConceptInfo == ci1).Single();
            var ca3 = conceptApplications.Where(ca => ca.ConceptInfo == ci3).Single();

            Assert.IsTrue(DirectAndIndirectDependencies(ca1).Contains(ca3), "Concept application ca3 should be included in direct or indirect dependencies of ca1.");
        }

        private static IEnumerable<ConceptApplication> DirectAndIndirectDependencies(ConceptApplication ca)
        {
            return ca.DependsOn.Select(cad => cad.ConceptApplication)
                .Union(ca.DependsOn.Select(cad => cad.ConceptApplication)
                    .SelectMany(DirectAndIndirectDependencies));
        }

        [TestMethod]
        public void DatabaseGenerator_NoTransaction()
        {
            var oldApplications = new List<ConceptApplication> {
                    CreateBaseCiApplication("A", new SimpleConceptImplementation()),
                    CreateBaseCiApplication("B", new NoTransactionConceptImplementation()),
                    CreateBaseCiApplication("C", new SimpleConceptImplementation()) };
            var newApplications = new List<NewConceptApplication> {
                    CreateBaseCiApplication("D", new SimpleConceptImplementation()),
                    CreateBaseCiApplication("E", new NoTransactionConceptImplementation()),
                    CreateBaseCiApplication("F", new SimpleConceptImplementation()) };

            var sqlExecuter = TestDatabaseGenerator(oldApplications, newApplications).SqlExecuter;
            string executedSqlReport = string.Concat(sqlExecuter.ExecutedScriptsWithTransaction
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
