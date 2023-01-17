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

using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System;
using System.Linq;

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

        public string Report() => $"{Id}, version {Version}" + (string.IsNullOrEmpty(Folder) ? "" : $", build folder '{Folder}'") + ".";

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

        const string DslScriptsSubfolder = "DslScripts";
        private static readonly string DslScriptsSubfolderPrefix = DslScriptsSubfolder + Path.DirectorySeparatorChar;

        /// <summary>
        /// Extracts all files from a given subfolder into a new (virtual) package, and removes them from the current package.
        /// </summary>
        internal InstalledPackage ExtractSubpackage(Subpackage subpackage)
        {
            string subpackageFolder = Path.GetFullPath(Path.Combine(Folder, subpackage.Folder));
            if (subpackageFolder.Last() != Path.DirectorySeparatorChar)
                subpackageFolder += Path.DirectorySeparatorChar; // Makes sure to avoid selecting files from a subfolder which Name begins with the wanted subfolder name.

            subpackage.Dependencies ??= Array.Empty<string>();

            var virtualPackage = new InstalledPackage(
                Id + "." + subpackage.Name,
                "",
                Dependencies.Concat(subpackage.Dependencies.Select(dependency => new PackageRequest { Id = Id + "." + dependency, VersionsRange = "" })).ToList(),
                subpackageFolder,
                ContentFiles.Where(f => f.PhysicalPath.StartsWith(subpackageFolder, StringComparison.OrdinalIgnoreCase)).ToList());

            ContentFiles.RemoveAll(f => f.PhysicalPath.StartsWith(subpackageFolder, StringComparison.OrdinalIgnoreCase));

            foreach (var file in virtualPackage.ContentFiles)
            {
                // Files in the new packages should have paths relative to the package's folder, instead of the project's root folder.
                if (file.InPackagePath.Length <= subpackage.Folder.Length
                    || (file.InPackagePath[subpackage.Folder.Length] != Path.DirectorySeparatorChar
                    && file.InPackagePath[subpackage.Folder.Length] != Path.AltDirectorySeparatorChar))
                    throw new FrameworkException($"Unexpected InPackagePath of a file '{file.InPackagePath}'." +
                        $" It should start with the subpackage Folder name '{subpackage.Folder}' followed by a directory separator.");
                file.InPackagePath = file.InPackagePath.Substring(subpackage.Folder.Length + 1);

                // DSL scripts in the referenced packages need to be in the DslScripts folder, to match the behavior of DiskDslScriptLoader.LoadPackageScripts.
                if (Path.GetExtension(file.InPackagePath).Equals(".rhe", StringComparison.OrdinalIgnoreCase))
                    if (!file.InPackagePath.StartsWith(DslScriptsSubfolderPrefix, StringComparison.OrdinalIgnoreCase))
                        file.InPackagePath = Path.Combine(DslScriptsSubfolder, file.InPackagePath);
            }

            if (!virtualPackage.ContentFiles.Any() && !Directory.Exists(subpackageFolder))
                throw new ArgumentException($"Subpackage '{subpackage.Name}' directory '{subpackageFolder}' does not exist. Review the Rhetos build settings.");

            return virtualPackage;
        }

        internal void AddDependencies(List<InstalledPackage> createdPackages)
        {
            Dependencies = Dependencies.Concat(createdPackages.Select(p => new PackageRequest { Id = p.Id, VersionsRange = "" })).ToList();
        }
    }
}
