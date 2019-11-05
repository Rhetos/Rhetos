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

using Rhetos.Utilities;
using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using Rhetos.TestCommon;

namespace Rhetos.Dsl.Test
{
    class GenericParserHelper<TConceptInfo> : GenericParser where TConceptInfo : IConceptInfo, new()
    {
        public TokenReader tokenReader;

        public GenericParserHelper(string keyword)
            : base(typeof(TConceptInfo), keyword)
        {
        }

        public TConceptInfo QuickParse(string dsl)
        {
            return QuickParse(dsl, new Stack<IConceptInfo>());
        }

        public TConceptInfo QuickParse(string dsl, IConceptInfo contextParent)
        {
            Stack<IConceptInfo> context = new Stack<IConceptInfo>();
            context.Push(contextParent);

            tokenReader = GenericParserTest.TestTokenReader(dsl);
            return (TConceptInfo)Parse(tokenReader, context).Value;
        }

        public TConceptInfo QuickParse(string dsl, Stack<IConceptInfo> context)
        {
            tokenReader = GenericParserTest.TestTokenReader(dsl);
            return (TConceptInfo)Parse(tokenReader, context).Value;
        }
    }


    [TestClass]
    public class GenericParserTest
    {
        internal static TokenReader TestTokenReader(string dsl, int position = 0)
        {
            return new TokenReader(new TestTokenizer(dsl).GetTokens(), position);
        }

        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        [TestMethod]
        public void ParseTest()
        {
            var simpleParser = new GenericParserHelper<SimpleConceptInfo>("module");
            SimpleConceptInfo ci = simpleParser.QuickParse("module Wholesale");
            Assert.AreEqual("Wholesale", ci.Name);
        }

        [TestMethod]
        public void ParsePosition()
        {
            var simpleParser = new GenericParserHelper<SimpleConceptInfo>("abc");
            var tokenReader = TestTokenReader("simple abc def", 1);
            SimpleConceptInfo ci = (SimpleConceptInfo)simpleParser.Parse(tokenReader, new Stack<IConceptInfo>()).Value;

            Assert.AreEqual("def", ci.Name);
            TestUtility.AssertContains(tokenReader.ReportPosition(), "column 15,");
        }

        [TestMethod]
        public void ParseKeywordCaseInsensitive()
        {
            var simpleParser = new GenericParserHelper<SimpleConceptInfo>("simple");
            SimpleConceptInfo ci = simpleParser.QuickParse("SIMple abc");
            Assert.AreEqual("abc", ci.Name);
        }

        [TestMethod]
        public void ParseNotEnoughParameters()
        {
            var simpleParser = new GenericParserHelper<SimpleConceptInfo>("module");
            TestUtility.ShouldFail(() => simpleParser.QuickParse("module"),
                "Name", "SimpleConceptInfo", "past the end");
        }

        [TestMethod]
        public void ParseWrongConcept_EmptyErrorForUnrecognizedKeyword()
        {
            var simpleParser = new GenericParser(typeof(SimpleConceptInfo), "simple");
            var tokenReader = TestTokenReader("simp simple abc");
            var ciOrError = simpleParser.Parse(tokenReader, new Stack<IConceptInfo>());
            Assert.IsTrue(ciOrError.IsError);
            Assert.AreEqual("", ciOrError.Error);
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void ParseNotEnoughParametersInSingleConceptDescription()
        {
            try
            {
                var simpleParser = new GenericParserHelper<SimpleConceptInfo>("module");
                SimpleConceptInfo ci = simpleParser.QuickParse("module { entiti e }");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.IsTrue(ex.Message.Contains("SimpleConceptInfo"), "Concept type.");
                Assert.IsTrue(ex.Message.Contains("Special"), "Unexpected special token while reading text.");
                Assert.IsTrue(ex.Message.Contains("{"), "Unexpected special token '{' while reading text.");
                Assert.IsTrue(ex.Message.Contains("quotes"), "Use quotes to specify text (e.g. '{')");
                throw;
            }
        }

        //===============================================================

        class DerivedConceptInfo : SimpleConceptInfo
        {
            public string Name2 { get; set; }
            public string Name3 { get; set; }
        }

        [TestMethod]
        public void ParseDerived()
        {
            var derivedParser = new GenericParserHelper<DerivedConceptInfo>("derived");
            DerivedConceptInfo ci = derivedParser.QuickParse("derived abc def 123");
            Assert.AreEqual("abc", ci.Name);
            Assert.AreEqual("def", ci.Name2);
            Assert.AreEqual("123", ci.Name3);
        }

        //===============================================================

        class ComplexConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public SimpleConceptInfo SimpleConceptInfo { get; set; }
        }

        [TestMethod]
        public void ParseComplex()
        {
            var complexParser = new GenericParserHelper<ComplexConceptInfo>("complex");
            ComplexConceptInfo ci = complexParser.QuickParse("complex a b");
            Assert.AreEqual("a", ci.Name);
            Assert.AreEqual("b", ci.SimpleConceptInfo.Name);
        }

        //===============================================================

        class EnclosedConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public SimpleConceptInfo Parent { get; set; }
            [ConceptKey]
            public string Name { get; set; }
        }

        [TestMethod]
        public void ParseEnclosed()
        {
            var enclosedParser = new GenericParserHelper<EnclosedConceptInfo>("enclosed");

            Stack<IConceptInfo> stack = new Stack<IConceptInfo>();
            stack.Push(new SimpleConceptInfo { Name = "a" });

            var tokenReader = TestTokenReader("simple a { enclosed b; }", 3);
            EnclosedConceptInfo ci = (EnclosedConceptInfo)enclosedParser.Parse(tokenReader, stack).Value;
            Assert.AreEqual("a", ci.Parent.Name);
            Assert.AreEqual("b", ci.Name);
            TestUtility.AssertContains(tokenReader.ReportPosition(), "before: \";");
        }

        [TestMethod]
        public void ParseEnclosedInline()
        {
            var enclosedParser = new GenericParserHelper<EnclosedConceptInfo>("enclosed");
            EnclosedConceptInfo ci = enclosedParser.QuickParse("enclosed a.b");
            Assert.AreEqual("a", ci.Parent.Name);
            Assert.AreEqual("b", ci.Name);
        }

        class EnclosedConceptInfoLevel2 : IConceptInfo
        {
            [ConceptKey]
            public EnclosedConceptInfo Parent { get; set; }
            [ConceptKey]
            public string Name { get; set; }
        }

        [TestMethod]
        public void ParseEnclosedPartiallyInline()
        {
            var enclosedParser = new GenericParserHelper<EnclosedConceptInfoLevel2>("enclosedlevel2");
            var root = new SimpleConceptInfo { Name = "a" };
            EnclosedConceptInfoLevel2 ci = enclosedParser.QuickParse("enclosedlevel2 b.c", root);
            Assert.AreEqual("a", ci.Parent.Parent.Name);
            Assert.AreEqual("b", ci.Parent.Name);
            Assert.AreEqual("c", ci.Name);
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void ParseEnclosedInlineError()
        {
            var dsl = "enclosed abc def";
            var parser = new GenericParserHelper<EnclosedConceptInfo>("enclosed");
            try
            {
                EnclosedConceptInfo ci = parser.QuickParse(dsl);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("\".\""), "Expecting \".\"");
                var msg = parser.tokenReader.ReportPosition();
                Assert.IsTrue(msg.Contains("def"), "Report the unexpected text.");
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void ParseEnclosedInWrongConcept()
        {
            var parser = new GenericParserHelper<EnclosedConceptInfo>("enclosed");
            try
            {
                EnclosedConceptInfo ci = parser.QuickParse(
                    "enclosed myparent.myname",
                    new ComplexConceptInfo { Name = "c", SimpleConceptInfo = new SimpleConceptInfo { Name = "s" } });
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("EnclosedConceptInfo"), "EnclosedConceptInfo");
                Assert.IsTrue(e.Message.Contains("ComplexConceptInfo"), "ComplexConceptInfo");
                throw;
            }
        }

        class EnclosedReference : IConceptInfo
        {
            [ConceptKey]
            public SimpleConceptInfo Parent { get; set; }
            [ConceptKey]
            public EnclosedConceptInfo Reference { get; set; }
        }

        [TestMethod]
        public void ParseEnclosedReference()
        {
            // simple parent1 { enclosed other_enclosed; } simple parent2 { reference (parent1.other_enclosed); }

            var context = new Stack<IConceptInfo>();
            context.Push(new SimpleConceptInfo { Name = "parent2" });
            var referenceParser = new GenericParserHelper<EnclosedReference>("reference");

            var ci = referenceParser.QuickParse("reference parent1.other_enclosed", context);
            Assert.AreEqual("parent2", ci.Parent.Name);
            Assert.AreEqual("parent1", ci.Reference.Parent.Name);
            Assert.AreEqual("other_enclosed", ci.Reference.Name);
        }

        class EnclosedSingleProperty1 : IConceptInfo
        {
            [ConceptKey]
            public SimpleConceptInfo Parent { get; set; }
        }

        class EnclosedSingleProperty2 : IConceptInfo
        {
            [ConceptKey]
            public EnclosedSingleProperty1 Parent { get; set; }
        }

        /// <summary>
        /// A single-reference concept (a decoration) that references another single-reference concept must always be used with embedded syntax to avoid ambiguity.
        /// This is not a must-have feature:
        /// 1. Ambiguity occurs only when there are multiple concepts with the same footprint (keyword and key properties),
        /// such as EntityHistoryAllProperties and LoggingAllProperties.
        /// 2. The ambiguity could be resolved by a semantic check, but that would be hard to implement because of the possibility to reference concepts
        /// that will later be created by the macro concepts.
        /// </summary>
        [TestMethod]
        public void ParseEnclosedSinglePropertyConcept()
        {
            var parent = new SimpleConceptInfo { Name = "parent" };
            var enclosed1 = new EnclosedSingleProperty1 { Parent = parent };
            var enclosed2 = new EnclosedSingleProperty2 { Parent = enclosed1 };

            {
                // simple parent { enclosed1; }
                var context = new Stack<IConceptInfo>(new[] { parent });
                var parser = new GenericParserHelper<EnclosedSingleProperty1>("enclosed1");
                var ci = parser.QuickParse("enclosed1", context);
                Assert.AreEqual("parent", ci.Parent.Name);
            }

            {
                // simple parent; enclosed1 parent;
                var context = new Stack<IConceptInfo>();
                var parser = new GenericParserHelper<EnclosedSingleProperty1>("enclosed1");
                var ci = parser.QuickParse("enclosed1 parent", context);
                Assert.AreEqual("parent", ci.Parent.Name);
            }

            {
                // simple parent { enclosed1 { enclosed2; } }
                var context = new Stack<IConceptInfo>(new IConceptInfo[] { parent, enclosed1 });
                var parser = new GenericParserHelper<EnclosedSingleProperty2>("enclosed2");
                var ci = parser.QuickParse("enclosed2", context);
                Assert.AreEqual("parent", ci.Parent.Parent.Name);
            }

            {
                // simple parent { enclosed1; enclosed2; }
                var context = new Stack<IConceptInfo>(new[] { parent });
                var parser = new GenericParserHelper<EnclosedSingleProperty2>("enclosed2");
                TestUtility.ShouldFail(() => parser.QuickParse("enclosed2", context),
                    "EnclosedSingleProperty2 must be enclosed within the referenced parent concept EnclosedSingleProperty1");
            }

            {
                // simple parent { enclosed1; } enclosed2 parent;
                var context = new Stack<IConceptInfo>();
                var parser = new GenericParserHelper<EnclosedSingleProperty2>("enclosed2");
                TestUtility.ShouldFail(() => parser.QuickParse("enclosed2 parent", context),
                    "EnclosedSingleProperty2 must be enclosed within the referenced parent concept EnclosedSingleProperty1");
            }
        }

        class ConceptWithKey : IConceptInfo
        {
            [ConceptKey]
            public SimpleConceptInfo Parent { get; set; }
            [ConceptKey]
            public string Name { get; set; }
            public string Comment { get; set; }
        }

        class ReferenceWithKey : IConceptInfo
        {
            [ConceptKey]
            public ConceptWithKey Parent { get; set; }
        }

        [TestMethod]
        public void ParseReferenceWithKey()
        {
            string dsl = "reference wholesale.product";
            var parser = new GenericParserHelper<ReferenceWithKey>("reference");
            ReferenceWithKey ci = parser.QuickParse(dsl);
            Assert.AreEqual("wholesale", ci.Parent.Parent.Name);
            Assert.AreEqual("product", ci.Parent.Name);
            Assert.AreEqual(default(string), ci.Parent.Comment);
        }

        class MultipleReferenceWithKey : IConceptInfo
        {
            [ConceptKey]
            public ConceptWithKey Parent { get; set; }
            public ConceptWithKey Reference { get; set; }
        }

        [TestMethod]
        public void ParseMultipleReferenceWithKey()
        {
            string dsl = "reference wholesale.product maticni.mjestotroska";
            var parser = new GenericParserHelper<MultipleReferenceWithKey>("reference");
            MultipleReferenceWithKey ci = parser.QuickParse(dsl);
            Assert.AreEqual("wholesale", ci.Parent.Parent.Name);
            Assert.AreEqual("product", ci.Parent.Name);
            Assert.AreEqual(default(string), ci.Parent.Comment);
            Assert.AreEqual("maticni", ci.Reference.Parent.Name);
            Assert.AreEqual("mjestotroska", ci.Reference.Name);
            Assert.AreEqual(default(string), ci.Reference.Comment);
        }


        class DerivedWithKey : ConceptWithKey
        {
        }

        class ReferenceToDerivedWithKey : IConceptInfo
        {
            [ConceptKey]
            public DerivedWithKey Parent { get; set; }
        }

        [TestMethod]
        public void ParseReferenceToDerivedWithKey()
        {
            string dsl = "reference wholesale.product";
            var parser = new GenericParserHelper<ReferenceToDerivedWithKey>("reference");
            ReferenceToDerivedWithKey ci = parser.QuickParse(dsl);
            Assert.AreEqual("wholesale", ci.Parent.Parent.Name);
            Assert.AreEqual("product", ci.Parent.Name);
            Assert.AreEqual(default(string), ci.Parent.Comment);
        }

        //===============================================================

        [ConceptKeyword("menu")]
        internal class MenuCI : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        class ExtendedRootMenuCI : MenuCI
        {
            public string RootData { get; set; }
        }

        [ConceptKeyword("menu")]
        internal class SubMenuCI : MenuCI
        {
            public MenuCI Parent { get; set; }
        }

        [TestMethod]
        public void Recursive_EnclosedNode()
        {
            ExtendedRootMenuCI root = new ExtendedRootMenuCI { Name = "R", RootData = "SomeData" };

            string dsl = "menu M";
            SubMenuCI mi = new GenericParserHelper<SubMenuCI>("menu").QuickParse(dsl, root);
            Assert.AreEqual("M", mi.Name);
            Assert.AreEqual("R", mi.Parent.Name);
            Assert.AreEqual("SomeData", ((ExtendedRootMenuCI)mi.Parent).RootData);
        }

        [TestMethod]
        public void Recursive_EnclosedNodeLevel2()
        {
            ExtendedRootMenuCI root = new ExtendedRootMenuCI { Name = "R", RootData = "SomeData" };
            SubMenuCI parent = new SubMenuCI { Name = "M", Parent = root };

            string dsl = "menu M2";
            SubMenuCI mi = new GenericParserHelper<SubMenuCI>("menu").QuickParse(dsl, parent);
            Assert.AreEqual("M2", mi.Name);
            Assert.AreEqual("M", mi.Parent.Name);
            Assert.AreEqual("R", ((SubMenuCI)mi.Parent).Parent.Name);
            Assert.AreEqual("SomeData", ((ExtendedRootMenuCI)((SubMenuCI)mi.Parent).Parent).RootData);
        }

        [TestMethod]
        public void Recursive_Flat()
        {
            string dsl = "menu R M";
            SubMenuCI mi = new GenericParserHelper<SubMenuCI>("menu").QuickParse(dsl);
            Assert.AreEqual("R", mi.Name);
            Assert.AreEqual("M", mi.Parent.Name);
        }

        class LeftRecursiveCI : IConceptInfo
        {
            [ConceptKey]
            public LeftRecursiveCI Parent { get; set; }
            [ConceptKey]
            public string Name { get; set; }
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void Recursive_Root()
        {
            try
            {
                string dsl = "menu a b";
                LeftRecursiveCI mi = new GenericParserHelper<LeftRecursiveCI>("menu").QuickParse(dsl);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("root"), "Recursive concept cannot be used as a root.");
                Assert.IsTrue(e.Message.Contains("non-recursive"), "Non-recursive concept should be uses as a root.");
                throw;
            }
        }

        class InterfaceReferenceConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public IConceptInfo Referece { get; set; }
            [ConceptKey]
            public string Data { get; set; }
        }

        [TestMethod]
        public void InterfaceReference_Enclosed()
        {
            var parent = new SimpleConceptInfo { Name = "parent" };
            string dsl = "intref data";
            var parsedConcept = new GenericParserHelper<InterfaceReferenceConceptInfo>("intref").QuickParse(dsl, parent);

            Assert.AreEqual(typeof(SimpleConceptInfo), parsedConcept.Referece.GetType());
            Assert.AreEqual("parent", ((SimpleConceptInfo)parsedConcept.Referece).Name);
            Assert.AreEqual("data", parsedConcept.Data);
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void InterfaceReference_ErrorIfNotEnclosed()
        {
            try
            {
                string dsl = "intref parent data";
                var parsedConcept = new GenericParserHelper<InterfaceReferenceConceptInfo>("intref").QuickParse(dsl);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}