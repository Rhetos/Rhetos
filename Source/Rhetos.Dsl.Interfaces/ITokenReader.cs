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

namespace Rhetos.Dsl
{
    public interface ITokenReader
    {
        /// <summary>
        /// Throws exception if there are no more tokens. Throws exception if next token is a special token.
        /// </summary>
        string ReadText();

        /// <summary>
        /// Throws exception if next token does not equal "value". Ignores case. Exception will contain text from 'reason' argument.
        /// </summary>
        void Read(string value, string reason);

        /// <summary>
        /// Returns false if next token does not equal "value". Ignores case.
        /// </summary>
        bool TryRead(string value);

        int CurrentPosition { get; }
    }
}
