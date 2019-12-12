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
    public class DatabaseModelBuilderTest
    {
        #region Helper classes

        private class SimpleConceptImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) { return "create " + ((BaseCi)conceptInfo).Name; }
            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) { return "remove " + ((BaseCi)conceptInfo).Name; }
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

        private class SimpleCi2 : BaseCi
        {
            public string Data { get; set; }
        }

        private class SimpleCi3 : BaseCi
        {
            public string Data { get; set; }
        }

        private class MultipleReferencingCi : BaseCi
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

        #endregion Helper classes

        //============================================================================

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
            var dependencies = DatabaseModelGeneratorAccessor.ExtractDependenciesFromConceptInfos(all);

            string result = string.Join(" ", dependencies
                .Select(d => ((dynamic)d).DependsOn.ConceptInfo.Name + "<" + ((dynamic)d).Dependent.ConceptInfo.Name)
                .OrderBy(str => str));
            Console.WriteLine(result);

            Assert.AreEqual("1<3 2<3 5<4 A<1 A<3 B<1 B<2 B<3 C<2 C<3 C<4 C<5", result);
        }

        [TestMethod]
        public void ExtractCreateQueries()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            sqlCodeBuilder.InsertCode(DatabaseModelGeneratorAccessor.GetConceptApplicationSeparator(0));
            const string createQuery1 = "create query 1";
            sqlCodeBuilder.InsertCode(createQuery1);

            sqlCodeBuilder.InsertCode(DatabaseModelGeneratorAccessor.GetConceptApplicationSeparator(1));
            const string createQuery2 = "create query 2";
            sqlCodeBuilder.InsertCode(createQuery2);

            var ca1 = new NewConceptApplication(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation());
            var ca2 = new NewConceptApplication(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation());
            var newConceptApplications = new List<NewConceptApplication> { ca1, ca2 };
            DatabaseModelGeneratorAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications);

            Assert.AreEqual(createQuery1, ca1.CreateQuery);
            Assert.AreEqual(createQuery2, ca2.CreateQuery);
        }

        [TestMethod]
        public void ExtractCreateQueries_BeforeFirst1()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            sqlCodeBuilder.InsertCode("before first");

            sqlCodeBuilder.InsertCode(DatabaseModelGeneratorAccessor.GetConceptApplicationSeparator(0));
            const string createQuery1 = "create query 1";
            sqlCodeBuilder.InsertCode(createQuery1);

            sqlCodeBuilder.InsertCode(DatabaseModelGeneratorAccessor.GetConceptApplicationSeparator(1));
            const string createQuery2 = "create query 2";
            sqlCodeBuilder.InsertCode(createQuery2);

            var ca1 = new NewConceptApplication(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation());
            var ca2 = new NewConceptApplication(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation());
            var newConceptApplications = new List<NewConceptApplication> { ca1, ca2 };

            TestUtility.ShouldFail<FrameworkException>(
                () => DatabaseModelGeneratorAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications),
                "The first segment should be empty");
        }

        [TestMethod]
        public void ExtractCreateQueries_Empty()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            var newConceptApplications = new List<NewConceptApplication>();
            DatabaseModelGeneratorAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications);
        }

        [TestMethod]
        public void ExtractCreateQueries_BeforeFirst2()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            sqlCodeBuilder.InsertCode("before first");

            var newConceptApplications = new List<NewConceptApplication>();
            TestUtility.ShouldFail<FrameworkException>(
                () => DatabaseModelGeneratorAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode, newConceptApplications),
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

            IEnumerable<Dependency> actual = DatabaseModelGeneratorAccessor.GetConceptApplicationDependencies(conceptInfoDependencies, new[] { ca1a, ca1b, ca2a, ca2b });

            Assert.AreEqual("2a-1a, 2a-1b, 2b-1a, 2b-1b", string.Join(", ", actual.Select(Dump).OrderBy(s => s)));
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
            var databasePlugins = MockDatabasePluginsContainer.Create(conceptImplementations);
            var databaseModelGenerator = new DatabaseModelGeneratorAccessor(databasePlugins, dslModel);
            var conceptApplications = databaseModelGenerator.CreateNewApplications();
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
    }
}
