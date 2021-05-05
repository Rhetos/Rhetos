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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Extensibility;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class DslModelTest
    {
        #region Sample concept classes

        [ConceptKeyword("SIMPLE")]
        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public string Data { get; set; }

            public SimpleConceptInfo() { }
            public SimpleConceptInfo(string name, string data)
            {
                Name = name;
                Data = data;
            }
        }

        class DerivedConceptInfo : SimpleConceptInfo
        {
            public string Extra { get; set; }

            public DerivedConceptInfo(string name, string data, string extra)
                : base(name, data)
            {
                Extra = extra;
            }
        }

        [ConceptKeyword("REF")]
        class RefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public SimpleConceptInfo Reference { get; set; }

            public RefConceptInfo() { }
            public RefConceptInfo(string name, SimpleConceptInfo reference)
            {
                Name = name;
                Reference = reference;
            }
        }

        #endregion

        internal class StubDslParser : IDslParser
        {
            private readonly IEnumerable<IConceptInfo> _rawConcepts;
            public StubDslParser(IEnumerable<IConceptInfo> rawConcepts) { _rawConcepts = rawConcepts; }
            public IEnumerable<IConceptInfo> ParsedConcepts { get { return _rawConcepts; } }
        }

        internal class StubMacroIndex : IIndex<Type, IEnumerable<IConceptMacro>>
        {
            public bool TryGetValue(Type key, out IEnumerable<IConceptMacro> value)
            {
                value = new IConceptMacro[] { };
                return true;
            }

            public IEnumerable<IConceptMacro> this[Type key]
            {
                get { return new IConceptMacro[] { }; }
            }
        }

        internal class StubMacroOrderRepository : IMacroOrderRepository
        {
            public List<MacroOrder> Load() { return new List<MacroOrder>(); }
            public void Save(IEnumerable<MacroOrder> macroOrders) { }
        }

        internal class StubDslModelFile : IDslModelFile
        {
            public void SaveConcepts(IEnumerable<IConceptInfo> concepts) { }
        }

        static IDslModel NewDslModel(IDslParser parser, IEnumerable<IConceptInfo> conceptPrototypes)
        {
            var dslContainter = new DslContainer(new ConsoleLogProvider(), new MockPluginsContainer<IDslModelIndex>(new DslModelIndexByType()));
            var dslModel = new DslModel(
                parser,
                new ConsoleLogProvider(),
                dslContainter,
                new StubMacroIndex(),
                new IConceptMacro[] { },
                conceptPrototypes,
                new StubMacroOrderRepository(),
                new StubDslModelFile(),
                new BuildOptions());
            return dslModel;
        }

        static List<IConceptInfo> DslModelFromConcepts(IEnumerable<IConceptInfo> rawConcepts)
        {
            var dslModel = NewDslModel(new StubDslParser(rawConcepts), rawConcepts);
            return dslModel.Concepts.ToList();
        }

        static List<IConceptInfo> DslModelFromScript(string dsl, IConceptInfo[] conceptInfoPluginsForGenericParser)
        {
            var nullDslParser = new DslParser(
                new TestTokenizer(dsl),
                new DslGrammar(conceptInfoPluginsForGenericParser, new ConsoleLogProvider()),
                new ConsoleLogProvider(),
                new BuildOptions());
            Console.WriteLine("Parsed concepts:");
            Console.WriteLine(string.Join(Environment.NewLine, nullDslParser.ParsedConcepts.Select(ci => " - " + ci.GetShortDescription())));

            var dslModel = NewDslModel(nullDslParser, conceptInfoPluginsForGenericParser);
            return dslModel.Concepts.ToList();
        }


        //=========================================================================

        public static string GetReport(IEnumerable<IConceptInfo> concepts)
        {
            // Removes build version from the InitializationConcept.
            return TestUtility.DumpSorted(concepts, c => c is InitializationConcept ? "InitializationConcept" : c.GetUserDescription());
        }

        [TestMethod]
        public void OrganizeConceptsByKey_RemoveDuplicate()
        {
            var concepts = new List<IConceptInfo> { new SimpleConceptInfo("a", "aaa"), new SimpleConceptInfo("a", "aaa") };
            var newConcepts = DslModelFromConcepts(concepts);
            Assert.AreEqual("InitializationConcept, SIMPLE a", GetReport(newConcepts));
        }

        [TestMethod]
        [ExpectedException(typeof(DslSyntaxException))]
        public void OrganizeConceptsByKey_ErrorIfDuplicatesNotEqual()
        {
            try
            {
                var concepts = new List<IConceptInfo> { new SimpleConceptInfo("aaa", "xxx"), new SimpleConceptInfo("aaa", "bbb") };
                DslModelFromConcepts(concepts);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("aaa"));
                Assert.IsTrue(ex.Message.Contains("xxx"));
                Assert.IsTrue(ex.Message.Contains("bbb"));
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DslSyntaxException))]
        public void OrganizeConceptsByKey_RecursiveConceptErrorIfDuplicatesNotEqual()
        {
            try
            {
                const string dsl = "menu abc { menu abc; }";
                DslModelFromScript(dsl, new[] { new GenericParserTest.MenuCI(), new GenericParserTest.SubMenuCI() });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Assert.IsTrue(ex.Message.Contains("abc"));
                throw;
            }
        }

        //===================================================================================

        [TestMethod]
        public void ReplaceReferencesWithFullConcepts_Test()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>();
            concepts.Add(new SimpleConceptInfo("a", "aaa"));
            concepts.Add(new SimpleConceptInfo("b", "bbb"));
            concepts.Add(new SimpleConceptInfo("c", "ccc"));
            RefConceptInfo ci = new RefConceptInfo("r", new SimpleConceptInfo("b", null));
            concepts.Add(ci);

            Assert.AreEqual(null, ci.Reference.Data);
            DslModelFromConcepts(concepts);
            Assert.AreEqual("bbb", ci.Reference.Data);
        }

        [TestMethod]
        public void ReplaceReferencesWithFullConcepts_UnresolvedReference()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>();
            concepts.Add(new SimpleConceptInfo("ax", "aaa"));
            concepts.Add(new SimpleConceptInfo("cx", "ccc"));
            RefConceptInfo ci = new RefConceptInfo("rx", new SimpleConceptInfo("bx", null));
            concepts.Add(ci);

            TestUtility.ShouldFail<DslSyntaxException>(
                () => DslModelFromConcepts(concepts),
                "Referenced", "REF rx.bx", "SIMPLE bx");
        }

        [TestMethod]
        public void ReplaceReferencesWithFullConcepts_DerivedConcept()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>();
            concepts.Add(new DerivedConceptInfo("a", "aaa", "aaaaa"));
            concepts.Add(new DerivedConceptInfo("b", "bbb", "bbbbb"));
            concepts.Add(new DerivedConceptInfo("c", "ccc", "aaaaa"));
            RefConceptInfo ci = new RefConceptInfo("r", new SimpleConceptInfo("b", null));
            concepts.Add(ci);

            Assert.AreEqual(null, ci.Reference.Data);
            DslModelFromConcepts(concepts);
            Assert.AreEqual("bbb", ci.Reference.Data);
            Assert.IsTrue(ci.Reference is DerivedConceptInfo, "ci.Reference is DerivedConceptInfo");
            Assert.AreEqual("bbbbb", (ci.Reference as DerivedConceptInfo).Extra);
        }

        //===================================================================================
        [TestMethod]
        public void ResolveReferencedConceptsAndRemoveDuplicates_RemoveDuplicates()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>();
            concepts.Add(new SimpleConceptInfo("a", "aaa"));
            concepts.Add(new SimpleConceptInfo("b", "bbb"));
            concepts.Add(new SimpleConceptInfo("a", "aaa"));

            List<IConceptInfo> cleared = DslModelFromConcepts(concepts);
            Assert.AreEqual("InitializationConcept, SIMPLE a, SIMPLE b", GetReport(cleared));

            Assert.AreEqual("a", (cleared[1] as SimpleConceptInfo).Name);
            Assert.AreEqual("b", (cleared[2] as SimpleConceptInfo).Name);
        }

        //===================================================================================

#pragma warning disable CS0618 // Type or member is obsolete. Unit test for the obsolete interface.
        class ConceptWithSemanticsValidation : IValidationConcept
#pragma warning restore CS0618 // Type or member is obsolete
        {
            [ConceptKey]
            public string Name { get; set; }

            public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
            {
                if (Name.Length > 3)
                    throw new Exception("Name too long.");
            }
        }

        [TestMethod]
        public void CheckSemanticsTest_Pass()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>() { new ConceptWithSemanticsValidation { Name = "abc" } };
            Assert.IsNotNull(DslModelFromConcepts(concepts));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Name too long.")]
        public void CheckSemanticsTest_Fail()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>() { new ConceptWithSemanticsValidation { Name = "abcd" } };
            Assert.IsNotNull(DslModelFromConcepts(concepts));
        }

        //===================================================================================

        [TestMethod]
        public void SortReferencesBeforeUsingConceptTest()
        {
            var c1 = new SimpleConceptInfo { Name = "n1", Data = "" };
            var c2 = new RefConceptInfo { Name = "n2", Reference = c1 };
            var concepts = new List<IConceptInfo>() { c2, c1 };

            concepts = DslModelFromConcepts(concepts);

            Assert.AreEqual("InitializationConcept, REF n2.n1, SIMPLE n1", GetReport(concepts));

            Assert.AreSame(c1, concepts[1]);
            Assert.AreSame(c2, concepts[2]);
        }

        //===================================================================================

        [ConceptKeyword("MACRO")]
        class MacroConceptInfo : IConceptInfo, IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public MacroConceptInfo(string value) { Value = value; }

            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                return new List<IConceptInfo>
                           {
                               new SimpleConceptInfo(Value + "1", ""),
                               new SimpleConceptInfo(Value + "2", "")
                           };
            }
        }

        [TestMethod]
        public void ExpandMacroConcepts_Simple()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new SimpleConceptInfo("a", ""),
                                                  new MacroConceptInfo("b")
                                              };

            Assert.AreEqual(
                "InitializationConcept, MACRO b, SIMPLE a, SIMPLE b1, SIMPLE b2",
                GetReport(DslModelFromConcepts(concepts)));
        }

        [ConceptKeyword("SECONDLEVELMACRO")]
        class SecondLevelMacroConceptInfo : IConceptInfo, IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public SecondLevelMacroConceptInfo(string value) { Value = value; }

            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                return new List<IConceptInfo>
                           {
                               new MacroConceptInfo(Value + "x")
                           };
            }
        }

        [TestMethod]
        public void ExpandMacroConcepts_MultiplePassesLinear()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new MacroConceptInfo("a"),
                                                  new SecondLevelMacroConceptInfo("b")
                                              };

            Assert.AreEqual(
                "InitializationConcept, MACRO a, MACRO bx, SECONDLEVELMACRO b, SIMPLE a1, SIMPLE a2, SIMPLE bx1, SIMPLE bx2",
                GetReport(DslModelFromConcepts(concepts)));
        }

        class RecursiveMacroConceptInfo : IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public RecursiveMacroConceptInfo(string value) { Value = value; }
            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                int v = int.Parse(Value);
                return new List<IConceptInfo> { new RecursiveMacroConceptInfo((v + 1).ToString()) };
            }
        }

        [TestMethod]
        public void ExpandMacroConcepts_InfiniteLoop()
        {
            var concepts = new List<IConceptInfo> { new RecursiveMacroConceptInfo("1") };
            TestUtility.ShouldFail<DslSyntaxException>(
                () => DslModelFromConcepts(concepts),
                "infinite loop",
                "RecursiveMacroConceptInfo");
        }

        [TestMethod]
        public void ExpandMacroConcepts_IgnoreDuplicates()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new SimpleConceptInfo("b1", ""),
                                                  new MacroConceptInfo("b")
                                              };
            Assert.AreEqual(
                "InitializationConcept, MACRO b, SIMPLE b1, SIMPLE b2",
                GetReport(DslModelFromConcepts(concepts)));
        }

        [TestMethod]
        [ExpectedException(typeof(DslSyntaxException))]
        public void ExpandMacroConcepts_ErrorOnDuplicateKeyDifferentValue()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new SimpleConceptInfo("b1", "xxx"),
                                                  new MacroConceptInfo("b")
                                              };
            List<string> expected = new List<string> { "MACRO b", "SIMPLE b1", "SIMPLE b2" };

            try
            {
                DslModelFromConcepts(concepts).Select(c => c.GetUserDescription()).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.IsTrue(ex.Message.Contains("xxx"));
                Assert.IsTrue(ex.Message.Contains("b1"));
                throw;
            }
        }

        [ConceptKeyword("MULTIPASS1")]
        class MultiplePassMacroConceptInfo1 : IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                return existingConcepts.OfType<SimpleConceptInfo>().Where(c => !c.Name.StartsWith("dup"))
                    .Select(c => new SimpleConceptInfo {Name = "dup" + c.Name, Data = ""});
            }
        }

        [ConceptKeyword("MULTIPASS2")]
        class MultiplePassMacroConceptInfo2 : IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                int count = existingConcepts.OfType<SimpleConceptInfo>().Where(c => !c.Name.StartsWith("dup")).Count();
                if (count < 3)
                    return new List<IConceptInfo> { new SimpleConceptInfo { Name = count.ToString(), Data = "" } };
                return null;
            }
        }

        [TestMethod]
        public void ExpandMacroConcepts_MultiplePassesWithBackReferences()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new MultiplePassMacroConceptInfo1 {Value = "x"},
                                                  new MultiplePassMacroConceptInfo2 {Value = "x"}
                                              };

            var result = DslModelFromConcepts(concepts);
            Console.WriteLine(string.Join(", ", result.Select(c => c.GetUserDescription())));

            Assert.AreEqual(3, result.OfType<SimpleConceptInfo>().Where(c => !c.Name.StartsWith("dup")).Count());
            Assert.AreEqual(3, result.OfType<SimpleConceptInfo>().Where(c => c.Name.StartsWith("dup")).Count());
        }

        [TestMethod]
        public void ReferenceToMacroConcept()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new RefConceptInfo {Name = "r", Reference = new SimpleConceptInfo { Name = "2", Data = ""}},
                                                  new MultiplePassMacroConceptInfo2 {Value = "x"}
                                              };

            var result = DslModelFromConcepts(concepts);
            Assert.AreEqual("InitializationConcept, MULTIPASS2 x, REF r.2, SIMPLE 0, SIMPLE 1, SIMPLE 2", GetReport(result));
        }

        //===================================================================================
        // IAlternativeInitializationConcept:

        [ConceptKeyword("alter1")]
        class AlternativeConcept1 : IAlternativeInitializationConcept
        {
            [ConceptKey]
            public SimpleConceptInfo Parent { get; set; }
            [ConceptKey]
            public string Name { get; set; }
            public RefConceptInfo RefToParent { get; set; }

            public IEnumerable<string> DeclareNonparsableProperties()
            {
                return new[] { "Name", "RefToParent" };
            }

            public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
            {
                Name = "a1";
                RefToParent = new RefConceptInfo { Name = "ref", Reference = new SimpleConceptInfo { Name = Parent.Name, Data = Parent.Data } };

                createdConcepts = new[] { RefToParent };
            }
        }

        [ConceptKeyword("alter2")]
        class AlternativeConcept2 : IAlternativeInitializationConcept
        {
            [ConceptKey]
            public AlternativeConcept1 Alter1 { get; set; }
            [ConceptKey]
            public string Name { get; set; }
            public string Data { get; set; }

            public IEnumerable<string> DeclareNonparsableProperties()
            {
                return new[] { "Name" };
            }

            public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
            {
                Name = "a2";
                createdConcepts = null;
            }
        }

        [TestMethod]
        public void InitiallyParsedAlternativeInitializationConceptTest()
        {
            string dsl = "SIMPLE s d; ALTER1 s; ALTER2 s.a1 d2;";
            var grammar = new IConceptInfo[] { new SimpleConceptInfo(), new AlternativeConcept1(), new AlternativeConcept2() };

            var parsedConcepts = new TestDslParser(dsl, grammar).ParsedConcepts;
            var dslModelConcepts = DslModelFromConcepts(parsedConcepts);

            var s = dslModelConcepts.OfType<SimpleConceptInfo>().Single();
            var a1 = dslModelConcepts.OfType<AlternativeConcept1>().Single();
            var a2 = dslModelConcepts.OfType<AlternativeConcept2>().Single();
            var r = dslModelConcepts.OfType<RefConceptInfo>().Single();

            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+SimpleConceptInfo Name=s Data=d", s.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+AlternativeConcept1 Parent=s Name=a1 RefToParent=ref.s", a1.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+AlternativeConcept2 Alter1=s.a1 Name=a2 Data=d2", a2.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+RefConceptInfo Name=ref Reference=s", r.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+SimpleConceptInfo Name=s Data=d", r.Reference.GetErrorDescription());

            Assert.AreSame(s, a1.Parent);
            Assert.AreSame(r, a1.RefToParent);
            Assert.AreSame(a1, a2.Alter1);
            Assert.AreSame(s, r.Reference);

            Assert.AreEqual("alter1, alter2, InitializationConcept, REF, SIMPLE", TestUtility.DumpSorted(dslModelConcepts, c => c.GetKeywordOrTypeName()));
        }

        [TestMethod]
        public void InitiallyParsedAlternativeInitializationConcept_Embedded()
        {
            string dsl = "SIMPLE s d { ALTER1 { ALTER2 d2; } }";
            var grammar = new IConceptInfo[] { new SimpleConceptInfo(), new AlternativeConcept1(), new AlternativeConcept2() };
            var parsedConcepts = new TestDslParser(dsl, grammar).ParsedConcepts;
            var dslModelConcepts = DslModelFromConcepts(parsedConcepts);

            var s = dslModelConcepts.OfType<SimpleConceptInfo>().Single();
            var a1 = dslModelConcepts.OfType<AlternativeConcept1>().Single();
            var a2 = dslModelConcepts.OfType<AlternativeConcept2>().Single();
            var r = dslModelConcepts.OfType<RefConceptInfo>().Single();

            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+SimpleConceptInfo Name=s Data=d", s.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+AlternativeConcept1 Parent=s Name=a1 RefToParent=ref.s", a1.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+AlternativeConcept2 Alter1=s.a1 Name=a2 Data=d2", a2.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+RefConceptInfo Name=ref Reference=s", r.GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslModelTest+SimpleConceptInfo Name=s Data=d", r.Reference.GetErrorDescription());

            Assert.AreSame(s, a1.Parent);
            Assert.AreSame(r, a1.RefToParent);
            Assert.AreSame(a1, a2.Alter1);
            Assert.AreSame(s, r.Reference);

            Assert.AreEqual("alter1, alter2, InitializationConcept, REF, SIMPLE", TestUtility.DumpSorted(dslModelConcepts, c => c.GetKeywordOrTypeName()));
        }
    }
}
