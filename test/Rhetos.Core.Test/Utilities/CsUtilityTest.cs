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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class CsUtilityTest
    {
        [TestMethod]
        public void QuotedString()
        {
            string stringConstant = "\r\nabc \" \\ \\\" 123 \\\"\" \\\\\"\"\r\n\t\t \rx\nx ";

            string code = string.Format(
                @"using System;
                namespace GeneratedModuleQuotedString
                {{
                    public class C
                    {{
                        public static string F1()
                        {{
                            return {0};
                        }}
                        public static string F2()
                        {{
                            return {1};
                        }}
                    }}
                }}",
                CsUtility.QuotedString(stringConstant),
                CsUtility.QuotedString(null));

            Console.WriteLine(code);

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilation compilation = CSharpCompilation.Create(
                "GeneratedQuotedStringAssembly",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                if (emitResult.Success)
                {
                    var ourAssembly = Assembly.Load(dllStream.ToArray());

                    Type generatedClass = ourAssembly.GetType("GeneratedModuleQuotedString.C");

                    {
                        MethodInfo generatedMethod = generatedClass.GetMethod("F1");
                        string generatedCodeResult = (string)generatedMethod.Invoke(null, Array.Empty<object>());
                        Assert.AreEqual(stringConstant, generatedCodeResult);
                    }

                    {
                        MethodInfo generatedMethod = generatedClass.GetMethod("F2");
                        string generatedCodeResult = (string)generatedMethod.Invoke(null, Array.Empty<object>());
                        Assert.IsNull(generatedCodeResult);
                    }
                }
                else
                {
                    foreach (var error in emitResult.Diagnostics)
                        Console.WriteLine(error);
                    Assert.Fail("Compiler errors");
                }
            }
        }

        [TestMethod]
        public void ValidateNameTest()
        {
            string[] validNames = new[] {
                "abc", "ABC", "i",
                "a12300", "a1a",
                "_abc", "_123", "_", "a_a_"
            };

            string[] invalidNames = new[] {
                "0", "2asdasd", "123", "1_",
                null, "",
                " abc", "abc ", " ",
                "!", "@", "#", "a!", "a@", "a#",
                "ač", "č",
            };

            foreach (string name in validNames)
            {
                Console.WriteLine("Testing valid name '" + name + "'.");
                string error = CsUtility.GetIdentifierError(name);
                Console.WriteLine("Error: " + error);
                Assert.IsNull(error);
            }

            foreach (string name in invalidNames)
            {
                Console.WriteLine("Testing invalid name '" + name + "'.");
                string error = CsUtility.GetIdentifierError(name);
                Console.WriteLine("Error: " + error);

                if (name == null)
                    TestUtility.AssertContains(error, "null");
                else if (name == "")
                    TestUtility.AssertContains(error, "empty");
                else
                    TestUtility.AssertContains(error, new[] { name, "not valid" });
            }
        }

        class BaseClass { public string Name; }

        class DerivedClass : BaseClass { }

        [TestMethod]
        public void GetTypeHierarchyTest()
        {
            object o = new DerivedClass { Name = "abc" };
            string expected = "BaseClass-DerivedClass";
            string actual = string.Join("-", CsUtility.GetClassHierarchy(o.GetType()).Select(type => type.Name));
            Assert.AreEqual(expected, actual);

            o = new object();
            expected = "";
            actual = string.Join("-", CsUtility.GetClassHierarchy(o.GetType()).Select(type => type.Name));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ConcatenateEnumerables()
        {
            var tests = new (List<IEnumerable<string>> Input, int ExpectedCount, string ExpectedType)[]
            {
                (new List<IEnumerable<string>> { }, 0, "System.String[]"),
                (new List<IEnumerable<string>> { new[] { "a" } }, 1, "System.String[]"),
                (new List<IEnumerable<string>> { new[] { "a" }, new[] { "b" } }, 2, "System.Linq.Enumerable+Concat2Iterator`1[System.String]"),
            };

            List<string> actualReport = [];
            List<string> expectedReport = [];
            foreach (var test in tests)
            {
                IEnumerable<string> output = CsUtility.Concatenate(test.Input);

                actualReport.Add($"{output.Count()} {output.GetType()}");
                expectedReport.Add($"{test.ExpectedCount} {test.ExpectedType}");
            }

            Assert.AreEqual(
                string.Join(Environment.NewLine, expectedReport),
                string.Join(Environment.NewLine, actualReport));
        }

        [TestMethod]
        public void ConcatenateCollections()
        {
            var tests = new (List<IReadOnlyCollection<string>> Input, int ExpectedCount, string ExpectedType)[]
            {
                (new List<IReadOnlyCollection<string>> { }, 0, "System.String[]"),
                (new List<IReadOnlyCollection<string>> { new[] { "a" } }, 1, "System.String[]"),
                (new List<IReadOnlyCollection<string>> { new[] { "a" }, new[] { "b" } }, 2, "System.Collections.Generic.List`1[System.String]"),
            };

            List<string> actualReport = [];
            List<string> expectedReport = [];
            foreach (var test in tests)
            {
                IReadOnlyCollection<string> output = CsUtility.Concatenate(test.Input);

                actualReport.Add($"{output.Count} {output.GetType()}");
                expectedReport.Add($"{test.ExpectedCount} {test.ExpectedType}");
            }

            Assert.AreEqual(
                string.Join(Environment.NewLine, expectedReport),
                string.Join(Environment.NewLine, actualReport));
        }

        [TestMethod]
        public void NaturalSortTest_Simple()
        {
            string test = @"0, 1, 9, 10, 11, 100, s1:1\9.9\x, s2:1\9.10\x, s3:1\10.9\x, s4:1\10.10\x, s5:1\11\x, s6:1\11.next\x";

            var unsortedList = test.Split(new[] { ", " }, StringSplitOptions.None).Reverse();
            Assert.AreNotEqual(test, TestUtility.Dump(unsortedList));
            Assert.AreEqual(test, TestUtility.Dump(unsortedList.OrderBy(s => CsUtility.GetNaturalSortString(s))));
        }

        [TestMethod]
        public void NaturalSortTest_NonNumeric()
        {
            var tests = new[] { "", " ", "  ", "a", ".", "-" };

            foreach (var test in tests)
                Assert.AreEqual(test, CsUtility.GetNaturalSortString(test));
        }

        [TestMethod]
        public void MatchPrefixes()
        {
            // Exact match:
            TestMatchPrefixes("a b c", "b", "b");
            TestMatchPrefixes("b", "b", "b");
            TestMatchPrefixes("a c", "b", "");

            // Simple:
            TestMatchPrefixes("a1 b1 b21 b22 c1", "b", "b1 b21 b22");
            TestMatchPrefixes("a1 b1 b21 b22 c1", "b2", "b21 b22");
            TestMatchPrefixes("a1 b1 b21 b22 c1", "b21", "b21");

            // Empty lists:
            TestMatchPrefixes("", "b", "");
            TestMatchPrefixes("b", "", "");
            TestMatchPrefixes("", "", "");

            // Case insensitive:
            TestMatchPrefixes("a1 b1 B21 b22 c1", "B", "b1 B21 b22");
            TestMatchPrefixes("A1 B1 b21 B22 C1", "b2", "b21 B22");

            // Complex:
            TestMatchPrefixes("a1 a1. a1.b1 a1.b2 a1.c a1b", "a1.b", "a1.b1 a1.b2");
            TestMatchPrefixes("a1 a1. a1.b1 a1.b2 a1.c a1b", "a1.b2", "a1.b2");
            TestMatchPrefixes("a1 a1. a1.b1 a1.b2 a1.c a1b", "a1.", "a1. a1.b1 a1.b2 a1.c");
        }

        private void TestMatchPrefixes(string strings, string prefixes, string expected)
        {
            var listStrings = strings.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var listPrefixes = prefixes.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var listExpected = expected.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var listActual = CsUtility.MatchPrefixes(listStrings, listPrefixes);

            Assert.AreEqual(TestUtility.DumpSorted(listExpected), TestUtility.DumpSorted(listActual), "Matching strings '" + strings + "' for prefixes '" + prefixes + "'.");
        }

        [TestMethod]
        public void FirstLine()
        {
            var tests = new ListOfTuples<string, string>
            {
                { "abc", "abc" },
                { "", "" },
                { null, null },
                { "abc\r\ndef\r\n123", "abc" },
                { "a\r\ndef", "a" },
                { "\r\ndef", "" },
                { "\r\ndef\r\n", "" },
                { "\r\ndef\r\n1234", "" },
                { "\r", "" },
                { "\n", "" },
                { "abc\rdef\n123", "abc" },
                { "abc\ndef\r123", "abc" },
            };

            foreach (var test in tests)
                Assert.AreEqual(test.Item2, CsUtility.FirstLine(test.Item1), "Test: " + test.Item1);
        }

        [TestMethod]
        public void ReportSegment()
        {
            var tests = new ListOfTuples<string, int, int, string>
            {
                { "", -100, 3, "" },
                { "", -1, 3, "" },
                { "", 0, 3, "" },
                { "", 1, 3, "" },
                { "", 100, 3, "" },

                { "abc", -1000, 100, "abc" },
                { "abc", -10, 100, "abc" },
                { "abc", 0, 100, "abc" },
                { "abc", 10, 100, "abc" },
                { "abc", 1000, 100, "abc" },

                { "abcde", 0, 1, "a..." },
                { "abcde", 1, 1, "...b..." },
                { "abcde", 5, 1, "...e" },

                { "abcde", -10, 3, "abc..." },
                { "abcde", -1, 3, "abc..." },
                { "abcde", 0, 3, "abc..." },
                { "abcde", 1, 3, "abc..." },
                { "abcde", 2, 3, "...bcd..." },
                { "abcde", 3, 3, "...cde" },
                { "abcde", 4, 3, "...cde" },
                { "abcde", 5, 3, "...cde" },
                { "abcde", 10, 3, "...cde" },

                { "abcde", -10, 2, "ab..." },
                { "abcde", -1, 2, "ab..." },
                { "abcde", 0, 2, "ab..." },
                { "abcde", 1, 2, "ab..." },
                { "abcde", 2, 2, "...bc..." },
                { "abcde", 3, 2, "...cd..." },
                { "abcde", 4, 2, "...de" },
                { "abcde", 5, 2, "...de" },
                { "abcde", 10, 2, "...de" },

                { "\r\n\t", 0, 1, @"\r..." },
                { "\r\n\t", 1, 1, @"...\n..." },
                { "\r\n\t", 2, 1, @"...\t" },
                { "\r\n\t\\", 2, 10, @"\r\n\t\\" },
            };

            foreach (var test in tests)
                Assert.AreEqual(test.Item4, CsUtility.ReportSegment(test.Item1, test.Item2, test.Item3), $"Test: {test.Item1} {test.Item2} {test.Item3}");
        }

        [TestMethod]
        public void GetShortTypeName()
        {
            var types = new[]
            {
                typeof(int),
                typeof(string),
                typeof(InnerClass),
                typeof(List<int>),
                typeof(List<string>),
                typeof(List<InnerClass>),
                typeof(int[]),
                typeof(string[]),
                typeof(InnerClass[]),
                typeof(Dictionary<List<InnerClass[]>, InnerClass>),
            };

            var results = types.Select(t => CsUtility.GetShortTypeName(t)).ToList();

            string expected =
@"Int32
String
InnerClass
List`1<Int32>
List`1<String>
List`1<InnerClass>
Int32[]
String[]
InnerClass[]
Dictionary`2<List`1<InnerClass[]>, InnerClass>";

            TestUtility.AssertAreEqualByLine(expected, string.Join("\r\n", results));
        }

        class InnerClass { };

        [TestMethod]
        public void Indent()
        {
            Assert.AreEqual("", CsUtility.Indent("", 0));
            Assert.AreEqual(" a", CsUtility.Indent("a", 1));
            Assert.AreEqual("  a", CsUtility.Indent("a", 2));
            Assert.AreEqual("  a\r\n  b", CsUtility.Indent("a\r\nb", 2));
            Assert.AreEqual("  a\r\n  b", CsUtility.Indent("a\nb", 2));
        }

        [TestMethod]
        public void LimitSimple()
        {
            var tests = new (string Text, int Limit, string Expected)[]
            {
                ("", 0, ""),
                ("", 1, ""),
                ("", 2, ""),
                ("ab", 0, ""),
                ("ab", 1, "a"),
                ("ab", 2, "ab"),
                ("ab", 3, "ab"),
            };

            var expectedReport = string.Join(Environment.NewLine, tests.Select(test => $"{test.Text}, {test.Limit} => {test.Expected}"));
            var actualReport = string.Join(Environment.NewLine, tests.Select(test => $"{test.Text}, {test.Limit} => {Try(() => test.Text.Limit(test.Limit))}"));
            Assert.AreEqual(expectedReport, actualReport);
        }

        [TestMethod]
        public void LimitWithTrimMark()
        {
            var tests = new (string Text, int Limit, string Expected)[]
            {
                ("abcd", 0, ""),
                ("abcd", 1, "1"),
                ("abcd", 2, "12"),
                ("abcd", 3, "a12"),
                ("abcd", 4, "abcd"),
                ("", 0, ""),
                ("", 1, ""),
                ("", 2, ""),
                ("", 3, ""),
                ("a", 0, ""),
                ("a", 1, "a"),
                ("a", 2, "a"),
                ("a", 3, "a"),
                ("aa", 0, ""),
                ("aa", 1, "1"),
                ("aa", 2, "aa"),
                ("aaa", 0, ""),
                ("aaa", 1, "1"),
                ("aaa", 2, "12"),
                ("aaa", 3, "aaa"),
            };

            var expectedReport = string.Join(Environment.NewLine, tests.Select(test => $"{test.Text}, {test.Limit} => {test.Expected}"));
            var actualReport = string.Join(Environment.NewLine, tests.Select(test => $"{test.Text}, {test.Limit} => {Try(() => test.Text.Limit(test.Limit, "12"))}"));
            Assert.AreEqual(expectedReport, actualReport);
        }

        [TestMethod]
        public void LimitWithLengthInfo()
        {
            var tests = new (string Text, int Limit, string Expected)[]
            {
                (new string('a', 50), 51, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                (new string('a', 50), 50, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                (new string('a', 50), 49, "aaaaaaaaaaaaaaaaaaaaaaaaaaaa... (total length 50)"),
                (new string('a', 50), 40, "aaaaaaaaaaaaaaaaaaa... (total length 50)"),
                (new string('a', 50), 20, "... (total length 50"),
                (new string('a', 50),  6, "... (t"),
                ("", 0, ""),
                ("", 1, ""),
                ("a", 0, ""),
                ("a", 1, "a"),
                ("a", 2, "a"),
                ("aa", 0, ""),
                ("aa", 1, "."),
                ("aa", 2, "aa"),
            };

            var expectedReport = string.Join(Environment.NewLine, tests.Select(test => $"{test.Text}, {test.Limit} => {test.Expected}"));
            var actualReport = string.Join(Environment.NewLine, tests.Select(test => $"{test.Text}, {test.Limit} => {Try(() => test.Text.Limit(test.Limit, true))}"));
            Assert.AreEqual(expectedReport, actualReport);
        }

        [TestMethod]
        public void LimitWithHash()
        {
            var tests = new (string Text, int Limit, string Expected)[]
            {
                ("a", 12, "a"),
                ("aaaaaaaaaaaa", 12, "aaaaaaaaaaaa"),
                ("aaaaaaaaaaaaa", 12, "aaa_E16AD69F"),
                ("aaaaaaaaaaaab", 12, "aaa_48F8B50E"),
                ("aaaaaaaaaaaa ", 12, "aaa_96655C70"),
                ("aaaaaaaaaaaaaa", 12, "aaa_CEC53900"),
                ("aaaaaaaaaaaa", 11, "aa_E16AD69F"),
                ("aaaaaaaaaaaa", 10, "a_CEC53900"),
                ("aaaaaaaaaaaa", 9, "ArgumentException: Minimal limit for LimitWithHash is 10."),
            };

            string report(string text, int limit, string result) => $"{text}, {limit} => {result}";

            var expectedReport = string.Join(Environment.NewLine,
                tests.Select(test => report(test.Text, test.Limit, test.Expected)));

            var actualReport = string.Join(Environment.NewLine,
                tests.Select(test => report(test.Text, test.Limit, Try(() => test.Text.LimitWithHash(test.Limit)))));

            Assert.AreEqual(expectedReport, actualReport);
        }

        private static string Try(Func<object> func)
        {
            try
            {
                return func().ToString();
            }
            catch (Exception e)
            {
                return $"{e.GetType().Name}: {e.Message}";
            }
        }

        [TestMethod]
        public void GetUnderlyingGenericTypeSubclassOfRawGeneric()
        {
            TestUtility.ShouldFail<ArgumentException>(() => CsUtility.GetUnderlyingGenericType(typeof(int), typeof(IEnumerable<>)), "Interfaces are not supported");
            TestUtility.ShouldFail<ArgumentException>(() => CsUtility.GetUnderlyingGenericType(typeof(int), typeof(System.Collections.ArrayList)), "The type must be a generic type");
            TestUtility.ShouldFail<ArgumentException>(() => CsUtility.GetUnderlyingGenericType(typeof(int), typeof(List<string>)), "The generic type should not have any type arguments");
            Assert.IsNull(CsUtility.GetUnderlyingGenericType(typeof(int), typeof(List<>)), "If the method was not able to find a subclass that implements the generic type it will return null.");
            Assert.AreEqual(typeof(List<string>), CsUtility.GetUnderlyingGenericType(typeof(UnderlyingGenericTypeTestClass1), typeof(List<>)));
            Assert.AreEqual(typeof(List<string>), CsUtility.GetUnderlyingGenericType(typeof(UnderlyingGenericTypeTestClass2<string>), typeof(List<>)));
        }

        public class UnderlyingGenericTypeTestClass1 : List<string>
        { }

        public class UnderlyingGenericTypeTestClass2<T> : List<T>
        { }

        [TestMethod]
        public void ShallowCopyTest()
        {
            var orig = new C { S = "test1", I = 123 };
            string origReport = orig.ToString();

            var copy = CsUtility.ShallowCopy(orig);
            Assert.AreEqual(orig.ToString(), copy.ToString());

            copy.I = 456;
            Assert.AreNotEqual(orig.ToString(), copy.ToString());
        }

        class C
        {
            public string S { get; set; }
            public int I { get; set; }
            public override string ToString() => $"{S}/{I}";
        }

        [TestMethod]
        public void CastSimple()
        {
            object o = new C2();
            C1 c1 = CsUtility.Cast<C1>(o, "some object");
            Assert.ReferenceEquals(o, c1);
            Assert.AreEqual(typeof(C2).ToString(), c1.GetType().ToString());
        }

        class C1 { }
        class C2 : C1 { }

        [TestMethod]
        public void CastNull()
        {
            object o = null;
            C1 c1 = CsUtility.Cast<C1>(o, "some object");
            Assert.IsNull(c1);
        }

        [TestMethod]
        public void CastInvalid()
        {
            object o = new C1();
            TestUtility.ShouldFail<ArgumentException>(
                () => _ = CsUtility.Cast<C2>(o, "some object"),
                "Unexpected object type. The provided 'some object' is a 'Rhetos.Utilities.Test.CsUtilityTest+C1' instead of 'Rhetos.Utilities.Test.CsUtilityTest+C2'.");
        }

        [TestMethod]
        public void GroupItemsKeepOrderingTest()
        {
            var tests = new (int[] Input, string ExpectedOutput)[]
            {
                // The inputs are grouped by the last digit.
                 ([ 11, 21, 31 ], "(11, 21, 31)"),
                 ([ 11, 22, 33 ], "(11), (22), (33)"),
                 ([ 11, 21, 32, 42, 53, 63 ], "(11, 21), (32, 42), (53, 63)"),
                 ([ ], ""),
            };

            foreach (var test in tests)
            {
                var output = CsUtility.GroupItemsKeepOrdering(test.Input, e => e % 10);
                string report = string.Join(", ", output.Select(group => "(" + string.Join(", ", group.Items) + ")"));
                Assert.AreEqual(test.ExpectedOutput, report);
            }
        }
    }
}
