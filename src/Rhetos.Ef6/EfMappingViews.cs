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

using System.Collections.Generic;

namespace Rhetos.Persistence
{
    public class EfMappingViews
    {
        public Dictionary<string, string> Views { get; set; }

        /// <summary>
        /// Hash value provided by Entity Framework.
        /// It needs to be provided back to Entity Framework when loading EF mapping views from the cache.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Custom hash value base on DSL model, trying to detect if the EF mapping views cache need to be rebuilt.
        /// The <see cref="Hash"/> property is not enough because it does not detect modifying order or properties in the class,
        /// which can results with EntityCommandCompilationException when EF compiles LINQ query to an SQL query.
        /// </summary>
        public string AdditionalHash { get; set; }
    }
}
