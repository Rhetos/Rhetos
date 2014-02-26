﻿/*
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

using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl.Test
{
    [TestClass()]
    public class TokenizerTest
    {
        [TestMethod()]
        public void SkipWhitespaces_NoWhitespace()
        {
            string dsl = "a b";
            int position = 0;
            TokenizerInternals.SkipWhitespaces(dsl, ref position);
            Assert.AreEqual(0, position);
        }

        [TestMethod()]
        public void SkipWhitespaces_AllWhitespaces()
        {
            string dsl = "a \t\r\n. \t\r\n";
            int position = 1;
            TokenizerInternals.SkipWhitespaces(dsl, ref position);
            Assert.AreEqual('.', dsl[position]);
        }

        [TestMethod()]
        public void SkipWhitespaces_EndOfText()
        {
            string dsl = "a    ";
            int position = 2;
            TokenizerInternals.SkipWhitespaces(dsl, ref position);
            Assert.AreEqual(dsl.Length, position);
        }

        //===========================================================================================

        private static Token TestGetNextToken_ValueType(string dsl, ref int position)
        {
            var dslSource = new DslSourceHelper(dsl);
            return TokenizerInternals.GetNextToken_ValueType(dslSource, ref position);
        }

        static void CheckSingle(Token.TokenType expectedType, string expectedValue, string dsl)
        {
            int p = 0;
            Token t = TestGetNextToken_ValueType(dsl, ref p);
            Assert.AreEqual(expectedType, t.Type, dsl);
            Assert.AreEqual(expectedValue, t.Value);
        }


        [TestMethod()]
        public void GetNextToken_Test()
        {
            string dsl = "ab cd ef";
            int position = 3;
            string actual = TestGetNextToken_ValueType(dsl, ref position).Value;
            Assert.AreEqual("cd", actual);
            Assert.AreEqual(5, position);
        }

        [TestMethod()]
        public void GetNextToken_SimpleString()
        {
            CheckSingle(Token.TokenType.Text, "Ab", "Ab");
            CheckSingle(Token.TokenType.Text, "ab", "ab.");
            CheckSingle(Token.TokenType.Text, "ab", "ab{");
            CheckSingle(Token.TokenType.Text, "ab", "ab;");
            CheckSingle(Token.TokenType.Text, "ab", "ab\tcd");
            CheckSingle(Token.TokenType.Text, "ab", "ab\rcd");
            CheckSingle(Token.TokenType.Text, "ab", "ab\ncd");
            CheckSingle(Token.TokenType.Text, "ab_1", "ab_1");
            CheckSingle(Token.TokenType.Text, "_ab", "_ab");
            CheckSingle(Token.TokenType.Text, "123abc", "123abc.");
            CheckSingle(Token.TokenType.Text, "čćšđžČĆŠĐŽ", "čćšđžČĆŠĐŽ.");
        }

        [TestMethod()]
        public void GetNextToken_StringWithQuotes()
        {
            CheckSingle(Token.TokenType.Text, "ab", "'ab'");
            CheckSingle(Token.TokenType.Text, "ab", "\"ab\"");
            CheckSingle(Token.TokenType.Text, "{", "'{'");
            CheckSingle(Token.TokenType.Text, "{", "\"{\"");
            CheckSingle(Token.TokenType.Text, "", "''");
            CheckSingle(Token.TokenType.Text, "", "\"\"");
        }

        [TestMethod()]
        public void GetNextToken_Special()
        {
            CheckSingle(Token.TokenType.Special, ".", "..");
            CheckSingle(Token.TokenType.Special, "{", "{a");
            CheckSingle(Token.TokenType.Special, ";", ";a");
            CheckSingle(Token.TokenType.Special, "/", "/");
        }

        [TestMethod()]
        public void GetNextToken_Comment()
        {
            CheckSingle(Token.TokenType.Comment, "simple", "//simple");
            CheckSingle(Token.TokenType.Comment, " whitespace ", "// whitespace \r\n second line");
            CheckSingle(Token.TokenType.Comment, "", "//");
        }

        [TestMethod()]
        public void GetNextToken_NotComment()
        {
            CheckSingle(Token.TokenType.Text, "//", "'//'");
        }


        //===========================================================================================

        private static List<Token> TestGetTokens(string dsl)
        {
            var dslSource = new DslSourceHelper(dsl);
            return Tokenizer.GetTokens(dslSource);
        }

        static void CheckTokens(string expectedCSV, string dsl)
        {
            List<Token> tokens = TestGetTokens(dsl);
            string csv = string.Join(",", tokens.Select(t => t.Value));
            Assert.AreEqual(expectedCSV, csv);
        }

        static void CheckPositions(string expectedCSV, string dsl)
        {
            List<Token> tokens = TestGetTokens(dsl);
            string csv = string.Join(",", tokens.Select(t => t.PositionInDslSource.ToString()));
            Assert.AreEqual(expectedCSV, csv);
        }


        [TestMethod()]
        public void GetTokens_Simple()
        {
            CheckTokens("ab,cde", "ab cde");
            CheckPositions("0,3", "ab cde");
        }

        [TestMethod()]
        public void GetTokens_Empty()
        {
            CheckTokens("", "  \t\r\n  ");
        }

        [TestMethod()]
        public void GetTokens_Delimiter()
        {
            CheckTokens("simple,abc,;", "simple abc;");
            CheckTokens("simple,abc,{", "simple abc{");
            CheckTokens("simple,abc,def", "simple abc def");
            CheckTokens("simple,_abc", "simple _abc");
        }

        [TestMethod()]
        public void GetTokens_Separators()
        {
            CheckTokens("simple,abc", "\t simple    \t\r\n\r\n\n\tabc\t\r\n");
            CheckPositions("2,19", "\t simple    \t\r\n\r\n\n\tabc\t\r\n");
        }

        [TestMethod()]
        public void GetTokens_String()
        {
            CheckTokens("simple, a b ", "\"simple\" \" a b \"");
        }

        [TestMethod()]
        public void GetTokens_StringCroatian()
        {
            CheckTokens("simple,čćšđžČĆŠĐŽ", "simple \"čćšđžČĆŠĐŽ\"");
        }

        [TestMethod()]
        public void GetTokens_StringWithSpecialCharacters()
        {
            CheckTokens("simple,,./<>?;':[]\\{}|!@#$%^&*()-=_+", "simple \",./<>?;':[]\\{}|!@#$%^&*()-=_+\"");
        }


        [TestMethod()]
        public void GetTokens_StringWithQuotes()
        {
            CheckTokens("simple,abc\"def,next,abc\"def", "simple \"abc\"\"def\" next \"abc\"\"def\"");
            CheckTokens("simple,\"", "simple \"\"\"\"");
        }

        [TestMethod()]
        public void GetTokens_StringWithSingleQuotes()
        {
            CheckTokens("simple,abc\"def", "simple 'abc\"def'");
        }

        [TestMethod()]
        public void GetTokens_StringWithTwoSingleQuotes()
        {
            CheckTokens("simple,abc'def,next,abc'def", "simple 'abc''def' next 'abc''def'");
        }

        [TestMethod()]
        public void GetTokens_RemoveComments()
        {
            CheckTokens("", "//comment");
            CheckTokens("inline", "inline//comment comment");
            CheckTokens("one,two", "one //comment \t until end of line\r\ntwo");
            CheckTokens("one,two", "one //comment \t until end of line unix\ntwo");
        }


        //===========================================================================================

        [TestMethod()]
        public void GetTokens_ReferenceContextData()
        {
            string dsl = "11 '22' //00\r\n 33";
            foreach (Token t in TestGetTokens(dsl))
                Assert.AreEqual(dsl, t.DslSource.Script, t.Value);
        }
    }
}