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
using System.Text;
using System.Globalization;
using Rhetos.Utilities;

namespace Rhetos.Dsl
{
    public class TokenReader : Rhetos.Dsl.ITokenReader
    {
        public int PositionInTokenList { get; private set; }

        List<Token> _tokenList;
        Token CurrentToken { get { return _tokenList[PositionInTokenList]; } }
        
        public void CopyFrom(TokenReader tokenReader)
        {
            this._tokenList = tokenReader._tokenList;
            this.PositionInTokenList = tokenReader.PositionInTokenList;
        }

        public TokenReader(TokenReader tokenReader)
        {
            CopyFrom(tokenReader);
        }

        public TokenReader(List<Token> tokenList, int positionInTokenList)
        {
            this._tokenList = tokenList;
            this.PositionInTokenList = positionInTokenList;
        }

        public ValueOrError<string> ReadText()
        {
            if (PositionInTokenList >= _tokenList.Count || CurrentToken.Type == TokenType.EndOfFile)
                return ValueOrError.CreateError("Tried to read a token past the end of the DSL script.");

            if (CurrentToken.Type != TokenType.Text)
                return ValueOrError.CreateError(string.Format(CultureInfo.InvariantCulture,
                    "Unexpected token type ({0} {1}) while reading text. Use quotes to specify text if that was intended.",
                        CurrentToken.Type,
                        CurrentToken.Value == "'" ? "\"'\"" : "'" + CurrentToken.Value + "'"));

            string result = CurrentToken.Value;
            PositionInTokenList++;
            return result;
        }

        public bool EndOfInput { get { return PositionInTokenList >= _tokenList.Count; } }

        private static void ThrowReadException(string value, string reason)
        {
            throw new DslSyntaxException(string.Format(CultureInfo.InvariantCulture,
                "{0}Expected \"{1}\". ",
                    string.IsNullOrEmpty(reason) ? "" : (reason + " "),
                    value));
        }

        public bool TryRead(string value)
        {
            if (PositionInTokenList >= _tokenList.Count || CurrentToken.Type == TokenType.EndOfFile)
                return false;

            if (!string.Equals(CurrentToken.Value, value, StringComparison.InvariantCultureIgnoreCase))
                return false;

            PositionInTokenList++;

            return true;
        }

        public string ReportPosition()
        {
            
            DslScript dslScript;
            int position;

            if (PositionInTokenList < _tokenList.Count)
            {
                dslScript = CurrentToken.DslScript;
                position = CurrentToken.PositionInDslScript;
            }
            else if (_tokenList.Count > 0)
            {
                dslScript = _tokenList[_tokenList.Count - 1].DslScript;
                position = dslScript.Script.Length;
            }
            else
            {
                dslScript = new DslScript { Script = "", Name = "", Path = "" };
                position = 0;
            }
            return ScriptPositionReporting.ReportPosition(dslScript.Script, position, dslScript.Path);
        }

        /// <summary>
        /// This method should only be called between parsing two concepts.
        /// </summary>
        public void SkipEndOfFile()
        {
            while (PositionInTokenList < _tokenList.Count && CurrentToken.Type == TokenType.EndOfFile)
                PositionInTokenList++;
        }
    }
}