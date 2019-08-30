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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Deployment
{
    [DebuggerDisplay("{Path}")]
    public class DataMigrationScript : IComparable<DataMigrationScript>
    {
        public string Tag { get; set; }
        /// <summary>
        /// Full name of the script, including package name, subfolder path, and file name.
        /// This is not an actual file path on disk.
        /// </summary>
        public string Path { get; set; }
        public string Content { get; set; }

        private string _orderWithinPackage;

        /// <summary>
        /// Ordering of scripts between packages is defined by the package dependencies from a .nuspec file.
        /// </summary>
        private string OrderWithinPackage
        {
            get { return _orderWithinPackage ?? (_orderWithinPackage = ComputeOrder(Path)); }
        }

        private static string ComputeOrder(string s)
        {
            return CsUtility.GetNaturalSortString(s).Replace(@"\", @" \").ToLower();
        }

        /// <summary>
        /// This works correctly only on scripts from the same package.
        /// Ordering of scripts between packages is defined by the package dependencies from a .nuspec file.
        /// </summary>
        public int CompareTo(DataMigrationScript other)
        {
            return string.Compare(OrderWithinPackage, other.OrderWithinPackage, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
