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
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private static CodeGenerator CreateBaseCiApplication(string name)
        {
            return new CodeGenerator(new BaseCi { Name = name }, new SimpleConceptImplementation());
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

            public static CodeGenerator CreateApplication(string name, CodeGenerator reference1, CodeGenerator reference2)
            {
                return new CodeGenerator(
                    new MultipleReferencingCi { Name = name, Reference1 = (BaseCi)reference1.ConceptInfo, Reference2 = (BaseCi)reference2.ConceptInfo },
                    new SimpleConceptImplementation());
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

            var conceptImplementations = new PluginsMetadataList<IConceptDatabaseDefinition>
            {
                new NullImplementation(),
                { new SimpleConceptImplementation(), typeof(SimpleCi) },
            };

            var all = new List<CodeGenerator> { a, b, c, r1, r2, r3, r4, r5 };
            var dependencies = new DatabaseModelDependencies(new ConsoleLogProvider())
                .ExtractCodeGeneratorDependencies(all, MockDatabasePluginsContainer.Create(conceptImplementations));

            string result = string.Join(" ", dependencies
                .Select(d => ((dynamic)d).DependsOn.ConceptInfo.Name + "<" + ((dynamic)d).Dependent.ConceptInfo.Name)
                .OrderBy(str => str));
            Console.WriteLine(result);

            Assert.AreEqual("1<3 2<3 5<4 A<1 A<3 B<1 B<2 B<3 C<2 C<3 C<4 C<5", result);
        }

        [TestMethod]
        public void ExtractCreateQueries()
        {
            var ca1 = new CodeGenerator(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation());
            const string createQuery1 = "create query 1";
            var ca2 = new CodeGenerator(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation());
            const string createQuery2 = "create query 2";

            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetCodeGeneratorSeparator(ca1.Id));
            sqlCodeBuilder.InsertCode(createQuery1);
            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetCodeGeneratorSeparator(ca2.Id));
            sqlCodeBuilder.InsertCode(createQuery2);

            var createQueries = DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode);
            Assert.AreEqual(
                $"{ca1.Id}:create query 1, {ca2.Id}:create query 2",
                TestUtility.DumpSorted(createQueries, cq => $"{cq.Key}:{cq.Value}"));
        }

        [TestMethod]
        public void ExtractCreateQueries_BeforeFirst1()
        {
            var ca1 = new CodeGenerator(new BaseCi { Name = "ci1" }, new SimpleConceptImplementation());
            const string createQuery1 = "create query 1";
            var ca2 = new CodeGenerator(new BaseCi { Name = "ci2" }, new SimpleConceptImplementation());
            const string createQuery2 = "create query 2";

            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            sqlCodeBuilder.InsertCode("before first");
            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetCodeGeneratorSeparator(0));
            sqlCodeBuilder.InsertCode(createQuery1);
            sqlCodeBuilder.InsertCode(DatabaseModelBuilderAccessor.GetCodeGeneratorSeparator(1));
            sqlCodeBuilder.InsertCode(createQuery2);

            TestUtility.ShouldFail<FrameworkException>(
                () => DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode),
                "The first segment should be empty");
        }

        [TestMethod]
        public void ExtractCreateQueries_Empty()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");

            var createQueries = DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode);
            Assert.AreEqual(
                "",
                TestUtility.DumpSorted(createQueries, cq => $"{cq.Key}:{cq.Value}"));
        }

        [TestMethod]
        public void ExtractCreateQueries_BeforeFirst2()
        {
            var sqlCodeBuilder = new CodeBuilder("/*", "*/");
            sqlCodeBuilder.InsertCode("before first");

            var newConceptApplications = new List<CodeGenerator>();
            TestUtility.ShouldFail<FrameworkException>(
                () => DatabaseModelBuilderAccessor.ExtractCreateQueries(sqlCodeBuilder.GeneratedCode),
                "The first segment should be empty");
        }

        [TestMethod]
        public void GetConceptApplicationDependencies_SimpleTest()
        {
            IConceptInfo ci1 = new SimpleCi { Name = "1" };
            IConceptInfo ci2 = new SimpleCi { Name = "2" };
            CodeGenerator ca1a = new CodeGenerator(ci1, new SimpleConceptImplementation());
            CodeGenerator ca1b = new CodeGenerator(ci1, new SimpleConceptImplementation());
            CodeGenerator ca2a = new CodeGenerator(ci2, new SimpleConceptImplementation());
            CodeGenerator ca2b = new CodeGenerator(ci2, new SimpleConceptImplementation());

            var names = new Dictionary<int, string>
            {
                { ca1a.Id, "1a" },
                { ca1b.Id, "1b" },
                { ca2a.Id, "2a" },
                { ca2b.Id, "2b" },
            };

            var conceptInfoDependencies = new[] { Tuple.Create(ci2, ci1) };

            var actual = new DatabaseModelDependencies(new ConsoleLogProvider())
                .ConceptDependencyToImplementationDependency(conceptInfoDependencies, new[] { ca1a, ca1b, ca2a, ca2b });

            Assert.AreEqual(
                "2a-1a, 2a-1b, 2b-1a, 2b-1b",
                string.Join(", ", actual
                    .Select(d => names[d.DependsOn.Id] + "-" + names[d.Dependent.Id])
                    .OrderBy(s => s)));
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
                { new SimpleConceptImplementation(), typeof(SimpleCi) },
                { new ExtendingConceptImplementation(), typeof(SimpleCi3) },
            };

            tempConceptInfoDependencies = new[] { Tuple.Create(ci2, ci1), Tuple.Create(ci3, ci2) };
            // Concept application that implements ci3 should (indirectly) depend on concept application that implements ci1.
            // This test is specific because there is no concept application for ci2, so there is possibility of error when calculating dependencies between concept applications.

            var dslModel = new MockDslModel(new[] { ci1, ci2, ci3 });
            var databasePlugins = MockDatabasePluginsContainer.Create(conceptImplementations);
            var databaseModelBuilder = new DatabaseModelBuilderAccessor(databasePlugins, dslModel);
            var conceptApplications = databaseModelBuilder.CreateDatabaseModel().ConceptApplications;

            tempConceptInfoDependencies = null;

            var ca1 = conceptApplications.Where(ca => ca.ConceptInfoKey == ci1.GetKey()).Single();
            var ca3 = conceptApplications.Where(ca => ca.ConceptInfoKey == ci3.GetKey()).Single();

            Assert.IsTrue(DirectAndIndirectDependencies(ca1).Contains(ca3), "Concept application ca3 should be included in direct or indirect dependencies of ca1.");
        }

        private static IEnumerable<ConceptApplication> DirectAndIndirectDependencies(ConceptApplication ca)
        {
            return ca.DependsOn
                .Union(ca.DependsOn.SelectMany(DirectAndIndirectDependencies));
        }
    }
}
