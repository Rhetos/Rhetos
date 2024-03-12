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
using System.Diagnostics;

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

        /// <summary>
        /// SQL script that will be executed on database update.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// SQL script that will be executed on database update when reverting to an older version of the application.
        /// The "down" script is loaded from database table Rhetos.DataMigrationScript and executed,
        /// if the currently deployed application version does not include a data-migrations script with the same <see cref="Tag"/>.
        /// </summary>
        public string Down { get; set; }

        private string _orderWithinPackage;

        /// <summary>
        /// Ordering of scripts between packages is defined by the package dependencies from a .nuspec file.
        /// </summary>
        private string OrderWithinPackage
        {
            get { return _orderWithinPackage ??= ComputeOrderWithinPackage(Path); }
        }

        private static string ComputeOrderWithinPackage(string path)
        {
            return CsUtility.GetNaturalSortString(path).Replace(@"\", @" \").Replace(@"/", @" /").ToUpperInvariant();
        }

        /// <summary>
        /// This works correctly only on scripts from the same package.
        /// Ordering of scripts between packages is defined by the package dependencies from a .nuspec file.
        /// </summary>
        public int CompareTo(DataMigrationScript other)
        {
#pragma warning disable CA1309 // Use ordinal string comparison. Suppressed for backward compatibility.
            return string.Compare(OrderWithinPackage, other.OrderWithinPackage, StringComparison.InvariantCultureIgnoreCase);
#pragma warning restore CA1309 // Use ordinal string comparison
        }
    }
}
