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

using System.Diagnostics;

namespace Rhetos.Utilities
{
    [DebuggerDisplay("{Name ?? CsUtility.Limit(Sql, 100)}")]
    public class SqlBatchScript
    {
        public string Name { get; set; }

        public string Sql { get; set; }

        /// <summary>
        /// If the script is a batch, it will be split by a batch separator ("GO") before executing each part separately.
        /// See <see cref="SqlUtility.SplitBatches(string)"/> for current limitations of the batch scripts.
        /// </summary>
        public bool IsBatch { get; set; }
    };
}
