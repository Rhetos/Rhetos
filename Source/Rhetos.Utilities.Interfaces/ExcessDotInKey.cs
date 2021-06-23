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

namespace Rhetos.Utilities
{
    /// <summary>
    /// Before Rhetos v4.0, dot character was expected before string key parameter of current statement.
    /// Since Rhetos v4.0, dot should only be used for separating key parameters of referenced concept,
    /// but legacy syntax is allowed by setting this option to <see cref="Ignore"/> or <see cref="Warning"/>.
    /// </summary>
    public enum ExcessDotInKey { Ignore = 0, Warning = 1, Error = 2 }; // Numeric values are important for backward compatibility of serialized options in DslSyntax class.
}
