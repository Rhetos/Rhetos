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
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Deployment
{

    [DebuggerDisplay("{Id}")]
    public class InstalledPackage
    {
        public InstalledPackage(
            string id,
            string version,
            IEnumerable<PackageRequest> dependencies,
            string folder,
            PackageRequest request,
            string source,
            List<ContentFile> contentFiles = null)
        {
            Id = id;
            Version = version;
            Dependencies = dependencies;
            Folder = folder;
            Request = request;
            Source = source;
            ContentFiles = contentFiles ?? FilesFromFolder(folder, id);
        }

        public string Id { get; private set; }

        public string Version { get; private set; }

        public IEnumerable<PackageRequest> Dependencies { get; private set; }

        /// <summary>The local folder where the package files are extracted and used by Rhetos.</summary>
        ///<remarks>Instead of scanning this folder, use the <see cref="ContentFiles"/> property instead, because the <see cref="Folder"/>
        ///is missing the in-package file paths when using package directly from source, <see cref="ContentFile.InPackagePath"/>.</remarks>
        public string Folder { get; private set; }

        public List<ContentFile> ContentFiles { get; private set; }

        public PackageRequest Request { get; private set; }

        /// <summary>URI or a folder where the package was downloaded from.</summary>
        public string Source { get; set; }

        public string Report()
        {
            return Id + " " + Version + " (requested from " + Request.RequestedBy + ") in " + Folder + ".";
        }

        /// <summary>
        /// Local paths should be absolute in runtime to avoid ambiguity of current working folder when using the Rhetos server object model from other applications.
        /// </summary>
        public void SetAbsoluteFolderPath(string rhetosAppRootPath)
        {
            Folder = FilesUtility.RelativeToAbsolutePath(rhetosAppRootPath, Folder);
            foreach (var file in ContentFiles)
                file.PhysicalPath = FilesUtility.RelativeToAbsolutePath(rhetosAppRootPath, file.PhysicalPath);
        }

        /// <summary>
        /// Local paths should be relative when saving the path to a cache file, to allow moving the Rhetos server folder to testing environment or production.
        /// </summary>
        public void SetRelativeFolderPath(string rhetosAppRootPath)
        {
            Folder = FilesUtility.AbsoluteToRelativePath(rhetosAppRootPath, Folder);
            foreach (var file in ContentFiles)
                file.PhysicalPath = FilesUtility.AbsoluteToRelativePath(rhetosAppRootPath, file.PhysicalPath);
        }

        private static List<ContentFile> FilesFromFolder(string folder, string packageId)
        {
            if (!Directory.Exists(folder))
                throw new FrameworkException($"Source folder for package '{packageId}' does not exist: '{folder}'.");

            return Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Select(file => new ContentFile
                {
                    PhysicalPath = file,
                    InPackagePath = FilesUtility.AbsoluteToRelativePath(folder, file)
                })
                .ToList();
        }
    }
}
