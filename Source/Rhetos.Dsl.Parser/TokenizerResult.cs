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
using System.Collections.Generic;

namespace Rhetos.Dsl
{
    /// <summary>
    /// In case of a syntax error, <see cref="DslSyntaxException"/> will contain the error, and <see cref="Tokens"/> will contain the parsed tokens up to the point of error.
    /// This behavior is useful for external DSL analysis, such as DSL IntelliSense plugin.
    /// </summary>
    public class TokenizerResult
    {
        /// <summary>
        /// Verify <see cref="SyntaxError"/> before using <see cref="Tokens"/>.
        /// </summary>
        public List<Token> Tokens { get; set; }

        public DslSyntaxException SyntaxError { get; set; }
    }
}