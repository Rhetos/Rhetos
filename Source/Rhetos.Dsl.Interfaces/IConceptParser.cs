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
using Rhetos.Utilities;
using System.Collections.Generic;

namespace Rhetos.Dsl
{
    public interface IConceptParser
    {
        /// <summary>
        /// If the keyword is not recognized return empty error string.
        /// If the keyword is recognized, but the syntax is wrong, return error description.
        /// </summary>
        ValueOrError<IConceptInfo> Parse(ITokenReader tokenReader, Stack<IConceptInfo> context);
    }
}
