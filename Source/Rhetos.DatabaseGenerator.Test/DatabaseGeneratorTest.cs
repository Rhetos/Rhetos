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

namespace Rhetos.DatabaseGenerator.Test
{
    [TestClass]
    public class DatabaseGeneratorTest
    {
        #region Helper classes

        public class SimpleConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "create "+ ((BaseCi)conceptInfo).Name; }
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

            public static NewConceptApplication CreateApplication(string name)
            {
                return CreateApplication(name, new SimpleConceptImplementation());
            }

            public static NewConceptApplication CreateApplication(string name, IConceptDatabaseDefinition implementation)
            {
                var conceptInfo = new BaseCi { Name = name };
                return new NewConceptApplication(conceptInfo, implementation)
                {
                    CreateQuery = implementation.CreateDatabaseStructure(conceptInfo),
                    RemoveQuery = implementation.RemoveDatabaseStructure(conceptInfo),
                    DependsOn = new ConceptApplicationDependency[] { },
                    ConceptImplementationType = implementation.GetType(),
                };
            }
        }

        public class OtherBaseCi : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            public static NewConceptApplication CreateApplication(string name)
            {
                return CreateApplication(name, new SimpleConceptImplementation());
            }

            public static NewConceptApplication CreateApplication(string name, IConceptDatabaseDefinition implementation)
            {
                return new NewConceptApplication(new BaseCi { Name = name }, implementation)
                {
                    CreateQuery = "sql",
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }
        }

        public class SimpleCi : BaseCi
        {
            public string Data { get; set; }

            new public static NewConceptApplication CreateApplication(string sql)
            {
                return new NewConceptApplication(new SimpleCi { Name = "name", Data = "data" }, new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }

            public static NewConceptApplication CreateApplication(string name, string sql)
            {
                return new NewConceptApplication(new SimpleCi { Name = name, Data = "data" }, new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }

            public static NewConceptApplication CreateApplication(string name, string sql, ConceptApplication dependsOn)
            {
                return new NewConceptApplication(new SimpleCi { Name = name, Data = "data" }, new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new [] { new ConceptApplicationDependency { ConceptApplication = dependsOn } }
                };
            }

            new public static NewConceptApplication CreateApplication(string name, IConceptDatabaseDefinition implementation)
            {
                return new NewConceptApplication(new SimpleCi { Name = name, Data = "data" }, implementation)
                {
                    CreateQuery = "sql",
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }

            public static NewConceptApplication CreateApplication(string name, string sql, string data)
            {
                return new NewConceptApplication(new SimpleCi { Name = name, Data = data }, new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplicationDependency[] { }
                };
            }
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

            public static NewConceptApplication CreateApplication(string sql, NewConceptApplication reference)
            {
                return new NewConceptApplication(
                    new ReferencingCi { Reference = (SimpleCi)(reference.ConceptInfo), Data = "data" },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
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

        public static PluginsContainer<IConceptDatabaseDefinition> CreatePluginsContainer(PluginsMetadataList<IConceptDatabaseDefinition> pluginsWithMedata)
        {
            Lazy<IEnumerable<IConceptDatabaseDefinition>> plugins = new Lazy<IEnumerable<IConceptDatabaseDefinition>>(() =>
                pluginsWithMedata.Select(pm => pm.Item1));
            Lazy<IEnumerable<Meta<IConceptDatabaseDefinition>>> pluginsWithMetadata = new Lazy<IEnumerable<Meta<IConceptDatabaseDefinition>>>(() =>
                pluginsWithMedata.Select(pm => new Meta<IConceptDatabaseDefinition>(pm.Item1, pm.Item2)));
            Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>> pluginsByImplementation = new Lazy<IIndex<Type, IEnumerable<IConceptDatabaseDefinition>>>(() =>
                new StubIndex<IConceptDatabaseDefinition>(pluginsWithMedata));

            return new PluginsContainer<IConceptDatabaseDefinition>(plugins, pluginsByImplementation, new PluginsMetadataCache<IConceptDatabaseDefinition>(pluginsWithMetadata, new StubIndex<SuppressPlugin>()));
        }

        private static void TestDatabaseGenerator(
            IEnumerable<ConceptApplication> oldApplications,
            IEnumerable<NewConceptApplication> newApplications,
            out List<ConceptApplication> toBeRemoved,
            out List<NewConceptApplication> toBeInserted,
            bool computeDependencies = true)
        {
            var plugins = CreatePluginsContainer(new PluginsMetadataList<IConceptDatabaseDefinition>
                {
                    new NullImplementation(),
                    new SimpleConceptImplementation(),
                    new DependentConceptImplementation()
                });
            var databaseGenerator = new DatabaseGenerator_Accessor(null, plugins);

            if (computeDependencies)
            {
                databaseGenerator.ComputeDependsOn(oldApplications.Cast<NewConceptApplication>());
                databaseGenerator.ComputeDependsOn(newApplications);
            }

            databaseGenerator.CalculateApplicationsToBeRemovedAndInserted(
                oldApplications, newApplications,
                out toBeRemoved, out toBeInserted);
            Graph.TopologicalSort(toBeRemoved, DatabaseGenerator_Accessor.GetDependencyPairs(oldApplications));
            toBeRemoved.Reverse();
            Graph.TopologicalSort(toBeInserted, DatabaseGenerator_Accessor.GetDependencyPairs(newApplications));
        }

        #endregion

        //============================================================================

        [TestMethod]
        public void DatabaseGenerator_NoChange()
        {
            var oldApplications = new List<ConceptApplication> { SimpleCi.CreateApplication("unchanged") };
            var newApplications = new List<NewConceptApplication> { SimpleCi.CreateApplication("unchanged") };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(0, toBeRemoved.Count);
            Assert.AreEqual(0, toBeInserted.Count);
        }

        [TestMethod]
        public void DatabaseGenerator_SimpleChange()
        {
            var oldApplications = new List<ConceptApplication> { SimpleCi.CreateApplication("A", "old"), SimpleCi.CreateApplication("B", "unchanged") };
            var newApplications = new List<NewConceptApplication> { SimpleCi.CreateApplication("B", "unchanged"), SimpleCi.CreateApplication("A", "new") };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(1, toBeRemoved.Count);
            Assert.AreEqual(1, toBeInserted.Count);
            Assert.AreEqual(SimpleCi.CreateApplication("A", "old").ConceptInfo.GetFullDescription(), ((NewConceptApplication)toBeRemoved[0]).ConceptInfo.GetFullDescription());
            Assert.AreEqual(SimpleCi.CreateApplication("A", "new").ConceptInfo.GetFullDescription(), toBeInserted[0].ConceptInfo.GetFullDescription());
        }

        [TestMethod]
        public void DatabaseGenerator_MustRecreateDependentConcept()
        {
            var simpleV1 = SimpleCi.CreateApplication("v1");
            var simpleV2 = SimpleCi.CreateApplication("v2");
            var dependent = ReferencingCi.CreateApplication("", simpleV1);

            var oldApplications = new List<ConceptApplication> { simpleV1, dependent };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependent };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(2, toBeRemoved.Count);
            Assert.IsTrue(toBeRemoved.Contains(simpleV1));
            Assert.IsTrue(toBeRemoved.Contains(dependent));
            Assert.AreEqual(2, toBeInserted.Count);
            Assert.IsTrue(toBeInserted.Contains(simpleV2));
            Assert.IsTrue(toBeInserted.Contains(dependent));
        }

        [TestMethod]
        public void DatabaseGenerator_RecreatedDependentConceptReferencesNewVersion()
        {
            var simpleV1 = SimpleCi.CreateApplication("name", "v1", "data1");
            var simpleV2 = SimpleCi.CreateApplication("name", "v2", "data2");
            var dependentV1Unchanged = ReferencingCi.CreateApplication("", simpleV1);
            var dependentV2Unchanged = ReferencingCi.CreateApplication("", simpleV2);
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
            var simpleC1 = SimpleCi.CreateApplication("same", new SimpleConceptImplementation());
            var simpleC2 = SimpleCi.CreateApplication("same", new DependentConceptImplementation());
            var dependentC1Unchanged = ReferencingCi.CreateApplication("", simpleC1);
            var dependentC2Unchanged = ReferencingCi.CreateApplication("", simpleC2);
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
            var simple1a = SimpleCi.CreateApplication("a", "sqla1");
            var simple1b = SimpleCi.CreateApplication("b", "sqlb");
            var simple2a = SimpleCi.CreateApplication("a", "sqla2"); // Object modified in version 2.
            var simple2b = SimpleCi.CreateApplication("b", "sqlb", simple2a); // Adding a dependency that did not exist in the version 1.

            var oldApplications = new List<ConceptApplication> { simple1a, simple1b};
            var newApplications = new List<NewConceptApplication> { simple2a, simple2b };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted, computeDependencies: false);

            Assert.AreEqual(2, toBeRemoved.Count);
            Assert.AreEqual(2, toBeInserted.Count);
        }

        [TestMethod]
        public void DatabaseGenerator_MustRecreateDependentConceptInCorrectOrder()
        {
            var simpleV1 = SimpleCi.CreateApplication("v1");
            var simpleV2 = SimpleCi.CreateApplication("v2");
            var dependent = ReferencingCi.CreateApplication("", simpleV1);
            var secondReference1 = ReferenceToReferencingCi.CreateApplication("1", dependent);
            var secondReference2 = ReferenceToReferencingCi.CreateApplication("2", dependent);

            var oldApplications = new List<ConceptApplication> { simpleV1, dependent, secondReference1 };
            var newApplications = new List<NewConceptApplication> { simpleV2, dependent, secondReference2 };

            List<ConceptApplication> toBeRemoved;
            List<NewConceptApplication> toBeInserted;
            TestDatabaseGenerator(oldApplications, newApplications, out toBeRemoved, out toBeInserted);

            Assert.AreEqual(3, toBeRemoved.Count);
            Assert.IsTrue(toBeRemoved.IndexOf(dependent) < toBeRemoved.IndexOf(simpleV1));
            Assert.IsTrue(toBeRemoved.IndexOf(secondReference1) < toBeRemoved.IndexOf(dependent));
            Assert.AreEqual(3, toBeInserted.Count);
            Assert.IsTrue(toBeInserted.IndexOf(dependent) > toBeInserted.IndexOf(simpleV2));
            Assert.IsTrue(toBeInserted.IndexOf(secondReference2) > toBeInserted.IndexOf(dependent));
        }

        [TestMethod]
        public void ExtractDependenciesFromConceptInfosTest()
        {
            var a = BaseCi.CreateApplication("A");
            var b = BaseCi.CreateApplication("B");
            var c = BaseCi.CreateApplication("C");
            var r1 = MultipleReferencingCi.CreateApplication("1", a, b);
            var r2 = MultipleReferencingCi.CreateApplication("2", b, c);
            var r3 = MultipleReferencingCi.CreateApplication("3", r1, r2);
            var r5 = MultipleReferencingCi.CreateApplication("5", c, c);
            var r4 = MultipleReferencingCi.CreateApplication("4", c, r5);

            var all = new List<NewConceptApplication> { a, b, c, r1, r2, r3, r4, r5 };
            var dependencies = new DatabaseGenerator_Accessor().ExtractDependenciesFromConceptInfos(all);

            string result = string.Join(" ", dependencies
                .Select(d => ((dynamic)d).DependsOn.ConceptInfo.Name + "<" + ((dynamic)d).Dependent.ConceptInfo.Name)
                .OrderBy(str => str));
            Console.WriteLine(result);

            Assert.AreEqual("1<3 2<3 5<4 A<1 A<3 B<1 B<2 B<3 C<2 C<3 C<4 C<5", result);
        }

        [TestMethod]
        public void DatabaseGenerator_UnchangedMiddleReference()
        {
            var aold = SimpleCi.CreateApplication("a", "old");
            var b1 = ReferencingCi.CreateApplication("b", aold);
            var c1 = ReferenceToReferencingCi.CreateApplication("c", b1);
            var anew = SimpleCi.CreateApplication("a", "new");
            var b2 = ReferencingCi.CreateApplication("b", anew);
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
            sqlCodeBuilder.InsertCode("before first");

            var ca1 = new NewConceptApplication(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation())
            {
                Id = Guid.NewGuid(),
                CreateQuery = "sql",
                DependsOn = new ConceptApplicationDependency[] { }
            };
            DatabaseGenerator_Accessor.AddConceptApplicationSeparator(ca1, sqlCodeBuilder);
            const string createQuery1 = "create query 1";
            sqlCodeBuilder.InsertCode(createQuery1);

            var ca2 = new NewConceptApplication(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation())
            {
                Id = Guid.NewGuid(),
                CreateQuery = "sql",
                DependsOn = new ConceptApplicationDependency[] { }
            };
            DatabaseGenerator_Accessor.AddConceptApplicationSeparator(ca2, sqlCodeBuilder);
            const string createQuery2 = "create query 2";
            sqlCodeBuilder.InsertCode(createQuery2);

            var toBeInserted = new List<ConceptApplication> { ca1, ca2 };
            DatabaseGenerator_Accessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, toBeInserted);

            Assert.AreEqual(createQuery1, ca1.CreateQuery);
            Assert.AreEqual(createQuery2, ca2.CreateQuery);
        }

        [TestMethod]
        public void ExtractCreateQueries_Empty1()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            var toBeInserted = new List<ConceptApplication>();
            DatabaseGenerator_Accessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, toBeInserted);
        }

        [TestMethod]
        public void ExtractCreateQueries_Empty2()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            sqlCodeBuilder.InsertCode("before first");

            var toBeInserted = new List<ConceptApplication>();
            DatabaseGenerator_Accessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, toBeInserted);
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

            IEnumerable<Dependency> actual = DatabaseGenerator_Accessor.GetConceptApplicationDependencies(conceptInfoDependencies, new[] { ca1a, ca1b, ca2a, ca2b });

            Assert.AreEqual("2a-1a, 2a-1b, 2b-1a, 2b-1b", String.Join(", ", actual.Select(Dump).OrderBy(s => s)));
        }

        private static string Dump(Dependency dependency)
        {
            return dependency.DependsOn.CreateQuery + "-" + dependency.Dependent.CreateQuery;
        }

        class MockDslModel : IDslModel
        {
            public MockDslModel(IEnumerable<IConceptInfo> conceptInfos) { Concepts = conceptInfos; }
            public IEnumerable<IConceptInfo> Concepts { get; private set; }
            public IConceptInfo FindByKey(string conceptKey) { throw new NotImplementedException(); }
            public T GetIndex<T>() where T : IDslModelIndex
            {
                throw new NotImplementedException();
            }
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
            IConceptInfo ci1 = new SimpleCi { Name = "1" };
            IConceptInfo ci2 = new SimpleCi2 { Name = "2" };
            IConceptInfo ci3 = new SimpleCi3 { Name = "3" };

            tempConceptInfoDependencies = new[] { Tuple.Create(ci2, ci1), Tuple.Create(ci3, ci2) };
            // Concept application that implements ci3 should (indirectly) depend on concept application that implements ci1.
            // This test is specific because there is no concept application for ci2, so there is possibility of error when calculating dependencies between concept applications.

            var dslModel = new MockDslModel(new[] { ci1, ci2, ci3 });

            var plugins = CreatePluginsContainer(new PluginsMetadataList<IConceptDatabaseDefinition>
            {
                new NullImplementation(),
                { new SimpleConceptImplementation(), new Dictionary<string, object> { { "Implements", typeof(SimpleCi) } } },
                { new ExtendingConceptImplementation(), new Dictionary<string, object> { { "Implements", typeof(SimpleCi3) } } }
            });

            DatabaseGenerator_Accessor databaseGenerator = new DatabaseGenerator_Accessor(dslModel, plugins);
            var createdApplications = databaseGenerator.CreateNewApplications(new List<ConceptApplication>());
            tempConceptInfoDependencies = null;

            var ca1 = createdApplications.Where(ca => ca.ConceptInfo == ci1).Single();
            var ca3 = createdApplications.Where(ca => ca.ConceptInfo == ci3).Single();

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
                BaseCi.CreateApplication("A", new SimpleConceptImplementation()),
                BaseCi.CreateApplication("B", new NoTransactionConceptImplementation()),
                BaseCi.CreateApplication("C", new SimpleConceptImplementation()) };
            var newApplications = new List<NewConceptApplication> {
                BaseCi.CreateApplication("D", new SimpleConceptImplementation()),
                BaseCi.CreateApplication("E", new NoTransactionConceptImplementation()),
                BaseCi.CreateApplication("F", new SimpleConceptImplementation()) };

            var sqlExecuter = new MockSqlExecuter();
            var sqlTransactionBatches = new SqlTransactionBatches(sqlExecuter, new NullConfiguration(), new ConsoleLogProvider());
            var databaseGenerator = new DatabaseGenerator_Accessor(sqlTransactionBatches);
            databaseGenerator.ApplyChangesToDatabase(oldApplications, newApplications, oldApplications, newApplications);

            var executedSql = TestUtility.Dump(sqlExecuter.executedScriptsWithTransaction, scripts =>
                    (scripts.Item2 ? "TRAN" : "NOTRAN") + ": " + string.Join(", ", scripts.Item1));

            executedSql = ClearSqlForReport(executedSql);

            Assert.AreEqual(
                "TRAN: remove C, NOTRAN: remove B, TRAN: remove A, create D, NOTRAN: create E, TRAN: create F",
                executedSql);
        }

        private string ClearSqlForReport(string sql)
        {
            Console.WriteLine("ClearSqlForReport: " + sql);
            return sql
                .Replace(SqlUtility.NoTransactionTag, "");
        }

        class MockSqlExecuter : ISqlExecuter
        {
            public List<Tuple<List<string>, bool>> executedScriptsWithTransaction = new List<Tuple<List<string>, bool>>();

            public void ExecuteReader(string command, Action<System.Data.Common.DbDataReader> action)
            {
                throw new NotImplementedException();
            }

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction)
            {
                ExecuteSql(commands, useTransaction, null, null);
            }

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction, Action<int> beforeExecute, Action<int> afterExecute)
            {
                executedScriptsWithTransaction.Add(Tuple.Create(commands.ToList(), useTransaction));
            }
        }
    }

    internal class NullConfiguration : IConfiguration
    {
        public Lazy<bool> GetBool(string key, bool defaultValue)
        {
            return new Lazy<bool>(() => defaultValue);
        }

        public Lazy<T> GetEnum<T>(string key, T defaultValue) where T : struct
        {
            return new Lazy<T>(() => defaultValue);
        }

        public Lazy<int> GetInt(string key, int defaultValue)
        {
            return new Lazy<int>(() => defaultValue);
        }

        public Lazy<string> GetString(string key, string defaultValue)
        {
            return new Lazy<string>(() => defaultValue);
        }
    }
}
