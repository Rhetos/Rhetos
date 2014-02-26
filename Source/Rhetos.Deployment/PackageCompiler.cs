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
using System.IO;
using Ionic.Zip;

namespace Rhetos.Deployment
{
    public static class PackageCompiler
    {
        public static string CreatePackage(string packageRootFolder)
        {
            if (!Directory.Exists(packageRootFolder))
                throw new ApplicationException(String.Format("Folder {0} does not exist.", packageRootFolder));

            Package packageInfo = PackageSetExtractor.ReadPackageInfo(packageRootFolder, packageRootFolder);

            if (String.IsNullOrEmpty((packageInfo.Version)))
                throw new ApplicationException("Package information does not contain package version.");
            if (String.IsNullOrEmpty((packageInfo.Identifier)))
                throw new ApplicationException("Package information does not contain package name.");

            DirectoryInfo directoryInfo = new DirectoryInfo(packageRootFolder);
            if (directoryInfo.Parent != null)
                directoryInfo = directoryInfo.Parent;
            var packageFileName = String.Format("{0}_{1}.zip", packageInfo.Identifier, packageInfo.Version.Replace(".", "_"));

            if (!String.IsNullOrEmpty(packageInfo.SpecificFor))
                packageFileName = String.Format("{0}_{1}", packageInfo.SpecificFor, packageFileName);

            packageFileName = Path.Combine(directoryInfo.FullName, packageFileName);

            using (var zipFile = new ZipFile())
            {
                zipFile.ParallelDeflateThreshold = -1; // Workaround for issue http://dotnetzip.codeplex.com/workitem/14252 "ZipFile.Save method get blocked into WaitOne()"

                zipFile.AddFile(Path.Combine(packageRootFolder, PackageSetExtractor.PackageInfoFileName), ".");

                foreach (string readmeFile in ReadmeFiles)
                {
                    string readmeFilePath = Path.Combine(packageRootFolder, readmeFile);
                    if (File.Exists(readmeFilePath))
                        zipFile.AddFile(readmeFilePath, ".");
                }
                
                AddPluginsToZip(zipFile, packageRootFolder);

                AddFolderToZip(zipFile, packageRootFolder, PackageSetExtractor.FldDslScripts);
                AddFolderToZip(zipFile, packageRootFolder, PackageSetExtractor.FldResources);
                AddFolderToZip(zipFile, packageRootFolder, PackageSetExtractor.FldDataMigration);

                if (File.Exists(packageFileName))
                    File.Delete(packageFileName);

                zipFile.Save(packageFileName);
            }

            return packageFileName;
        }

        private static string[] ReadmeFiles = new[] { "Readme.md", "Readme.txt", "Readme.doc", "Readme.docx", "Readme.html" };

        private static void AddFolderToZip(ZipFile zipFile, string packageRootFolder, string addFolder)
        {
            string sourceFolder = Path.Combine(packageRootFolder, addFolder);

            if (Directory.Exists(sourceFolder))
                zipFile.AddDirectory(sourceFolder, addFolder);
        }

        private static void AddPluginsToZip(ZipFile zipFile, string packageRootFolder)
        {
            string sourceFolder = Path.Combine(packageRootFolder, PackageSetExtractor.FldPlugins, "ForDeployment");

            if (Directory.Exists(sourceFolder))
                zipFile.AddDirectory(sourceFolder, PackageSetExtractor.FldPlugins);
        }
    }
}