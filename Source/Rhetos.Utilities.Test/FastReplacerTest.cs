/*
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
using System.Diagnostics;
using Rhetos.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Rhetos.Utilities.Test
{
    [TestClass()]
    public class FastReplacerTest
    {
        //===========================================================
        // APPEND

        [TestMethod()]
        public void Append_Simple()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("a");
            fr.Append("b");
            fr.Append("c");
            Assert.AreEqual("abc", fr.ToString());
        }

        [TestMethod()]
        public void Append_Simple_OtherLanguages()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("abc");
            fr.Append(@" čćšđž;'[]\");
            Assert.AreEqual(@"abc čćšđž;'[]\", fr.ToString());
        }

        //===========================================================
        // REPLACE

        [TestMethod()]
        public void Replace_Simple()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            fr.Replace("/*a*/", "123");
            Assert.AreEqual("123", fr.ToString());
        }

        [TestMethod()]
        public void Replace_RemoveWhenReplaced()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            Assert.AreEqual(true, fr.Replace("/*a*/", "123"));
            Assert.AreEqual(false, fr.Replace("/*a*/", "456"));
            Assert.AreEqual("123", fr.ToString());
        }

        [TestMethod()]
        public void Replace_Complex()
        {
            const string initial = "(/*1*/)";
            const int replaceCount = 1000;

            string frResult = DoReplacingWithFastReplacer(initial, replaceCount);
            string stringResult = DoReplacingWithString(initial, replaceCount);

            Assert.AreEqual(stringResult, frResult);
        }

        [TestMethod()]
        [Timeout(10000)]
        public void Replace_Performance()
        {
            const string initial = "(/*1*/)";
            const int replaceCount = 20000;

            Stopwatch s = Stopwatch.StartNew();
            string frResult = DoReplacingWithFastReplacer(initial, replaceCount);
            TimeSpan frTime = s.Elapsed;

            Assert.AreEqual(220011, frResult.Length);
            Assert.IsTrue(frTime.TotalSeconds < 1.0, "Test duration should be around 0.2 sec on a CPU with 5.5 Windows Experience Index.");
        }

        private static string DoReplacingWithFastReplacer(string initial, int replaceCount)
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append(initial);
            for (int i = 1; i <= replaceCount; i++)
            {
                string token = "/*" + i + "*/";
                string newText = "(/*" + (2 * i) + "*//*" + (2 * i + 1) + "*/)";
                fr.Replace(token, newText);
            }
            return fr.ToString();
        }

        private static string DoReplacingWithString(string initial, int replaceCount)
        {
            string stringResult = string.Copy(initial);
            for (int i = 1; i <= replaceCount; i++)
            {
                string token = "/*" + i + "*/";
                string newText = "(/*" + (2 * i) + "*//*" + (2 * i + 1) + "*/)";
                stringResult = stringResult.Replace(token, newText);
            }
            return stringResult;
        }

        //===========================================================
        // INSERT

        [TestMethod()]
        public void InsertBefore_Simple()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            fr.InsertBefore("/*a*/", "123");
            Assert.AreEqual("123/*a*/", fr.ToString());
        }

        [TestMethod()]
        public void InsertBefore_ComplexAndOrder()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*//*b*/");
            fr.InsertBefore("/*b*/", "1");
            fr.InsertBefore("/*a*/", "2");
            fr.InsertBefore("/*b*/", "3");
            fr.InsertBefore("/*a*/", "4");
            Assert.AreEqual("24/*a*/13/*b*/", fr.ToString());
        }

        [TestMethod()]
        public void InsertAfter_Simple()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            fr.InsertAfter("/*a*/", "123");
            Assert.AreEqual("/*a*/123", fr.ToString());
        }

        [TestMethod()]
        public void InsertAfter_ComplexAndOrder()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*//*b*/");
            fr.InsertAfter("/*b*/", "1");
            fr.InsertAfter("/*a*/", "2");
            fr.InsertAfter("/*b*/", "3");
            fr.InsertAfter("/*a*/", "4");
            Assert.AreEqual("/*a*/24/*b*/13", fr.ToString());
        }

        //===========================================================
        // CONTAINS

        [TestMethod()]
        public void Contains_Test()
        {
            string a = "/*a*/";
            string b = "/*b*/";
            string c = "/*c*/";
            var fr = new FastReplacer("/*", "*/");
            Assert.AreEqual(false, fr.Contains(a));
            fr.Append(a);
            Assert.AreEqual(true, fr.Contains(a));
            fr.Replace(a, b);
            Assert.AreEqual(false, fr.Contains(a));
            Assert.AreEqual(true, fr.Contains(b));
            fr.Replace(b, b);
            Assert.AreEqual(true, fr.Contains(b));
            fr.Replace(b, c);
            Assert.AreEqual(false, fr.Contains(b));
            Assert.AreEqual(c, fr.ToString());
        }


        //===========================================================
        // COMBINATION OF OPERATIONS

        [TestMethod()]
        public void Combination_TokenReuse()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            fr.Replace("/*a*/", "/*a*/");
            Assert.AreEqual("/*a*/", fr.ToString());

            fr.InsertAfter("/*a*/", "/*a*/");
            Assert.AreEqual("/*a*//*a*/", fr.ToString());

            fr.InsertBefore("/*a*/", "/*a*/");
            Assert.AreEqual("/*a*//*a*//*a*//*a*/", fr.ToString());
        }


        [TestMethod()]
        public void Combination1()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("1");
            fr.Append("2/*a*/3");
            fr.Append("4");
            Assert.AreEqual("12/*a*/34", fr.ToString());

            fr.InsertBefore("/*a*/", "x/*a*/x");
            Assert.AreEqual("12x/*a*/x/*a*/34", fr.ToString());

            fr.InsertBefore("/*a*/", "-");
            Assert.AreEqual("12x-/*a*/x-/*a*/34", fr.ToString());
        }

        [TestMethod()]
        public void Combination2()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            fr.InsertAfter("/*a*/", "/*c*/");
            fr.Replace("/*a*/", "/*b*/");
            fr.InsertAfter("/*b*/", "/*d*/");

            fr.Replace("/*a*/", "a");
            fr.Replace("/*b*/", "b");
            fr.Replace("/*c*/", "c");
            fr.Replace("/*d*/", "d");
            Assert.AreEqual("bdc", fr.ToString());
        }

        [TestMethod()]
        public void Combination_Order()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*//*b*/");

            fr.InsertBefore("/*b*/", "1");
            fr.InsertBefore("/*b*/", "2");
            fr.InsertAfter("/*a*/", "3");
            fr.InsertAfter("/*a*/", "4");
            fr.InsertBefore("/*b*/", "5");
            fr.InsertAfter("/*a*/", "6");
            Assert.AreEqual("/*a*/346125/*b*/", fr.ToString());

            fr.Replace("/*a*/", "7");
            fr.Replace("/*b*/", "8");
            Assert.AreEqual("73461258", fr.ToString());
        }

        [TestMethod()]
        public void Combination_OrderAdvanced()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            fr.InsertAfter("/*a*/", "1");
            fr.InsertAfter("/*a*/", "2");
            fr.Replace("/*a*/", "/*a*/");
            fr.InsertAfter("/*a*/", "3");
            Assert.AreEqual("/*a*/312", fr.ToString());
        }

        //===========================================================
        // CASE INSENSITIVE

        [TestMethod()]
        public void CaseSensitive()
        {
            var fr = new FastReplacer("{", "}");
            fr.Append("{a}");
            Assert.IsTrue(fr.Contains("{a}"));
            Assert.IsFalse(fr.Contains("{A}"));
            Assert.IsFalse(fr.Replace("{A}", "x"));
            Assert.AreEqual("{a}", fr.ToString());
        }

        [TestMethod()]
        public void CaseSensitive_OtherLanguages()
        {
            var fr = new FastReplacer("{", "}");
            fr.Append("{č}");
            Assert.IsTrue(fr.Contains("{č}"));
            Assert.IsFalse(fr.Contains("{Č}"));
            Assert.IsFalse(fr.Replace("{Č}", "x"));
            Assert.AreEqual("{č}", fr.ToString());
        }

        [TestMethod()]
        public void CaseInsensitive()
        {
            var fr = new FastReplacer("{", "}", false);
            fr.Append("{a}");
            Assert.IsTrue(fr.Contains("{a}"));
            Assert.IsTrue(fr.Contains("{A}"));
            Assert.IsTrue(fr.Replace("{A}", "x"));
            Assert.AreEqual("x", fr.ToString());
        }

        [TestMethod()]
        public void CaseInsensitive_OtherLanguages()
        {
            var fr = new FastReplacer("{", "}", false);
            fr.Append("{č}");
            Assert.IsTrue(fr.Contains("{č}"));
            Assert.IsTrue(fr.Contains("{Č}"));
            Assert.IsTrue(fr.Replace("{Č}", "x"));
            Assert.AreEqual("x", fr.ToString());
        }



        //===========================================================
        // ERROR HANDLING

        private static void ShouldFail(Action func, string message)
        {
            bool exceptionThrown = false;
            try
            {
                func();
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
            }
            Assert.IsTrue(exceptionThrown, message);
        }


        private static void FailAppend(string text)
        {
            ShouldFail(delegate
            {
                var fr = new FastReplacer("/*", "*/");
                fr.Append(text);
            }, "Expected exception was not thrown while appending \"" + text + "\".");
        }

        [TestMethod()]
        public void TestErrorHandling_InvalidTokenInsertedInText()
        {
            FailAppend("/*123");
            FailAppend("/*123/*4*/");
            FailAppend("/*123/*4*/5*/");
            FailAppend("/**/");
        }

        [TestMethod]
        public void IgnoreTokenMarksInMultilineText()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append(@"
/*a
*/
/*
/*b*/
*/");
            Assert.IsTrue(fr.Contains("/*b*/"));
            Assert.IsFalse(fr.Contains("/*a*/"));
            fr.Replace("/*b*/", "123");
            Assert.AreEqual(@"
/*a
*/
/*
123
*/", fr.ToString());
        }

        private static void FailUsingToken(string token)
        {
            ShouldFail(delegate
            {
                var fr = new FastReplacer("/*", "*/");
                fr.Append("/*a*/");
                fr.Contains(token);
            }, "Expected exception was not thrown while using token \"" + token + "\" in function Contains.");
            ShouldFail(delegate
            {
                var fr = new FastReplacer("/*", "*/");
                fr.Append("/*a*/");
                fr.Replace(token, "x");
            }, "Expected exception was not thrown while using token \"" + token + "\" in function Replace.");
            ShouldFail(delegate
            {
                var fr = new FastReplacer("/*", "*/");
                fr.Append("/*a*/");
                fr.InsertBefore(token, "x");
            },
                       "Expected exception was not thrown while using token \"" + token + "\" in function InsertBefore.");
            ShouldFail(delegate
            {
                var fr = new FastReplacer("/*", "*/");
                fr.Append("/*a*/");
                fr.InsertAfter(token, "x");
            },
                       "Expected exception was not thrown while using token \"" + token + "\" in function InsertAfter.");
        }

        [TestMethod()]
        public void TestErrorHandling_NotUsingToken()
        {
            FailUsingToken("a");
            FailUsingToken("/*a");
            FailUsingToken("a*/");
            FailUsingToken("/**/");
            FailUsingToken("/*a/*b*/c*/");
            FailUsingToken("/*a*/b*/");
            FailUsingToken("/*a/*b*/");
            FailUsingToken("/*a*//*c*/");
            FailUsingToken("/*a*/b");
            FailUsingToken("a/*b*/");
        }

        [TestMethod()]
        public void RequiredTokenDelimiters()
        {
            ShouldFail(delegate { new FastReplacer("", ""); }, "Expected exception if token delimiters are not provided.");
        }

        [TestMethod()]
        public void ReturnValue()
        {
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*/");
            Assert.AreEqual(true, fr.InsertAfter("/*a*/", "1"));
            Assert.AreEqual(true, fr.InsertBefore("/*a*/", "2"));
            Assert.AreEqual(true, fr.Replace("/*a*/", "/*b*/"));
            Assert.AreEqual(false, fr.InsertAfter("/*a*/", "1"));
            Assert.AreEqual(false, fr.InsertBefore("/*a*/", "2"));
            Assert.AreEqual(false, fr.Replace("/*a*/", "/*c*/"));
            Assert.AreEqual("2/*b*/1", fr.ToString());

            Assert.AreEqual(true, fr.Replace("/*b*/", "/*a*/"));
            Assert.AreEqual(false, fr.Replace("/*x*/", "x1"));
            Assert.AreEqual(false, fr.Replace("/*b*/", "x4"));
            Assert.AreEqual(false, fr.Replace("/*c*/", "x5"));
            Assert.AreEqual(true, fr.Replace("/*a*/", "x6"));
            Assert.AreEqual("2x61", fr.ToString());
        }

        //===========================================================
        // POSSIBLY UNINTUITIVE BEHAVIOUR

        [TestMethod()]
        public void PossiblyUnintuitiveBehaviour_IgnoreTokenIfNotFromSingleText()
        {
            // Behaviour is different from standard String.Replace function:
            // Token is ignored if it is composed by concatenation of two different strings.
            var fr = new FastReplacer("/*", "*/");
            fr.Append("/*a*//*b*/");
            fr.Replace("/*a*/", "/");
            fr.Replace("/*b*/", "*c*/");

            Assert.AreEqual("/*c*/", fr.ToString());
            Assert.IsFalse(fr.Contains("/*c*/"));
            bool replaced = fr.Replace("/*c*/", "-");
            Assert.IsFalse(replaced);
            Assert.AreEqual("/*c*/", fr.ToString());
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void PossiblyUnintuitiveBehaviour_NotAComment()
        {
            // Behaviour is different from standard C# or SQL comments:
            // Tokens are recognised even if they are inside a string (token is not always a comment).
            var fr = new FastReplacer("/*", "*/");
            fr.Append("PRINT 'Token starts with /* sequence'");
        }
    }
}
