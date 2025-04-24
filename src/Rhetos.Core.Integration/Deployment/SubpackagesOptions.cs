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

namespace Rhetos.Deployment
{
    /// <summary>
    /// Build-time options.
    /// </summary>
    [Options("Rhetos")]
    public class SubpackagesOptions
    {
        /// <summary>
        /// Dictionary key is the subpackage name.
        /// </summary>
        public Subpackage[] Subpackages { get; set; }
    }

    public class Subpackage
    {
        /// <summary>
        /// The full package name (package ID) may be different from its <see cref="Name"/>. See <see cref="GetPackageId"/>.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// See <see cref="GetPackageId"/>.
        /// </summary>
        public bool AddProjectNameToPackageId { get; set; } = true;

        /// <summary>
        /// The full package name (package ID) is be constructed by adding project name to the subpackage name: "{projectName}.{subpackage.Name}", if <see cref="AddProjectNameToPackageId"/> is enabled.
        /// Otherwise, the package ID is same as <see cref="Name"/>.
        /// </summary>
        public string GetPackageId(string projectName) => AddProjectNameToPackageId ? $"{projectName}.{Name}" : Name;

        /// <summary>
        /// Subfolder path relative to the .csproj file.
        /// The folder contains all files of the subpackage.
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        /// Names of the referenced subpacakages.
        /// </summary>
        public string[] Dependencies { get; set; }
    }
}
