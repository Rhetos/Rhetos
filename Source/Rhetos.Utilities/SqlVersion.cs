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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public enum SqlVersion
    {
        /// <summary>
        ///     SQL Server 8 (2000).
        /// </summary>
        Sql8 = 80,

        /// <summary>
        ///     SQL Server 9 (2005).
        /// </summary>
        Sql9 = 90,

        /// <summary>
        ///     SQL Server 10 (2008).
        /// </summary>
        Sql10 = 100,

        /// <summary>
        ///     SQL Server 11 (2012).
        /// </summary>
        Sql11 = 110,

        // Higher versions go here
    }
}
