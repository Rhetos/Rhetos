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

using Microsoft.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults results = provider.CompileAssemblyFromSource(new CompilerParameters(new string[] { }, "GeneratedQuotedStringAssembly"), code);
            foreach (CompilerError error in results.Errors)
                Console.WriteLine(error);
            Assert.AreEqual(0, results.Errors.Count, "Compiler errors");

            Console.WriteLine("CompiledAssembly: " + results.CompiledAssembly.Location);
            Type generatedClass = results.CompiledAssembly.GetType("GeneratedModuleQuotedString.C");

            {
                MethodInfo generatedMethod = generatedClass.GetMethod("F1");
                string generatedCodeResult = (string)generatedMethod.Invoke(null, new object[] { });
                Assert.AreEqual(stringConstant, generatedCodeResult);
            }

            {
                MethodInfo generatedMethod = generatedClass.GetMethod("F2");
                string generatedCodeResult = (string)generatedMethod.Invoke(null, new object[] { });
                Assert.IsNull(generatedCodeResult);
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
    }
}
