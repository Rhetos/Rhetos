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
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl.Test
{
    class GenericParserHelper<TConceptInfo> : IConceptParser where TConceptInfo : IConceptInfo, new()
    {
        public TokenReader tokenReader;
        public DslSyntax DslSyntax;
        public GenericParser GenericParser;

        public GenericParserHelper(string overrideKeyword = null)
            : this(DslSyntaxHelper.CreateDslSyntax(typeof(TConceptInfo)), overrideKeyword)
        {
        }

        public GenericParserHelper(DslSyntax syntax, string overrideKeyword = null)
        {
            DslSyntax = syntax;
            ConceptType conceptType = DslSyntax.GetConceptType(typeof(TConceptInfo), overrideKeyword);
            GenericParser = new GenericParser(conceptType);
        }

        public ValueOrError<ConceptSyntaxNode> Parse(ITokenReader tokenReader, Stack<ConceptSyntaxNode> context, out List<string> warnings)
            => GenericParser.Parse(tokenReader, context, out warnings);

        public TConceptInfo QuickParse(string dsl, IConceptInfo contextParent = null)
        {
            var context = new Stack<IConceptInfo>();
            if (contextParent != null)
                context.Push(contextParent);

            return QuickParse(dsl, context);
        }

        public TConceptInfo QuickParse(string dsl, Stack<IConceptInfo> context)
        {
            var contextNodes = new Stack<ConceptSyntaxNode>(context
                .Select(ci => DslSyntax.CreateConceptSyntaxNode(ci)).Reverse());

            tokenReader = GenericParserTest.TestTokenReader(dsl);
            ConceptSyntaxNode node = GenericParser.Parse(tokenReader, contextNodes, out var warnings).Value;
            return (TConceptInfo)ConceptInfoHelper.ConvertNodeToConceptInfo(node, new Dictionary<ConceptSyntaxNode, IConceptInfo>());
        }
    }


    [TestClass]
    public class GenericParserTest
    {
        internal static TokenReader TestTokenReader(string dsl, int position = 0)
        {
            var tokenizerResult = new TestTokenizer(dsl).GetTokens();
            if (tokenizerResult.SyntaxError != null)
                ExceptionsUtility.Rethrow(tokenizerResult.SyntaxError);
            return new TokenReader(tokenizerResult.Tokens, position);
        }

        [ConceptKeyword("simple")]
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
            var node = simpleParser.GenericParser.Parse(tokenReader, new Stack<ConceptSyntaxNode>(), out var warnings).Value;
            SimpleConceptInfo ci = (SimpleConceptInfo)ConceptInfoHelper.ConvertNodeToConceptInfo(node, new Dictionary<ConceptSyntaxNode, IConceptInfo>());

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
            var simpleParser = new GenericParserHelper<SimpleConceptInfo>();
            var tokenReader = TestTokenReader("simp simple abc");
            var ciOrError = simpleParser.GenericParser.Parse(tokenReader, new Stack<ConceptSyntaxNode>(), out var warnings);
            Assert.IsTrue(ciOrError.IsError);
            Assert.AreEqual("", ciOrError.Error);
        }

        [TestMethod]
        public void ParseNotEnoughParametersInSingleConceptDescription()
        {
            var simpleParser = new GenericParserHelper<SimpleConceptInfo>("module");
            TestUtility.ShouldFail<InvalidOperationException>(
                () => simpleParser.QuickParse("module { entiti e }"),
                "SimpleConceptInfo",
                "Special",
                "{",
                "quotes");
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

            var stack = new Stack<ConceptSyntaxNode>();
            stack.Push(enclosedParser.DslSyntax.CreateConceptSyntaxNode(new SimpleConceptInfo { Name = "a" }));

            var tokenReader = TestTokenReader("simple a { enclosed b; }", 3);
            var node = enclosedParser.GenericParser.Parse(tokenReader, stack, out var warnings).Value;
            var ci = (EnclosedConceptInfo)ConceptInfoHelper.ConvertNodeToConceptInfo(node, new Dictionary<ConceptSyntaxNode, IConceptInfo>());
            Assert.AreEqual("a", ci.Parent.Name);
            Assert.AreEqual("b", ci.Name);
            TestUtility.AssertContains(tokenReader.ReportPosition(), "before: \";");
        }

        [TestMethod]
        public void ParseEnclosedInline()
        {
            var enclosedParser = new GenericParserHelper<EnclosedConceptInfo>("enclosed");
            EnclosedConceptInfo ci = enclosedParser.QuickParse("enclosed a b");
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
            EnclosedConceptInfoLevel2 ci = enclosedParser.QuickParse("enclosedlevel2 b c", root);
            Assert.AreEqual("a", ci.Parent.Parent.Name);
            Assert.AreEqual("b", ci.Parent.Name);
            Assert.AreEqual("c", ci.Name);

            ci = enclosedParser.QuickParse("enclosedlevel2 a.b c");
            Assert.AreEqual("a", ci.Parent.Parent.Name);
            Assert.AreEqual("b", ci.Parent.Name);
            Assert.AreEqual("c", ci.Name);
        }

        [TestMethod]
        public void ParseEnclosedInlineError()
        {
            var enclosedParser = new GenericParserHelper<EnclosedConceptInfoLevel2>("enclosedlevel2");
            TestUtility.ShouldFail<InvalidOperationException>(
                () => enclosedParser.QuickParse("enclosedlevel2 a b c"),
                "\".\"");
        }

        [TestMethod]
        public void ParseEnclosedInWrongConcept()
        {
            var syntax = DslSyntaxHelper.CreateDslSyntax(typeof(EnclosedConceptInfo), typeof(ComplexConceptInfo));
            var parser = new GenericParserHelper<EnclosedConceptInfo>(syntax, "enclosed");
                TestUtility.ShouldFail<InvalidOperationException>(
                    () => parser.QuickParse(
                        "enclosed myparent.myname",
                        new ComplexConceptInfo { Name = "c", SimpleConceptInfo = new SimpleConceptInfo { Name = "s" } }),
                "EnclosedConceptInfo",
                "ComplexConceptInfo");
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

        [ConceptKeyword("enclosed1")]
        class EnclosedSingleProperty1 : IConceptInfo
        {
            [ConceptKey]
            public SimpleConceptInfo Parent { get; set; }
        }

        [ConceptKeyword("enclosed2")]
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
                    "enclosed2 must be nested within the referenced parent concept enclosed1");
            }

            {
                // simple parent { enclosed1; } enclosed2 parent;
                var context = new Stack<IConceptInfo>();
                var parser = new GenericParserHelper<EnclosedSingleProperty2>("enclosed2");
                TestUtility.ShouldFail(() => parser.QuickParse("enclosed2 parent", context),
                    "enclosed2 must be nested within the referenced parent concept enclosed1");
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

        [TestMethod]
        public void RootCannotBeNested()
        {
            var parser = new GenericParserHelper<LikeModule>("Module");

            Assert.AreEqual("LikeModule Module1", parser.QuickParse("Module Module1").GetUserDescription());

            TestUtility.ShouldFail<InvalidOperationException>(
                () => parser.QuickParse("Module Module1", new LikeModule { Name = "Module0" }),
                "cannot be nested");
        }

        //===============================================================
        // Recursive concept:

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
            [ConceptParent]
            public MenuCI Parent { get; set; }
        }

        [TestMethod]
        public void Recursive_EnclosedNode()
        {
            ExtendedRootMenuCI root = new ExtendedRootMenuCI { Name = "R", RootData = "SomeData" };

            string dsl = "menu M";

            var syntax = DslSyntaxHelper.CreateDslSyntax(typeof(MenuCI), typeof(ExtendedRootMenuCI), typeof(SubMenuCI));
            SubMenuCI mi = new GenericParserHelper<SubMenuCI>(syntax, "menu").QuickParse(dsl, root);
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
            var syntax = DslSyntaxHelper.CreateDslSyntax(typeof(MenuCI), typeof(ExtendedRootMenuCI), typeof(SubMenuCI));
            SubMenuCI mi = new GenericParserHelper<SubMenuCI>(syntax, "menu").QuickParse(dsl, parent);
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
        public void Recursive_Root()
        {
            string dsl = "menu a b";
            TestUtility.ShouldFail<InvalidOperationException>(
                () => new GenericParserHelper<LeftRecursiveCI>("menu").QuickParse(dsl),
                "Recursive concept 'LeftRecursiveCI' cannot be used as a root");
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
            var syntax = DslSyntaxHelper.CreateDslSyntax(typeof(InterfaceReferenceConceptInfo), typeof(SimpleConceptInfo));
            var parsedConcept = new GenericParserHelper<InterfaceReferenceConceptInfo>(syntax, "intref").QuickParse(dsl, parent);

            Assert.AreEqual(typeof(SimpleConceptInfo), parsedConcept.Referece.GetType());
            Assert.AreEqual("parent", ((SimpleConceptInfo)parsedConcept.Referece).Name);
            Assert.AreEqual("data", parsedConcept.Data);
        }

        [TestMethod]
        public void InterfaceReference_ErrorIfNotEnclosed()
        {
            string dsl = "intref parent data";
            TestUtility.ShouldFail<InvalidOperationException>(
                () => new GenericParserHelper<InterfaceReferenceConceptInfo>("intref").QuickParse(dsl),
                "Member of type IConceptInfo can only be nested within the referenced parent concept. It must be a first member or marked with ConceptParentAttribute.");
        }

        //================================================================================
        // Complex ConceptParent nesting:

        class LikeModule : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        class LikeEntity : IConceptInfo
        {
            [ConceptKey]
            public LikeModule Module { get; set; }

            [ConceptKey]
            public string Name { get; set; }
        }

        class ComplexNested : IConceptInfo
        {
            [ConceptKey]
            public LikeEntity Entity1 { get; set; }

            [ConceptParent]
            [ConceptKey]
            public LikeEntity Entity2 { get; set; }

            public string Description { get; set; }
        }

        class ComplexNested2 : IConceptInfo
        {
            [ConceptKey]
            public LikeEntity Entity1 { get; set; }

            [ConceptParent]
            [ConceptKey]
            public LikeEntity Entity2 { get; set; }

            [ConceptKey]
            public string Description { get; set; }
        }

        [TestMethod]
        public void ComplexNestedTest()
        {
            var parser = new GenericParserHelper<ComplexNested>("ComplexNested");
            string expectedParsedConcept = "ComplexNested Module1.Entity1.Module2.Entity2";

            // In root:
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(
                dsl: "ComplexNested Module1.Entity1 Module2.Entity2 Description",
                contextParent: null)
                .GetUserDescription());

            // Nested in entity:
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(
                dsl: "ComplexNested Module1.Entity1 Description",
                contextParent: new LikeEntity { Module = new LikeModule { Name = "Module2" }, Name = "Entity2" })
                .GetUserDescription());

            // Nested in module:
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(
                dsl: "ComplexNested Module1.Entity1 Entity2 Description",
                contextParent: new LikeModule { Name = "Module2" })
                .GetUserDescription());
        }

        [TestMethod]
        public void ComplexNested2Test()
        {
            var parser = new GenericParserHelper<ComplexNested2>("ComplexNested2");
            string expectedParsedConcept = "ComplexNested2 Module1.Entity1.Module2.Entity2.Description";

            // In root:
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(
                dsl: "ComplexNested2 Module1.Entity1 Module2.Entity2 Description",
                contextParent: null)
                .GetUserDescription());

            // Nested in entity:
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(
                dsl: "ComplexNested2 Module1.Entity1 Description",
                contextParent: new LikeEntity { Module = new LikeModule { Name = "Module2" }, Name = "Entity2" })
                .GetUserDescription());

            // Nested in module:
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(
                dsl: "ComplexNested2 Module1.Entity1 Entity2 Description",
                contextParent: new LikeModule { Name = "Module2" })
                .GetUserDescription());
        }

        //================================================================================
        // Dot separator:

        class KeyReferenceReference : IConceptInfo
        {
            [ConceptKey]
            public LikeEntity Entity1 { get; set; }

            [ConceptKey]
            public LikeEntity Entity2 { get; set; }
        }

        class KeyReferenceString : IConceptInfo
        {
            [ConceptKey]
            public LikeEntity Entity { get; set; }

            [ConceptKey]
            public string Name { get; set; }
        }

        [TestMethod]
        public void KeyReferenceReferenceSeparator()
        {
            var parser = new GenericParserHelper<KeyReferenceReference>("KeyReferenceReference");
            string expectedParsedConcept = "KeyReferenceReference Module1.Entity1.Module2.Entity2";
            string dsl = "KeyReferenceReference Module1.Entity1 Module2.Entity2";
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(dsl).GetUserDescription());
        }

        [TestMethod]
        public void KeyReferenceStringSeparator()
        {
            // Currently KeyReferenceString expects '.' separator in DSL script, while KeyReferenceReference does not.
            // More consistent behavior would be to use dot only for referenced concept keys, and not here before string property.
            var parser = new GenericParserHelper<KeyReferenceString>("KeyReferenceString");
            string expectedParsedConcept = "KeyReferenceString Module1.Entity1.Name";
            string dsl = "KeyReferenceString Module1.Entity1 Name";
            Assert.AreEqual(expectedParsedConcept, parser.QuickParse(dsl).GetUserDescription());
        }
    }
}