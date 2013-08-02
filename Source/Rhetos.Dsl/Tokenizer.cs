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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.IO;
using Rhetos.Utilities;

namespace Rhetos.Dsl
{
    public static class Tokenizer
    {
        public static List<Token> GetTokens(IDslSource dslSource)
        {
            List<Token> tokens = new List<Token>();
            int scriptPosition = 0;
            
            while (true)
            {
                TokenizerInternals.SkipWhitespaces(dslSource.Script, ref scriptPosition);
                if (scriptPosition >= dslSource.Script.Length)
                    break;

                int startPosition = scriptPosition;
                Token t = TokenizerInternals.GetNextToken_ValueType(dslSource, ref scriptPosition);

                if (t.Type != Token.TokenType.Comment)
                {
                    t.DslSource = dslSource;
                    t.PositionInDslSource = startPosition;
                    tokens.Add(t);
                }
            }
            return tokens;
        }
    }

    public static class TokenizerInternals
    {
        readonly static char[] Whitespaces = { ' ', '\t', '\n', '\r' };

        public static void SkipWhitespaces(string dsl, ref int position)
        {
            Contract.Requires(dsl != null);
            Contract.Requires(position >= 0 && position <= dsl.Length);
            Contract.Ensures(position >= 0 && position <= dsl.Length);
            Contract.Ensures(position >= Contract.OldValue(position));

            while (position < dsl.Length && Whitespaces.Contains(dsl[position]))
                position++;
        }

        public static Token GetNextToken_ValueType(IDslSource dslSource, ref int position)
        {
            var dsl = dslSource.Script;
            Contract.Requires(dsl != null);
            Contract.Requires(position >= 0 && position <= dsl.Length);
            Contract.Ensures(position >= 0 && position <= dsl.Length);
            Contract.Ensures(position >= Contract.OldValue(position));
            Contract.Ensures(Contract.Result<string>() != null);

            System.Diagnostics.Debug.Assert(
                position < dsl.Length && !Whitespaces.Contains(dsl[position]),
                "Unexpected call of GetNextToken_ValueType without skipping whitespaces.");

            if (IsSimpleStringElement(dsl[position]))
                return new Token
                {
                    Value = ReadSimpleStringToken(dsl, ref position),
                    Type = Token.TokenType.Text
                };
            else if (IsQuotedStringStart(dsl[position]))
                return new Token
                {
                    Value = ReadQuotedString(dsl, ref position),
                    Type = Token.TokenType.Text
                };
            else if (IsExternalTextStart(dsl[position]))
                return new Token
                {
                    Value = ReadExternalText(dslSource, ref position),
                    Type = Token.TokenType.Text
                };
            else if (IsSingleLineCommentStart(dsl, position))
                return new Token
                {
                    Value = ReadSingleLineComment(dsl, ref position),
                    Type = Token.TokenType.Comment
                };
            else
                return new Token
                {
                    Value = ReadSpecialCharacter(dsl, ref position),
                    Type = Token.TokenType.Special
                };
        }

        private static bool IsSingleLineCommentStart(string dsl, int position)
        {
            return position < dsl.Length && dsl[position] == '/'
                && position + 1 < dsl.Length && dsl[position + 1] == '/';
        }

        private static string ReadSingleLineComment(string dsl, ref int end)
        {
            end += 2;
            int begin = end;
            while (end < dsl.Length && dsl[end] != '\r' && dsl[end] != '\n')
                end++;
            return dsl.Substring(begin, end - begin);
        }

        private static string ReadSpecialCharacter(string dsl, ref int end)
        {
            end++;
            return dsl.Substring(end - 1, 1);
        }

        private static bool IsSimpleStringElement(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '_';
        }

        private static string ReadSimpleStringToken(string dsl, ref int end)
        {
            Contract.Requires(dsl != null);
            Contract.Requires(end >= 0 && end < dsl.Length);
            Contract.Ensures(end >= 0 && end < dsl.Length);
            Contract.Ensures(end >= Contract.OldValue(end));

            int begin = end;
            while (end < dsl.Length && IsSimpleStringElement(dsl[end]))
                end++;
            return dsl.Substring(begin, end - begin);
        }

        private static bool IsQuotedStringStart(char c)
        {
            return c == '"' || c == '\'';
        }

        private static string ReadQuotedString(string dsl, ref int end)
        {
            Contract.Requires(dsl != null);
            Contract.Requires(end >= 0 && end < dsl.Length);
            Contract.Ensures(end >= 0 && end < dsl.Length);
            Contract.Ensures(end >= Contract.OldValue(end));

            char quote = dsl[end];
            int begin = end;
            end++;

            while (true)
            {
                while (end < dsl.Length && dsl[end] != quote)
                    end++;
                if (end >= dsl.Length)
                    throw new DslSyntaxException("Unexpected end of script within quoted string. Missing closing character: " + quote + ".");
                if (end + 1 < dsl.Length && dsl[end + 1] == quote)
                {
                    // Two quote characters make escape sequence for a quote within the string:
                    end += 2;
                    continue;
                }
                else
                {
                    // Single quote ends string:
                    end++;
                    break;
                }
            }

            return dsl.Substring(begin + 1, end - begin - 2).Replace(new string(quote, 2), new string(quote, 1));
        }

        private static bool IsExternalTextStart(char c)
        {
            return c == '<';
        }

        private static string ReadExternalText(IDslSource dslSource, ref int end)
        {
            var dsl = dslSource.Script;
            Contract.Requires(dsl != null);
            Contract.Requires(end >= 0 && end < dsl.Length);
            Contract.Ensures(end >= 0 && end < dsl.Length);
            Contract.Ensures(end >= Contract.OldValue(end));

            int begin = end;
            end++;

            while (end < dsl.Length && dsl[end] != '>')
                end++;

            if (end >= dsl.Length)
                throw new DslSyntaxException("Unexpected end of script within external text reference. Missing closing character: '>'.");

            end++; // Skip closing character.

            string basicFilePath = dsl.Substring(begin + 1, end - begin - 2);
            string dslScriptFolder = Path.GetDirectoryName(dslSource.GetSourceFilePath(begin));
            return LoadFile(Path.Combine(dslScriptFolder, basicFilePath));
        }

        private static string LoadFile(string basicFilePath)
        {
            var filePaths = new List<string> { basicFilePath };

            string basicFileExtension = Path.GetExtension(basicFilePath);
            if (basicFileExtension.Equals(".sql", StringComparison.OrdinalIgnoreCase))
            {
                var directory = Path.GetDirectoryName(basicFilePath);
                var fileName = Path.GetFileNameWithoutExtension(basicFilePath);
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new DslSyntaxException("Referenced empty file name (" + basicFilePath + ") in DSL script.");

                string databaseSpecificFilePath = Path.Combine(directory, fileName + " (" + SqlUtility.DatabaseLanguage + ")" + basicFileExtension);
                filePaths.Insert(0, databaseSpecificFilePath);
            }

            Exception lastExcetion = null;
            foreach (var filePath in filePaths)
            {
                try
                {
                    return File.ReadAllText(filePath, Encoding.Default);
                }
                catch (Exception ex)
                {
                    lastExcetion = ex;
                }
            }
            throw new DslSyntaxException("Cannot find the extension file referenced in DSL script. Looking for:\r\n" + string.Join("\r\n", filePaths), lastExcetion);
        }
    }
}
