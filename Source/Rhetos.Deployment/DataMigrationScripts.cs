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
using System.IO;

namespace Rhetos.Deployment
{
    public class DataMigrationScripts
    {
        /// <summary>
        /// The scripts are sorted by the intended execution order.
        /// </summary>
        public List<DataMigrationScript> Scripts { get; set; }

        /// <summary>
        /// Replace "/" or "\" in script paths to Path.DirectorySeparatorChar, to locate correctly script 
        /// location in cross-platform environment. 
        /// </summary>
        public void ConvertToCrossPlatformPaths() 
        {
            foreach (var file in Scripts)
                file.Path = file.Path
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
