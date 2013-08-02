﻿/*
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
using System.Text;
using Rhetos.Utilities;
using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Rhetos.TestCommon;

namespace Rhetos.Dsl.Test
{
    [TestClass()]
    public class DslModelTest
    {
        #region Sample concept classes

        [ConceptKeyword("simple")]
        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public string Data { get; set; }

            public override string ToString() { return "SIMPLE " + Name; }
            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
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

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
            public DerivedConceptInfo(string name, string data, string extra)
                : base(name, data)
            {
                Extra = extra;
            }
        }

        class RefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public SimpleConceptInfo Reference { get; set; }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
            public RefConceptInfo() { }
            public RefConceptInfo(string name, SimpleConceptInfo reference)
            {
                Name = name;
                Reference = reference;
            }
            public override string ToString()
            {
                return "REF " + Name + " " + Reference.ToString();
            }
        }

        #endregion

        internal class StubDslParser : IDslParser
        {
            private readonly IEnumerable<IConceptInfo> _rawConcepts;
            public StubDslParser(IEnumerable<IConceptInfo> rawConcepts) { _rawConcepts = rawConcepts; }
            public IEnumerable<IConceptInfo> ParsedConcepts { get { return _rawConcepts; } }
        }

        static List<IConceptInfo> DslModelFromConcepts(IEnumerable<IConceptInfo> rawConcepts)
        {
            var dslModel = new DslModel(new StubDslParser(rawConcepts), new ConsoleLogProvider());
            return dslModel.Concepts.ToList();
        }

        static List<IConceptInfo> DslModelFromScript(string dsl, IConceptInfo[] conceptInfoPluginsForGenericParser)
        {
            var nullDslParser = new DslParser(new DslSourceHelper(dsl), conceptInfoPluginsForGenericParser, new ConsoleLogProvider());
            Console.WriteLine("Parsed concepts:");
            Console.WriteLine(string.Join(Environment.NewLine, nullDslParser.ParsedConcepts.Select(ci => " - " + ci.GetShortDescription())));

            var dslModel = new DslModel(nullDslParser, new ConsoleLogProvider());
            return dslModel.Concepts.ToList();
        }


        //=========================================================================


        [TestMethod()]
        public void OrganizeConceptsByKey_RemoveDuplicate()
        {
            var concepts = new List<IConceptInfo> { new SimpleConceptInfo("a", "aaa"), new SimpleConceptInfo("a", "aaa") };
            var newConcepts = DslModelFromConcepts(concepts);
            Assert.AreEqual(1, newConcepts.Count);
        }

        [TestMethod()]
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

        [TestMethod()]
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

        [TestMethod()]
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

        [TestMethod()]
        [ExpectedException(typeof(DslSyntaxException))]
        public void ReplaceReferencesWithFullConcepts_UnresolvedReference()
        {
            try
            {
                List<IConceptInfo> concepts = new List<IConceptInfo>();
                concepts.Add(new SimpleConceptInfo("ax", "aaa"));
                concepts.Add(new SimpleConceptInfo("cx", "ccc"));
                RefConceptInfo ci = new RefConceptInfo("rx", new SimpleConceptInfo("bx", null));
                concepts.Add(ci);

                DslModelFromConcepts(concepts);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("eferenc"), "Must contain explanation about \"references\".");
                Assert.IsTrue(ex.Message.Contains("RefConceptInfo"));
                Assert.IsTrue(ex.Message.Contains("SimpleConceptInfo"));
                Assert.IsTrue(ex.Message.Contains("rx"));
                Assert.IsTrue(ex.Message.Contains("bx"));
                throw;
            }
        }

        [TestMethod()]
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
        [TestMethod()]
        public void ResolveReferencedConceptsAndRemoveDuplicates_RemoveDuplicates()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>();
            concepts.Add(new SimpleConceptInfo("a", "aaa"));
            concepts.Add(new SimpleConceptInfo("b", "bbb"));
            concepts.Add(new SimpleConceptInfo("a", "aaa"));

            List<IConceptInfo> cleared = DslModelFromConcepts(concepts);
            Assert.AreEqual(2, cleared.Count);
            Assert.AreEqual("a", (cleared[0] as SimpleConceptInfo).Name);
            Assert.AreEqual("b", (cleared[1] as SimpleConceptInfo).Name);
        }


        //===================================================================================

        class ConceptWithSemanticsValidation : IConceptInfo, IValidationConcept
        {
            [ConceptKey]
            public string Name { get; set; }

            public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
            {
                if (Name.Length > 3)
                    throw new Exception("Name too long.");
            }
        }

        [TestMethod()]
        public void CheckSemanticsTest_Pass()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>() { new ConceptWithSemanticsValidation { Name = "abc" } };
            Assert.IsNotNull(DslModelFromConcepts(concepts));
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception), "Name too long.")]
        public void CheckSemanticsTest_Fail()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>() { new ConceptWithSemanticsValidation { Name = "abcd" } };
            Assert.IsNotNull(DslModelFromConcepts(concepts));
        }

        //===================================================================================

        [TestMethod()]
        public void SortReferencesBeforeUsingConceptTest()
        {
            var c1 = new SimpleConceptInfo { Name = "n1", Data = "" };
            var c2 = new RefConceptInfo { Name = "n2", Reference = c1 };
            List<IConceptInfo> concepts = new List<IConceptInfo>() { c2, c1 };

            concepts = DslModelFromConcepts(concepts);

            Assert.AreEqual(2, concepts.Count);
            Assert.AreEqual(c1, concepts[0]);
            Assert.AreEqual(c2, concepts[1]);
        }

        //===================================================================================

        class MacroConceptInfo : IConceptInfo, IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public MacroConceptInfo(string value) { Value = value; }
            public override string ToString() { return "MACRO " + Value; }

            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                return new List<IConceptInfo>
                           {
                               new SimpleConceptInfo(Value + "1", ""),
                               new SimpleConceptInfo(Value + "2", "")
                           };
            }
        }

        [TestMethod()]
        public void ExpandMacroConcepts_Simple()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new SimpleConceptInfo("a", ""),
                                                  new MacroConceptInfo("b")
                                              };
            List<string> expected = new List<string> {"SIMPLE a", "MACRO b", "SIMPLE b1", "SIMPLE b2"};

            List<string> actual = DslModelFromConcepts(concepts).Select(c => c.ToString()).ToList();

            expected.Sort();
            actual.Sort();
            Assert.AreEqual(string.Join(", ", expected), string.Join(", ", actual));
        }

        class SecondLevelMacroConceptInfo : IConceptInfo, IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public SecondLevelMacroConceptInfo(string value) { Value = value; }
            public override string ToString() { return "SECONDLEVELMACRO " + Value; }

            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                return new List<IConceptInfo>
                           {
                               new MacroConceptInfo(Value + "x")
                           };
            }
        }

        [TestMethod()]
        public void ExpandMacroConcepts_MultiplePassesLinear()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new MacroConceptInfo("a"),
                                                  new SecondLevelMacroConceptInfo("b")
                                              };
            List<string> expected = new List<string>
                                        {
                                            "MACRO a", "SIMPLE a1", "SIMPLE a2",
                                            "SECONDLEVELMACRO b", "MACRO bx", "SIMPLE bx1", "SIMPLE bx2"
                                        };
            List<string> actual = DslModelFromConcepts(concepts).Select(c => c.ToString()).ToList();

            expected.Sort();
            actual.Sort();
            Assert.AreEqual(string.Join(", ", expected), string.Join(", ", actual));
        }

        class RecursiveMacroConceptInfo : IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public RecursiveMacroConceptInfo(string value) { Value = value; }
            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                int v = int.Parse(Value);
                if (v == existingConcepts.Count())
                    return new List<IConceptInfo> { new RecursiveMacroConceptInfo((v + 1).ToString()) };
                return null;
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(DslSyntaxException))]
        public void ExpandMacroConcepts_InfiniteLoop()
        {
            try
            {
                List<IConceptInfo> concepts = new List<IConceptInfo> { new RecursiveMacroConceptInfo("1") };
                DslModelFromConcepts(concepts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                Assert.IsTrue(ex.Message.Contains("infinite loop"), ex.Message);
                Assert.IsTrue(ex.Message.Contains("RecursiveMacroConceptInfo"), ex.Message);
                throw;
            }
        }

        [TestMethod]
        public void ExpandMacroConcepts_IgnoreDuplicates()
        {
            List<IConceptInfo> concepts = new List<IConceptInfo>
                                              {
                                                  new SimpleConceptInfo("b1", ""),
                                                  new MacroConceptInfo("b")
                                              };
            List<string> expected = new List<string> { "MACRO b", "SIMPLE b1", "SIMPLE b2" };
            List<string> actual = DslModelFromConcepts(concepts).Select(c => c.ToString()).ToList();

            expected.Sort();
            actual.Sort();
            Assert.AreEqual(string.Join(", ", expected), string.Join(", ", actual));
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
                DslModelFromConcepts(concepts).Select(c => c.ToString()).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.IsTrue(ex.Message.Contains("xxx"));
                Assert.IsTrue(ex.Message.Contains("b1"));
                throw;
            }
        }

        class MultiplePassMacroConceptInfo1 : IMacroConcept
        {
            [ConceptKey]
            public string Value { get; set; }
            public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
            {
                return existingConcepts.OfType<SimpleConceptInfo>().Where(c => !c.Name.StartsWith("dup"))
                    .Select(c => new SimpleConceptInfo {Name = "dup" + c.Name, Data = ""});
            }
            public override string ToString()
            {
                return "MULTIPASS1 " + Value;
            }
        }

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
            public override string ToString()
            {
                return "MULTIPASS2 " + Value;
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
            Console.WriteLine(string.Join(", ", result.Select(c => c.ToString())));

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
            Assert.AreEqual("MULTIPASS2 x, REF r SIMPLE 2, SIMPLE 0, SIMPLE 1, SIMPLE 2", TestUtility.DumpSorted(result, item => item.ToString()));
        }
    }
}
