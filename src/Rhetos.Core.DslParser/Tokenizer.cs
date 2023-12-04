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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;

namespace Rhetos.Dsl
{
    /// <summary>
    /// Performs the lexical analysis for DSL scripts: Transforms source text into a list of tokens.
    /// </summary>
    public class Tokenizer : ITokenizer
    {
        private readonly IDslScriptsProvider _dslScriptsProvider;
        private readonly FilesUtility _filesUtility;
        private readonly Lazy<DslSyntax> _syntax;

        public Tokenizer(IDslScriptsProvider dslScriptsProvider, FilesUtility filesUtility, Lazy<DslSyntax> syntax)
        {
            _dslScriptsProvider = dslScriptsProvider;
            _filesUtility = filesUtility;
            _syntax = syntax;
        }

        public TokenizerResult GetTokens()
        {
            var tokens = new List<Token>();
            DslSyntaxException syntaxError = null;

            try
            {
                var tokenizerInternals = new TokenizerInternals(_syntax.Value);

                foreach (var dslScript in _dslScriptsProvider.DslScripts)
                {
                    int scriptPosition = 0;

                    while (true)
                    {
                        TokenizerInternals.SkipWhitespaces(dslScript.Script, ref scriptPosition);
                        if (scriptPosition >= dslScript.Script.Length)
                            break;

                        int startPosition = scriptPosition;
                        Token t = tokenizerInternals.GetNextToken_ValueType(dslScript, ref scriptPosition, _filesUtility.ReadAllText);
                        t.DslScript = dslScript;
                        t.PositionInDslScript = startPosition;
                        t.PositionEndInDslScript = scriptPosition;

                        if (t.Type != TokenType.Comment)
                            tokens.Add(t);
                    }

                    tokens.Add(new Token { DslScript = dslScript, PositionInDslScript = dslScript.Script.Length, PositionEndInDslScript = dslScript.Script.Length, Type = TokenType.EndOfFile, Value = "" });
                }
            }
            catch (DslSyntaxException e)
            {
                syntaxError = e;
            }

            return new TokenizerResult
            {
                Tokens = tokens,
                SyntaxError = syntaxError
            };
        }
    }
}
