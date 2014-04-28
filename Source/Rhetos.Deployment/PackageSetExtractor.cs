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
using System.IO;
using System.Text;
using Ionic.Zip;
using System.Linq;
using Rhetos.Utilities;

namespace Rhetos.Deployment
{
    public static class PackageSetExtractor
    {
        public const string FldDslScripts = "DslScripts";
        public const string FldPlugins = "Plugins";
        public const string FldResources = "Resources";
        public const string FldPackageInfo = "PackageInfo";
        public const string FldDataMigration = "DataMigration";
        public const string PackageInfoFileName = "PackageInfo.xml";

        private class DecompressedPackage
        {
            public Package Package { get; set; }
            public string TempDecompressedPath { get; set; }
        }

        public static void ExtractAndCombinePackages(string[] packages, string targetFolder)
        {
            string tempFolder = Path.Combine(targetFolder, "Temp");
            var pckgInfos = DecompressPackages(packages, tempFolder);
            CopyPackageFiles(pckgInfos, targetFolder);
            SafeDeleteDirectory(tempFolder);
        }

        /// <summary>
        /// Copies files from temporary folder to target folder. Reorganizes files to match target folder structure.
        /// Temp folders are organized "package\resource", target folders are organized "resource\package".
        /// </summary>
        private static void CopyPackageFiles(IEnumerable<DecompressedPackage> pckgInfos, string targetFolder)
        {
            CopyPackagePart(pckgInfos, targetFolder, FldPlugins, Path.Combine("bin", FldPlugins), true);
            CopyPackagePart(pckgInfos, targetFolder, FldResources, FldResources);
            CopyPackagePart(pckgInfos, targetFolder, FldDslScripts, FldDslScripts);
            CopyPackagePart(pckgInfos, targetFolder, FldDataMigration, FldDataMigration);
            CopyPackageInfo(pckgInfos, targetFolder);
        }

        private static void CopyPackageInfo(IEnumerable<DecompressedPackage> pckgInfos, string targetRoot)
        {
            string targetResourceFolder = Path.Combine(targetRoot, FldPackageInfo);
            SafeDeleteDirectory(targetResourceFolder);
            SafeCreateDirectory(targetResourceFolder);

            foreach (var pckgInfo in pckgInfos)
            {
                string source = Path.Combine(pckgInfo.TempDecompressedPath, PackageInfoFileName);
                string destination = Path.Combine(targetResourceFolder, pckgInfo.Package.Identifier + ".xml");
                SafeMoveFile(source, destination);
            }
        }

        private static void CopyPackagePart(IEnumerable<DecompressedPackage> pckgInfos, string targetRoot, string sourceSubfolder, string targetSubfolder, bool flat = false)
        {
            string targetResourceFolder = Path.Combine(targetRoot, targetSubfolder);
            SafeDeleteDirectory(targetResourceFolder);
            SafeCreateDirectory(targetResourceFolder); // An empty folder should exist even if there are no packages.

            foreach (var pckgInfo in pckgInfos)
            {
                string sourcePackagePart = Path.Combine(pckgInfo.TempDecompressedPath, sourceSubfolder);
                if (!Directory.Exists(sourcePackagePart))
                    continue;

                string targetPackageResourceFolder = targetResourceFolder;
                if (!flat)
                    targetPackageResourceFolder = Path.Combine(targetPackageResourceFolder, pckgInfo.Package.Identifier);

                foreach (var file in Directory.EnumerateFiles(sourcePackagePart, "*", SearchOption.AllDirectories))
                {
                    string subPath;
                    if (!flat)
                        subPath = Path.GetFullPath(file).Substring(Path.GetFullPath(sourcePackagePart).Length + 1);
                    else
                        subPath = Path.GetFileName(file);

                    string targetFile = Path.Combine(targetPackageResourceFolder, subPath);
                    SafeMoveFile(file, targetFile);
                }
            }
        }

        private static IEnumerable<DecompressedPackage> DecompressPackages(IEnumerable<string> packageFiles, string tempFolder)
        {
            var pckgInfos = packageFiles.Select(file => DecompressPackage(file, tempFolder)).ToList();

            var packagesByKey = pckgInfos.ToDictionary(p => p.Package.Identifier);

            var packageDependencies = 
                (from pckgInfo in pckgInfos
                from dependency in pckgInfo.Package.Dependencies
                select Tuple.Create(
                    packagesByKey.GetValue(dependency.Identifier, "Package " + pckgInfo.Package.Identifier + " has depedency on nonexistent package {0} in " + PackageInfoFileName + "."),
                    pckgInfo)).ToList();
    
            Graph.TopologicalSort(pckgInfos, packageDependencies);

            return pckgInfos;
        }

        private static DecompressedPackage DecompressPackage(string file, string targetRootPath)
        {
            if (!File.Exists(file))
                throw new ApplicationException(String.Format("File {0} does not exist.", file));

            var pckgInfo = new DecompressedPackage
            {
                TempDecompressedPath = Path.Combine(targetRootPath, Path.GetFileNameWithoutExtension(file))
            };
            DecompressFile(file, pckgInfo.TempDecompressedPath);
            pckgInfo.Package = ReadPackageInfo(pckgInfo.TempDecompressedPath, file);

            return pckgInfo;
        }

        private static void DecompressFile(string fileName, string targetFolder)
        {
            SafeCreateDirectory(targetFolder);

            using (var zipFile = ZipFile.Read(fileName))
                foreach (var zipEntry in zipFile)
                    zipEntry.Extract(targetFolder, ExtractExistingFileAction.OverwriteSilently);
        }

        public static Package ReadPackageInfo(string path, string errorContextPackagePath)
        {
            string file = Path.Combine(path, PackageInfoFileName);

            if (!File.Exists(file))
                throw new ApplicationException(String.Format("Package '{0}' does not contain package information stored in {1}.",
                    errorContextPackagePath, PackageInfoFileName));

            return Package.FromFile(file);
        }

        public static string[] ReadPackageList(string file)
        {
            file = Path.GetFullPath(file);
            string content = File.ReadAllText(file, Encoding.Default);

            string path = new FileInfo(file).DirectoryName;

            var lines = content.Split(Environment.NewLine.ToCharArray()).Select(fileName => fileName.Trim()).Where(fileName => !string.IsNullOrEmpty(fileName));
            return lines.Select(fileName => Path.Combine(path, fileName)).ToArray();
        }

        private static void Retry(Action action, string actionName)
        {
            const int maxTries = 10;
            for (int tries = maxTries; tries > 0; tries--)
            {
                try
                {
                    action();

                    //if (tries < maxTries)
                        //Console.WriteLine(" ... succeded.");
                    break;
                }
                catch
                {
                    if (tries <= 1)
                    {
                        //if (tries < maxTries)
                            //Console.WriteLine(" ... unsuccessful.");
                        throw;
                    }

                    //if (tries == maxTries)
                        //Console.Write(actionName + " failed");
                    //Console.Write(" ... retrying");
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
            }
        }

        private static void SafeCreateDirectory(string path)
        {
            try
            {
                // When using TortoiseHg and the Rhetos folder is opened in Windows Explorer,
                // Directory.CreateDirectory() will stochastically fail with UnauthorizedAccessException or DirectoryNotFoundException.
                Retry(() => Directory.CreateDirectory(path), "CreateDirectory");
                //Console.WriteLine("Created directory " + path);
            }
            catch (Exception ex)
            {
                throw new FrameworkException(String.Format("Can't create directory '{0}'. Check that it's not locked.", path), ex);
            }
        }

        private static void SafeDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                File.SetAttributes(path, FileAttributes.Normal);

                foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(dir, FileAttributes.Normal);

                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);

                Retry(() => Directory.Delete(path, true), "Directory.Delete");
                //Console.WriteLine("Deleted directory " + path);

                Retry(() => { if (Directory.Exists(path)) throw new FrameworkException("Failed to delete directory " + path); }, "Directory.Exists");
            }
            catch (Exception ex)
            {
                throw new FrameworkException(String.Format("Can't delete directory '{0}'. Check that it's not locked.", path), ex);
            }
        }

        private static void SafeMoveFile(string source, string destination)
        {
            try
            {
                SafeCreateDirectory(Path.GetDirectoryName(destination)); // Less problems with locked folders if the directory is created before moving the file. Locking may occur when using TortoiseHg and the Rhetos folder is opened in Windows Explorer.
                Retry(() => File.Move(source, destination), "File.Move");
            }
            catch (Exception ex)
            {
                throw new FrameworkException(String.Format("Can't move file '{0}' to '{1}'. Check that destination file or folder is not locked.", source, destination), ex);
            }
        }
    }
}
