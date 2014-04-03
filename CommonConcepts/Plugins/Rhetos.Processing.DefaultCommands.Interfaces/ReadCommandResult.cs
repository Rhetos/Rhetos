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

namespace Rhetos.Processing.DefaultCommands
{
    public class ReadCommandResult
    {
        /// Returned only if ReadCommandInfo.ReadTotalCount is set, otherwise it will be null.
        public object[] Records { get; set; }

        /// <summary>
        /// The total number of records that would be returned without paging (ReadCommandInfo.Top or Skip).
        /// The value is returned only if ReadCommandInfo.ReadTotalCount is set.
        /// </summary>
        public int? TotalCount { get; set; }
    }
}
