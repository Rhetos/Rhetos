/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Compiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Rhetos.Dsl;
using System.Linq;
using Rhetos.Extensibility;
using Rhetos.Factory;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.DatabaseGenerator;
using Autofac.Features.Metadata;
using System.Text;

namespace Rhetos.DatabaseGenerator.Test
{
    [TestClass]
    public class DatabaseGeneratorTest
    {
        #region Helper classes

        public class SimpleConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "CREATE SimpleConceptImplementation"; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        }

        public class DependentConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "CREATE DependentConceptImplementation"; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return ""; }
        }

        public class BaseCi : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            public override string ToString()
            {
                return string.Format("BASE {0}", Name);
            }

            public static NewConceptApplication CreateApplication(string name)
            {
                return new NewConceptApplication(new BaseCi { Name = name }, new SimpleConceptImplementation())
                {
                    CreateQuery = "sql",
                    DependsOn = new ConceptApplication[] { }
                };
            }

            public static NewConceptApplication CreateApplication(string name, IConceptDatabaseDefinition implementation)
            {
                var ca = CreateApplication(name);
                ca.ConceptImplementationType = implementation.GetType();

                return ca;
            }
        }

        public class OtherBaseCi : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            public override string ToString()
            {
                return string.Format("OTHER BASE {0}", Name);
            }

            public static NewConceptApplication CreateApplication(string name)
            {
                return CreateApplication(name, new SimpleConceptImplementation());
            }

            public static NewConceptApplication CreateApplication(string name, IConceptDatabaseDefinition implementation)
            {
                return new NewConceptApplication(new BaseCi { Name = name }, implementation)
                {
                    CreateQuery = "sql",
                    DependsOn = new ConceptApplication[] { }
                };
            }
        }

        public class SimpleCi : BaseCi
        {
            public string Data { get; set; }

            public override string ToString()
            {
                return string.Format("SIMPLE {0} {1}", Name, Data);
            }

            new public static NewConceptApplication CreateApplication(string sql)
            {
                return new NewConceptApplication(new SimpleCi { Name = "name", Data = "data" }, new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplication[] { }
                };
            }

            public static NewConceptApplication CreateApplication(string name, string sql)
            {
                return new NewConceptApplication(new SimpleCi { Name = name, Data = "data" }, new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplication[] { }
                };
            }

            new public static NewConceptApplication CreateApplication(string name, IConceptDatabaseDefinition implementation)
            {
                return new NewConceptApplication(new SimpleCi { Name = name, Data = "data" }, implementation)
                {
                    CreateQuery = "sql",
                    DependsOn = new ConceptApplication[] { }
                };
            }

            public static NewConceptApplication CreateApplication(string name, string sql, string data)
            {
                return new NewConceptApplication(new SimpleCi { Name = name, Data = data }, new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplication[] { }
                };
            }
        }

        public class SimpleCi2 : BaseCi
        {
            public string Data { get; set; }

            public override string ToString()
            {
                return string.Format("SIMPLE2 {0} {1}", Name, Data);
            }
        }

        public class SimpleCi3 : BaseCi
        {
            public string Data { get; set; }

            public override string ToString()
            {
                return string.Format("SIMPLE3 {0} {1}", Name, Data);
            }
        }

        public class ReferencingCi : IConceptInfo
        {
            [ConceptKey]
            public SimpleCi Reference { get; set; }
            public string Data { get; set; }

            public override string ToString()
            {
                return string.Format("DEPENDENT TO {0}-{1}", Reference.Name, Reference.Data);
            }

            public static NewConceptApplication CreateApplication(string sql, NewConceptApplication reference)
            {
                return new NewConceptApplication(
                    new ReferencingCi { Reference = (SimpleCi)(reference.ConceptInfo), Data = "data" },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplication[] { }
                };
            }
        }

        public class ReferenceToReferencingCi : IConceptInfo
        {
            [ConceptKey]
            public ReferencingCi Reference { get; set; }
            public string Data { get; set; }

            public override string ToString()
            {
                return string.Format("REFERENCE TO REFERENCE TO {0} {1}", Reference.Reference.Name, Reference.Reference.Data);
            }

            public static NewConceptApplication CreateApplication(string sql, NewConceptApplication reference)
            {
                return new NewConceptApplication(
                    new ReferenceToReferencingCi { Reference = (ReferencingCi)(reference.ConceptInfo), Data = "data" },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = sql,
                    DependsOn = new ConceptApplication[] { }
                };
            }
        }

        public class MultipleReferencingCi : BaseCi
        {
            public BaseCi Reference1 { get; set; }
            public BaseCi Reference2 { get; set; }

            public override string ToString()
            {
                return string.Format("MULTIREFERENCE {0}", Name);
            }

            public static NewConceptApplication CreateApplication(string name, NewConceptApplication reference1, NewConceptApplication reference2)
            {
                return new NewConceptApplication(
                    new MultipleReferencingCi { Name = name, Reference1 = (BaseCi)reference1.ConceptInfo, Reference2 = (BaseCi)reference2.ConceptInfo },
                    new SimpleConceptImplementation())
                {
                    CreateQuery = "sql",
                    DependsOn = new ConceptApplication[] { }
                };
            }
        }

        private static void TestDatabaseGenerator(
            IEnumerable<ConceptApplication> oldApplications,
            IEnumerable<NewConceptApplication> newApplications,
            out List<ConceptApplication> toBeRemoved,
            out List<NewConceptApplication> toBeInserted)
        {

            var plugins = new PluginsContainer<IConceptDatabaseDefinition>(new []
            {
                new Meta<IConceptDatabaseDefinition>(new NullImplementation(), new Dictionary<string, object> { }),
                new Meta<IConceptDatabaseDefinition>(new SimpleConceptImplementation(), new Dictionary<string, object> { }),
                new Meta<IConceptDatabaseDefinition>(new DependentConceptImplementation(), new Dictionary<string, object> { })
            });
            var databaseGenerator = new DatabaseGenerator_Accessor(null, plugins);

            databaseGenerator.ComputeDependsOn(oldApplications.Cast<NewConceptApplication>());
            databaseGenerator.ComputeDependsOn(newApplications);

            DatabaseGenerator_Accessor.CalculateApplicationsToBeRemovedAndInserted(
                oldApplications, newApplications,
                out toBeRemoved, out toBeInserted,
                new ConsoleLogger());
            DirectedGraph.TopologicalSort(toBeRemoved, DatabaseGenerator_Accessor.GetDependencyPairs(oldApplications));
            toBeRemoved.Reverse();
            DirectedGraph.TopologicalSort(toBeInserted, DatabaseGenerator_Accessor.GetDependencyPairs(newApplications));
        }

        #endregion

        //============================================================================

        [TestMethod]
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
        public void DatabaseGenerator_MustRecreateDependentConceptWithNewConceptInfoReference()
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
            Assert.AreEqual(2, toBeInserted.Count);
        }

        [TestMethod]
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
            var dependencies = DatabaseGenerator_Accessor.ExtractDependenciesFromConceptInfos(all);

            string result = string.Join(" ", dependencies
                .Select(d => ((dynamic)d).DependsOn.ConceptInfo.Name + "<" + ((dynamic)d).Dependent.ConceptInfo.Name)
                .OrderBy(str=>str));
            Console.WriteLine(result);

            Assert.AreEqual("1<3 2<3 5<4 A<1 A<3 B<1 B<2 B<3 C<2 C<3 C<4 C<5", result);
        }

        [TestMethod]
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
        public void ExtractCreateQueries()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            sqlCodeBuilder.InsertCode("before first");

            var ca1 = new NewConceptApplication(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation())
            {
                CreateQuery = "sql",
                DependsOn = new ConceptApplication[] { }
            };
            DatabaseGenerator_Accessor.AddConceptApplicationSeparator(ca1, sqlCodeBuilder);
            const string createQuery1 = "create query 1";
            sqlCodeBuilder.InsertCode(createQuery1);

            var ca2 = new NewConceptApplication(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation())
            {
                CreateQuery = "sql",
                DependsOn = new ConceptApplication[] { }
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
        public void ExtractCreateQueries_Empty1()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            var toBeInserted = new List<ConceptApplication>();
            DatabaseGenerator_Accessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, toBeInserted);
        }

        [TestMethod]
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
        public void ExtractCreateQueries_Empty2()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            sqlCodeBuilder.InsertCode("before first");

            var toBeInserted = new List<ConceptApplication>();
            DatabaseGenerator_Accessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, toBeInserted);
        }

        [TestMethod]
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
        public void GetConceptApplicationDependencies_SimpleTest()
        {
            IConceptInfo ci1 = new SimpleCi { Name = "1" };
            IConceptInfo ci2 = new SimpleCi { Name = "2" };
            ConceptApplication ca1a = new NewConceptApplication(ci1, new SimpleConceptImplementation()) { CreateQuery = "1a" };
            ConceptApplication ca1b = new NewConceptApplication(ci1, new SimpleConceptImplementation()) { CreateQuery = "1b" };
            ConceptApplication ca2a = new NewConceptApplication(ci2, new SimpleConceptImplementation()) { CreateQuery = "2a" };
            ConceptApplication ca2b = new NewConceptApplication(ci2, new SimpleConceptImplementation()) { CreateQuery = "2b" };

            IEnumerable<Tuple<IConceptInfo, IConceptInfo>> conceptInfoDependencies = new[] { Tuple.Create(ci2, ci1) };

            IEnumerable<Dependency> actual = DatabaseGenerator_Accessor.GetConceptApplicationDependencies(conceptInfoDependencies, new[] { ca1a, ca1b, ca2a, ca2b });

            Assert.AreEqual("2a-1a, 2a-1b, 2b-1a, 2b-1b", String.Join(", ", actual.Select(Dump).OrderBy(s => s)));
        }

        private static string Dump(Dependency dependency)
        {
            return dependency.DependsOn.CreateQuery + "-" + dependency.Dependent.CreateQuery;
        }

        class MockDslModel : IDslModel
        {
            private readonly IEnumerable<IConceptInfo> _conceptInfos;
            public MockDslModel(IEnumerable<IConceptInfo> conceptInfos) { _conceptInfos = conceptInfos; }
            public IEnumerable<IConceptInfo> Concepts { get { return _conceptInfos; } }
            public IConceptInfo FindByKey(string conceptKey) { throw new NotImplementedException(); }
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
        [DeploymentItem("Rhetos.DatabaseGenerator.dll")]
        public void CreateNewApplications_MissingMiddleApplications()
        {
            IConceptInfo ci1 = new SimpleCi { Name = "1" };
            IConceptInfo ci2 = new SimpleCi2 { Name = "2" };
            IConceptInfo ci3 = new SimpleCi3 { Name = "3" };

            tempConceptInfoDependencies = new[] { Tuple.Create(ci2, ci1), Tuple.Create(ci3, ci2) };
            // Concept application that implements ci3 should (indirectly) depend on concept application that implements ci1.
            // This test is specific because there is no concept application for ci2, so there is possibility of error when calculating dependencies between concept applications.

            var dslModel = new MockDslModel(new[] { ci1, ci2, ci3 });

            var plugins = new PluginsContainer<IConceptDatabaseDefinition>(new Meta<IConceptDatabaseDefinition>[]
                {
                    new Meta<IConceptDatabaseDefinition>(new NullImplementation(), new Dictionary<string, object> { }),
                    new Meta<IConceptDatabaseDefinition>(new SimpleConceptImplementation(), new Dictionary<string, object> { { "Implements", typeof(SimpleCi) } }),
                    new Meta<IConceptDatabaseDefinition>(new ExtendingConceptImplementation(), new Dictionary<string, object> { { "Implements", typeof(SimpleCi3) } })
                });

            DatabaseGenerator_Accessor databaseGenerator = new DatabaseGenerator_Accessor(dslModel, plugins);
            var createdApplications = databaseGenerator.CreateNewApplications();
            tempConceptInfoDependencies = null;

            var ca1 = createdApplications.Where(ca => ca.ConceptInfo == ci1).Single();
            var ca3 = createdApplications.Where(ca => ca.ConceptInfo == ci3).Single();

            Assert.IsTrue(DirectAndIndirectDependencies(ca1).Contains(ca3), "Concept application ca3 should be included in direct or indirect depedencies of ca1.");
        }

        private static IEnumerable<ConceptApplication> DirectAndIndirectDependencies(ConceptApplication ca)
        {
            return ca.DependsOn.Union(ca.DependsOn.SelectMany(DirectAndIndirectDependencies));
        }
    }
}
