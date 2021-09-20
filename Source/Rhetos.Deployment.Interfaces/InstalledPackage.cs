using System.IO;
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
using System.Diagnostics;
using System.Text;

namespace Rhetos.Deployment
{
    /// <summary>
    /// Build-time dependency package.
    /// It includes referenced NuGet packages and referenced projects.
    /// </summary>
    [DebuggerDisplay("{Id}")]
    public class InstalledPackage
    {
        public InstalledPackage(
            string id,
            string version,
            IEnumerable<PackageRequest> dependencies,
            string folder,
            List<ContentFile> contentFiles)
        {
            Id = id;
            Version = version;
            Dependencies = dependencies;
            Folder = folder;
            ContentFiles = contentFiles;
        }

        public string Id { get; private set; }

        public string Version { get; private set; }

        public IEnumerable<PackageRequest> Dependencies { get; private set; }

        /// <summary>
        /// The local cache folder at build-time, where the package files are extracted and used by Rhetos.
        /// This is a debug information for build, its value is <see langword="null"/> at run-time.
        /// </summary>
        /// <remarks>
        /// Instead of scanning this folder, use the <see cref="ContentFiles"/> property instead, because the <see cref="Folder"/>
        /// is missing the in-package file paths when using package directly from source, see <see cref="ContentFile.InPackagePath"/>.
        /// </remarks>
        public string Folder { get; private set; }

        public List<ContentFile> ContentFiles { get; private set; }

        public string Report() => $"{Id}, version {Version}, build folder '{Folder}'.";

        /// <summary>
        /// Removing folder and file paths that were available at build-time, to avoid accidentally using them at run-time.
        /// This includes <see cref="Folder"/> and <see cref="ContentFile.PhysicalPath"/>.
        /// </summary>
        public void RemoveBuildTimePaths()
        {
            Folder = null;
            foreach (var file in ContentFiles)
                file.PhysicalPath = null;
        }

        /// <summary>
        /// Replace "/" or "\" in package paths to Path.DirectorySeparatorChar, to locate correctly package
        /// location in cross-platform environment. 
        /// </summary>
        public void ConvertToCrossPlatformPaths() 
        {
            foreach (var file in ContentFiles)
                file.InPackagePath = file.InPackagePath
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
