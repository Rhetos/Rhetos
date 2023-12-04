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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.Dsl
{
    public class TokenizerInternals
    {
        private readonly static char[] Whitespaces = { ' ', '\t', '\n', '\r' };
        private static readonly HashSet<char> invalidPathChars = new HashSet<char>(Path.GetInvalidPathChars());
        private readonly DslSyntax _syntax;
        private readonly IExternalTextReader _externalTextReader;

        public TokenizerInternals(DslSyntax syntax, IExternalTextReader externalTextReader)
        {
            _syntax = syntax;
            _externalTextReader = externalTextReader;
        }

        public static void SkipWhitespaces(string script, ref int position)
        {
            while (position < script.Length && Whitespaces.Contains(script[position]))
                position++;
        }

        public Token GetNextToken_ValueType(DslScript dslScript, ref int position)
        {
            string script = dslScript.Script;
            if (position < script.Length && Whitespaces.Contains(script[position]))
                throw new FrameworkException("Unexpected call of GetNextToken_ValueType without skipping whitespaces.");

            if (IsSimpleStringElement(script[position]))
                return new Token
                {
                    Value = ReadSimpleStringToken(script, ref position),
                    Type = TokenType.Text
                };
            else if (IsQuotedStringStart(script[position]))
                return new Token
                {
                    Value = ReadQuotedString(dslScript, ref position),
                    Type = TokenType.Text
                };
            else if (IsExternalTextStart(script[position]))
                return new Token
                {
                    Value = ReadExternalText(dslScript, ref position),
                    Type = TokenType.Text
                };
            else if (IsSingleLineCommentStart(script, position))
                return new Token
                {
                    Value = ReadSingleLineComment(script, ref position),
                    Type = TokenType.Comment
                };
            else if (IsBlockCommentStart(script, position))
                return new Token
                {
                    Value = ReadBlockComment(dslScript, ref position),
                    Type = TokenType.Comment
                };
            else
                return new Token
                {
                    Value = ReadSpecialCharacter(script, ref position),
                    Type = TokenType.Special
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

        private static bool IsBlockCommentStart(string dsl, int position)
        {
            return position + 1 < dsl.Length && dsl[position] == '/' && dsl[position + 1] == '*';
        }

        private static string ReadBlockComment(DslScript dslScript, ref int end)
        {
            string dsl = dslScript.Script;
            end += 2;
            int begin = end;
            while (true)
            {
                if (end >= dsl.Length)
                {
                    var errorMessage = $"Unexpected end of file within a block comment. Expected '*/'.";
                    throw new DslSyntaxException(errorMessage, "RH0013", dslScript, begin, dsl.Length, null);
                }
                if (IsBlockCommentEnd(dsl, end))
                {
                    end += 2;
                    break;
                }
                end++;
            }
            return dsl.Substring(begin, end - 2 - begin);
        }

        private static bool IsBlockCommentEnd(string dsl, int position)
        {
            return position + 1 < dsl.Length && dsl[position] == '*' && dsl[position + 1] == '/';
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
            int begin = end;
            while (end < dsl.Length && IsSimpleStringElement(dsl[end]))
                end++;
            return dsl.Substring(begin, end - begin);
        }

        private static bool IsQuotedStringStart(char c)
        {
            return c == '"' || c == '\'';
        }

        private static string ReadQuotedString(DslScript dslScript, ref int end)
        {
            string script = dslScript.Script;
            char quote = script[end];
            int begin = end;
            end++;

            while (true)
            {
                while (end < script.Length && script[end] != quote)
                    end++;
                if (end >= script.Length)
                {
                    var errorMessage = $"Unexpected end of file within a quoted string. Missing closing character: {quote}.";
                    throw new DslSyntaxException(errorMessage, "RH0008", dslScript, begin, script.Length, null);
                }
                if (end + 1 < script.Length && script[end + 1] == quote)
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

            return script.Substring(begin + 1, end - begin - 2).Replace(new string(quote, 2), new string(quote, 1));
        }

        private static bool IsExternalTextStart(char c)
        {
            return c == '<';
        }

        private string ReadExternalText(DslScript dslScript, ref int end)
        {
            string script = dslScript.Script;
            int begin = end;
            end++;

            while (end < script.Length && script[end] != '>' && !invalidPathChars.Contains(script[end]))
                end++;

            if (end >= script.Length)
            {
                var errorMessage = "Unexpected end of script within external text reference. Missing closing character: '>'.";
                throw new DslSyntaxException(errorMessage, "RH0009", dslScript, begin, script.Length, null);
            }

            if (script[end] != '>')
            {
                var errorMessage = "Invalid filename character within external text reference.";
                throw new DslSyntaxException(errorMessage, "RH0010", dslScript, end, 0, null);
            }

            end++; // Skip closing character.

            string relativePathOrResourceName = script.Substring(begin + 1, end - begin - 2);
            var externalText = _externalTextReader.Read(dslScript, relativePathOrResourceName);

            if (!externalText.IsError)
                return externalText.Value;
            else
                throw new DslSyntaxException(externalText.Error, "RH0012", dslScript, begin, end, null);
        }
    }
}
