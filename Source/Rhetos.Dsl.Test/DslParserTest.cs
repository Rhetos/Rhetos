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
    [TestClass]
    public class DslParserTest
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


        class RefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public SimpleConceptInfo Reference { get; set; }
        }

        #endregion

        internal static TokenReader TestTokenReader(string dsl, int position = 0)
        {
            return new TokenReader(new TestTokenizer(dsl).GetTokens(), position);
        }


        //===================================================================================

        class TestErrorParser : IConceptParser
        {
            public const string ErrorMessage = "This parser expects '-' after the keyword.";
            readonly string Keyword;
            public TestErrorParser(string keyword)
            {
                this.Keyword = keyword;
            }
            public ValueOrError<IConceptInfo> Parse(ITokenReader tokenReader, Stack<IConceptInfo> context)
            {
                if (tokenReader.ReadText().Value == Keyword)
                {
                    if (tokenReader.TryRead("-"))
                        return new SimpleConceptInfo("", "");
                    else
                        return ValueOrError.CreateError(ErrorMessage);
                }
                return ValueOrError<IConceptInfo>.CreateError("");
            }
        }

        [TestMethod]
        public void ParseNextConcept_DontDescribeExceptionIfConceptNotRecognized()
        {
            string dsl = "a";
            var conceptParsers = new MultiDictionary<string, IConceptParser> ();
            conceptParsers.Add("b", new List<IConceptParser>() { new TestErrorParser("b") });

            TokenReader tokenReader = new TokenReader(new TestTokenizer(dsl).GetTokens(), 0);

            var e = TestUtility.ShouldFail<DslSyntaxException>(
                () => new TestDslParser(dsl).ParseNextConcept(tokenReader, null, conceptParsers));

            Assert.IsFalse(e.Message.Contains(TestErrorParser.ErrorMessage), "Exception must not contain: " + TestErrorParser.ErrorMessage);
        }

        [TestMethod]
        public void ParseNextConcept_PropagateErrorIfKeywordRecognized()
        {
            string dsl = "a";
            var conceptParsers = new MultiDictionary<string, IConceptParser>();
            conceptParsers.Add("a", new List<IConceptParser>() { new TestErrorParser("a") }); ;

            TokenReader tokenReader = TestTokenReader(dsl);

            TestUtility.ShouldFail<DslSyntaxException>(
                () => new TestDslParser(dsl).ParseNextConcept(tokenReader, null, conceptParsers),
                TestErrorParser.ErrorMessage);
        }

        //===================================================================================

        class ExtendedConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public string Data { get; set; }
            public string Data2 { get; set; }
        }

        class EnclosedRefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public SimpleConceptInfo Reference { get; set; }
            [ConceptKey]
            public string Name { get; set; }
        }


        [TestMethod]
        public void ParseNextConcept_SameKeyWordDifferentContext()
        {
            string dsl = "concept simple simpledata; concept ext extdata extdata2;";
            var conceptParsersList = new List<IConceptParser>()
            {
                new GenericParserHelper<SimpleConceptInfo>("concept"),
                new GenericParserHelper<ExtendedConceptInfo>("concept")
            };
            var conceptParsers = new MultiDictionary<string, IConceptParser>();
            conceptParsers.Add("concept", conceptParsersList);

            var noContext = new Stack<IConceptInfo>();
            TokenReader tokenReader = TestTokenReader(dsl);
            IConceptInfo concept = new TestDslParser(dsl).ParseNextConcept(tokenReader, noContext, conceptParsers);
            Assert.AreEqual(typeof(SimpleConceptInfo), concept.GetType());
            Assert.AreEqual("simple", (concept as SimpleConceptInfo).Name);
            Assert.AreEqual("simpledata", (concept as SimpleConceptInfo).Data);

            Assert.IsTrue(tokenReader.TryRead(";"), "Reading ';' between concepts.");

            concept = new TestDslParser(dsl).ParseNextConcept(tokenReader, noContext, conceptParsers);
            Assert.AreEqual(typeof(ExtendedConceptInfo), concept.GetType());
            Assert.AreEqual("ext", (concept as ExtendedConceptInfo).Name);
            Assert.AreEqual("extdata", (concept as ExtendedConceptInfo).Data);
            Assert.AreEqual("extdata2", (concept as ExtendedConceptInfo).Data2);

            Assert.IsTrue(tokenReader.TryRead(";"), "Reading ';' after the concept.");
        }

        [TestMethod]
        public void ParseNextConcept_SameKeyWordDifferentContext_Enclosed()
        {
            string dsl = "concept name data { concept ref; }";
            var conceptParsersList = new List<IConceptParser>()
            {
                new GenericParserHelper<SimpleConceptInfo>("concept"),
                new GenericParserHelper<EnclosedRefConceptInfo>("concept")
            };
            var conceptParsers = new MultiDictionary<string, IConceptParser>();
            conceptParsers.Add("concept", conceptParsersList);

            var context = new Stack<IConceptInfo>();
            TokenReader tokenReader = TestTokenReader(dsl);
            IConceptInfo concept = new TestDslParser(dsl).ParseNextConcept(tokenReader, context, conceptParsers);
            Assert.AreEqual(typeof(SimpleConceptInfo), concept.GetType());
            Assert.AreEqual("name", (concept as SimpleConceptInfo).Name);
            Assert.AreEqual("data", (concept as SimpleConceptInfo).Data);

			Assert.IsTrue(tokenReader.TryRead("{"), "Reading '{' between concepts.");

            context.Push(concept);
            concept = new TestDslParser(dsl).ParseNextConcept(tokenReader, context, conceptParsers);
            Assert.AreEqual(typeof(EnclosedRefConceptInfo), concept.GetType());
            Assert.AreEqual("ref", (concept as EnclosedRefConceptInfo).Name);
            Assert.AreEqual("name", (concept as EnclosedRefConceptInfo).Reference.Name);
            Assert.AreEqual("data", (concept as EnclosedRefConceptInfo).Reference.Data);
        }

        [TestMethod]
        [ExpectedException(typeof(DslSyntaxException))]
        public void ParseNextConcept_SameKeyWordDifferentContext_Ambiguous()
        {
            string dsl = "concept simple data; concept ref simple;";

            var conceptParsersList = new List<IConceptParser>()
            {
                new GenericParserHelper<SimpleConceptInfo>("concept"),
                new GenericParserHelper<RefConceptInfo>("concept")
            };
            var conceptParsers = new MultiDictionary<string, IConceptParser> ();
            conceptParsers.Add("concept", conceptParsersList);

            var noContext = new Stack<IConceptInfo>();
            TokenReader tokenReader = TestTokenReader(dsl);
            try
            {
                IConceptInfo concept = new TestDslParser(dsl).ParseNextConcept(tokenReader, noContext, conceptParsers);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("SimpleConceptInfo"), "Possible interpretation is SimpleConceptInfo.");
                Assert.IsTrue(e.Message.Contains("RefConceptInfo"), "Possible interpretation is RefConceptInfo.");
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DslSyntaxException))]
        public void ParserError_ExpectingErrorHandler()
        {
            string dsl = "concept first second whoami";
            var conceptParsersList = new List<IConceptParser>()
            {
                new GenericParserHelper<SimpleConceptInfo>("concept"),
            };
            var conceptParsers = new MultiDictionary<string, IConceptParser>();
            conceptParsers.Add("concept", conceptParsersList);

            try
            {
                new TestDslParser(dsl).ExtractConcepts(conceptParsers);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Assert.IsTrue(e.Message.Contains("concept"));
                foreach (var prop in typeof(SimpleConceptInfo).GetProperties())
                {
                    Assert.IsTrue(e.Message.Contains(prop.Name));
                    Assert.IsTrue(e.Message.Contains(prop.PropertyType.Name));
                }
                throw;
            }
        }

        //===================================================================================
        // DslParser error reporting:

        [TestMethod]
        public void DslParser_ErrorReporting()
        {
            DslParserParse("simple a b;");

            TestUtility.ShouldFail(() => DslParserParse("simple a"), // missing second parameter
                "simple", "end of the DSL script", MockDslScript.TestScriptName, "line 1", "column 1", "Cannot read the value of Data");

            TestUtility.ShouldFail(() => DslParserParse("simple a;"), // missing second parameter
                "simple", "unexpected", "';'", MockDslScript.TestScriptName, "line 1", "column 1", "Cannot read the value of Data");

            TestUtility.ShouldFail(() => DslParserParse("{"), // invalid syntax
                MockDslScript.TestScriptName, "line 1", "column 1");

            TestUtility.ShouldFail(() => DslParserParse("simple a b"), // missing semicolon
                "simple", "Expected \";\" or \"{\"", MockDslScript.TestScriptName, "line 1", "column 11");
        }
        
        private static IEnumerable<IConceptInfo> DslParserParse(params string[] dsl)
        {
            var dslParser = new DslParser(
                new TestTokenizer(dsl),
                new IConceptInfo[] { new SimpleConceptInfo() },
                new ConsoleLogProvider());
            var parsedConcepts = dslParser.ParsedConcepts;
            Console.WriteLine("Parsed concepts: " + string.Join("\r\n", dslParser.ParsedConcepts));
            return parsedConcepts;
        }

        [TestMethod]
        public void DslParser_MultipleFiles()
        {
            var concepts = DslParserParse("simple a b;", "simple c d;");
            Assert.AreEqual("InitializationConcept, SIMPLE a, SIMPLE c", TestUtility.DumpSorted(concepts,
                c => c is InitializationConcept ? "InitializationConcept" : c.GetUserDescription()));
        }

        [TestMethod]
        public void DslParser_ConceptSplitOverFiles()
        {
            TestUtility.ShouldFail(() => DslParserParse("simple a", " b;"),
                "past the end of the DSL script");

            TestUtility.ShouldFail(() => DslParserParse("simple a b", ";"),
                "Expected \";\" or \"{\"");
        }

        //===================================================================================
        // IAlternativeInitializationConcept:

        [ConceptKeyword("alter1")]
        class AlternativeConcept1: IAlternativeInitializationConcept
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
        public void AlternativeInitializationConceptTest()
        {
            string dsl = "SIMPLE s d; ALTER1 s; ALTER2 s.a1 d2;";
            var grammar = new IConceptInfo[] { new SimpleConceptInfo(), new AlternativeConcept1(), new AlternativeConcept2() };
            var parsedConcepts = new TestDslParser(dsl, grammar).ParsedConcepts;

            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+SimpleConceptInfo Name=s Data=d", parsedConcepts.OfType<SimpleConceptInfo>().Single().GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+AlternativeConcept1 Parent=s Name=a1 RefToParent=ref.s", parsedConcepts.OfType<AlternativeConcept1>().Single().GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+AlternativeConcept2 Alter1=s.a1 Name=a2 Data=d2", parsedConcepts.OfType<AlternativeConcept2>().Single().GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+RefConceptInfo Name=ref Reference=s", parsedConcepts.OfType<RefConceptInfo>().Single().GetErrorDescription());

            // References are not yet resolved when evaluating IAlternativeInitializationConcept, so the SimpleConceptInfo reference does not have a value of the Data property:
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+SimpleConceptInfo Name=s Data=<null>", parsedConcepts.OfType<RefConceptInfo>().Single().Reference.GetErrorDescription(),
                "This test does not describe the wanted behavior, it just describes the current state of the parser.");
        }

        [TestMethod]
        public void AlternativeInitializationConcept_Embedded()
        {
            string dsl = "SIMPLE s d { ALTER1 { ALTER2 d2; } }";
            var grammar = new IConceptInfo[] { new SimpleConceptInfo(), new AlternativeConcept1(), new AlternativeConcept2() };
            var parsedConcepts = new TestDslParser(dsl, grammar).ParsedConcepts;

            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+SimpleConceptInfo Name=s Data=d", parsedConcepts.OfType<SimpleConceptInfo>().Single().GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+AlternativeConcept1 Parent=s Name=a1 RefToParent=ref.s", parsedConcepts.OfType<AlternativeConcept1>().Single().GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+AlternativeConcept2 Alter1=s.a1 Name=a2 Data=d2", parsedConcepts.OfType<AlternativeConcept2>().Single().GetErrorDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+RefConceptInfo Name=ref Reference=s", parsedConcepts.OfType<RefConceptInfo>().Single().GetErrorDescription());

            // Embedded concepts have the parent reference resolved during parsing, so the SimpleConceptInfo reference has a value of the Data property:
            Assert.AreEqual("Rhetos.Dsl.Test.DslParserTest+SimpleConceptInfo Name=s Data=d", parsedConcepts.OfType<RefConceptInfo>().Single().Reference.GetErrorDescription());
        }

        [ConceptKeyword("alterror1")]
        class AlternativeError1 : IAlternativeInitializationConcept
        {
            [ConceptKey]
            public string Name { get; set; }
            public IEnumerable<string> DeclareNonparsableProperties() { return new[] { "Names" }; }
            public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts) { createdConcepts = null; }
        }

        [TestMethod]
        public void AlternativeInitializationConcept_ErrorHandling()
        {
            string dsl = "alterror1;";
            var grammar = new IConceptInfo[] { new AlternativeError1() };
            
            // Parsing a concept with invalid DeclareNonparsableProperties
            TestUtility.ShouldFail(
                () => { var concepts = new TestDslParser(dsl, grammar).ParsedConcepts; },
                "AlternativeError1", "invalid implementation", "Names", "does not exist", "DeclareNonparsableProperties");
        }
    }
}
