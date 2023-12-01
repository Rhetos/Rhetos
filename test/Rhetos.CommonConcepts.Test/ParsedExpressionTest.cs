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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class ParsedExpressionTest
    {
        [TestMethod]
        public void Parsing()
        {
            // Input format: "Expression / ArgumentTypes"
            string tests =
@"
a => /*1*/ a /*2*/ / int
a v => a.Length / 
(a, b) => (a + b).Length / int double
(string a, string b) => (a + b).Length / string string
(string a, string b) => (a + b).Length / string
(string a) => a.Length / string
(string a) => a.Length / int
(a) => a.Length / int
a => a.Length / int
delegate (string a) { return a.Length; } / string
(string x) => { /*1*/ x++; /*2*/ return /*3*/ x.Length; /*4*/ } / string
async () => await Task.Run(() => ""ad"") /
async () => ""ad""; // Good syntax, invalid semantics. /
a => a.Length / string
(List<Cus[]> a) => b.Length / x
/*asdf*/ a /*a*/ => /*fasd */ b.Length /*asdf */ / x
a => true / x
a => a / x
a => false / x
a => 123 / x
a => 123.0 / x
a => 123m / x
a => (int?)null / x
a => ""he\r\n\""llo"" / x
a => @""he""""llo"" / x
a => null / x
a => nulll / x
() => a / x
() => a /
{ print(a); } /
 /
a; b; /
";
            string expected = // Format: MethodParameters / MethodBody / ResultLiteral(if available), or Exception.
@"
(int a) { return /*1*/ a; }
DslConceptSyntaxException: TestConcept Test: C# syntax error '(1,16): error CS1002: ; expected' in code snippet 'a v => a.Length'.
(int a, double b) { return (a + b).Length; }
(string a, string b) { return (a + b).Length; }
DslConceptSyntaxException: TestConcept Test: The provided code snippet should have 1 parameters instead of 2. Code snippet: '(string a, string b) => (a + b).Length'. Expected parameter types: string.
(string a) { return a.Length; }
(string a) { return a.Length; }
(int a) { return a.Length; }
(int a) { return a.Length; }
DslConceptSyntaxException: TestConcept Test: The provided code snippet should be formatted as a C# lambda expression. Code snippet 'delegate (string a) { return a.Length; }' is 'AnonymousMethodExpression' instead of 'SimpleLambdaExpression or ParenthesizedLambdaExpression'.
(string x) { /*1*/ x++; /*2*/ return /*3*/ x.Length; /*4*/ }
() { return await Task.Run(() => ""ad""); }
() { return ""ad""; }/""ad""
(string a) { return a.Length; }
(List<Cus[]> a) { return b.Length; }
(x a) { return /*fasd */ b.Length; }
(x a) { return true; }/true
(x a) { return a; }
(x a) { return false; }/false
(x a) { return 123; }/123
(x a) { return 123.0; }/123.0
(x a) { return 123m; }/123m
(x a) { return (int?)null; }
(x a) { return ""he\r\n\""llo""; }/""he\r\n\""llo""
(x a) { return @""he""""llo""; }/@""he""""llo""
(x a) { return null; }/null
(x a) { return nulll; }
DslConceptSyntaxException: TestConcept Test: The provided code snippet should have 1 parameters instead of 0. Code snippet: '() => a'. Expected parameter types: x.
() { return a; }
DslConceptSyntaxException: TestConcept Test: The provided code snippet should be formatted as a C# lambda expression. Code snippet '{ print(a); }' is 'Block' instead of 'ExpressionStatement'.
DslConceptSyntaxException: TestConcept Test: The provided code snippet should be formatted as a C# lambda expression. Code snippet '' has no content. Expected content type is 'GlobalStatement'.
DslConceptSyntaxException: TestConcept Test: The provided code snippet should be formatted as a C# lambda expression. Code snippet 'a; b;' contains multiple nodes while only one is expected. Expected child node type is 'GlobalStatement'. The provided snippet contains 2 child nodes: GlobalStatement, GlobalStatement.
";

            IConceptInfo testConcept = new TestConcept { Name = "Test" };

            var results = new List<string>();

            foreach (var test in tests.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int split = test.LastIndexOf('/');
                string expressionText = test.Substring(0, split).Trim();
                string[] argumentTypes = test.Substring(split + 1).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).ToArray();

                try
                {
                    var parsedExpression = new ParsedExpression(expressionText, argumentTypes, testConcept);
                    results.Add($"{Simplify(parsedExpression.MethodParametersAndBody)}{(parsedExpression.ResultLiteral != null ? "/" + parsedExpression.ResultLiteral : "")}");
                }
                catch (Exception e)
                {
                    results.Add($"{e.GetType().Name}: {e.Message}");
                }
            }

            string report = string.Join("\r\n", results);
            Console.WriteLine(report.Replace("\"", "\"\""));

            TestUtility.AssertAreEqualByLine(expected.Trim(), report);
        }

        private string Simplify(string text) => _whitespaceRegex.Replace(text, " ").Trim();

        private static readonly Regex _whitespaceRegex = new Regex(@"\s+");

        private class TestConcept : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        [TestMethod]
        public void FormattingSimpleLambda()
        {
            string expressionText = @"
item
 => //1
/*2*/
item . Name+
item.Surname
/*3*/";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType" }, new TestConcept { Name = "Test" });

            // Place the block under the parameters line to match standard method formatting.
            Assert.AreEqual(@"(SomeType item)
        {
            return //1
/*2*/
item . Name+
item.Surname;
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void FormattingSimpleLambdaWithLineComment()
        {
            string expressionText = @"item => item.Name // Comment. \t \t";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType" }, new TestConcept { Name = "Test" });
            
            // The line-comment must not invalidate the semicolon!
            Assert.AreEqual(@"(SomeType item)
        {
            return item.Name;
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void FormattingBlock()
        {
            string expressionText = @"(string item,
// Not used:
int other
/*commented-out DateTime start*/)
=> {
return	item . Name;

}";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType", "int" }, new TestConcept { Name = "Test" });

            // Keep the original formatting withing the block. Place the block under the parameters line to match formatting of the other expression types.
            Assert.AreEqual(@"(string item,
// Not used:
int other)
        {
            return	item . Name;
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void InsertAdditionalCodeInSimpleLambda()
        {
            string expressionText = @"item => item . Name+
item.Surname";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType" }, new TestConcept { Name = "Test" }, "/*InsertedCode*/");
            // Place the block under the parameters line to match standard method formatting.
            Assert.AreEqual(@"(SomeType item)
        {/*InsertedCode*/
            return item . Name+
item.Surname;
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void InsertAdditionalCodeInBlock()
        {
            string expressionText = @"item => {
return	item . Name;

}";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType" }, new TestConcept { Name = "Test" }, "/*InsertedCode*/");
            Assert.AreEqual(@"(SomeType item)
        {/*InsertedCode*/
            return	item . Name;
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void InsertAdditionalCodeInBlock2()
        {
            string expressionText = @"item => { return item.Name; }";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType" }, new TestConcept { Name = "Test" }, "/*InsertedCode*/");
            Assert.AreEqual(@"(SomeType item)
        {/*InsertedCode*/
            return item.Name;
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void InsertAdditionalParametersInSimpleLambda()
        {
            string expressionText = @"item => item . Name+
item.Surname";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType" }, new TestConcept { Name = "Test" }, null, "1\r\n2");
            // Place the block under the parameters line to match standard method formatting.
            Assert.AreEqual(@"(SomeType item1
2)
        {
            return item . Name+
item.Surname;
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void InsertAdditionalParametersInParenthesizedBlock()
        {
            string expressionText = @"(string item,
// Not used:
int other
/*commented-out DateTime start*/) => {

/*start tag*/
return	item . Name;
/*end tag*/

}";

            var parsedExpression = new ParsedExpression(expressionText, new[] { "SomeType", "int" }, new TestConcept { Name = "Test" }, null, "1\r\n2");
            Assert.AreEqual(@"(string item,
// Not used:
int other1
2)
        {
            /*start tag*/
return	item . Name;
/*end tag*/
        }", parsedExpression.MethodParametersAndBody);
        }

        [TestMethod]
        public void ExpressionsParameters()
        {
            // Input format: "Expression / ArgumentTypes"
            string tests =
@"
a => a.Length
(a, b) => a.Length
(a, b) => (a + b).Length / List<C> double
(string a, List<C> b) => (a + b).Length / List<C> double
(string a, List<C> b) => (a + b).Length
(string a, b) => (a + b).Length / int double
";
            string expected = // Format: MethodParameters / MethodBody / ResultLiteral(if available), or Exception.
@"
null-a
null-a, null-b
List<C>-a, double-b
string-a, List<C>-b
string-a, List<C>-b
int-a, double-b
";

            IConceptInfo testConcept = new TestConcept { Name = "Test" };

            var results = new List<string>();

            foreach (var test in tests.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var testParts = test.Split('/');
                string expressionText = testParts[0].Trim();
                string[] argumentTypes = testParts.Length > 1
                    ? testParts[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(argument => argument.Trim()).ToArray()
                    : null;

                try
                {
                    var parsedExpression = new ParsedExpression(expressionText, argumentTypes, testConcept);
                    results.Add(TestUtility.Dump(parsedExpression.ExpressionParameters, p => $"{p.Type ?? "null"}-{p.Name}"));
                }
                catch (Exception e)
                {
                    results.Add($"{e.GetType().Name}: {e.Message}");
                }
            }

            string report = string.Join("\r\n", results);
            Console.WriteLine(report.Replace("\"", "\"\""));

            TestUtility.AssertAreEqualByLine(expected.Trim(), report);
        }
    }
}
