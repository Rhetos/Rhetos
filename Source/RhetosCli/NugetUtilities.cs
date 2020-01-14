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

using Rhetos.Deployment;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos
{
    internal class NugetUtilities
    {
        internal static NuGet.ProjectModel.LockFile GetLockFile(string projectRootFolder)
        {
            return NuGet.ProjectModel.LockFileUtilities.GetLockFile(Path.Combine(projectRootFolder, "obj", "project.assets.json"), null);
        }

        internal static NuGet.Frameworks.NuGetFramework GetTargetFramework(NuGet.ProjectModel.LockFile lockFile, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                var targets = lockFile.Targets.Select(x => x.TargetFramework).Distinct();
                if (targets.Count() > 1)
                    throw new FrameworkException("There are multiple targets set. Pass the target version through the command line???");
                if (targets.Count() == 0)
                    throw new FrameworkException("No targets???");

                return targets.First();
            }
            else
            {
                return NuGet.Frameworks.NuGetFramework.Parse(target);
            }
        }

        internal static InstalledPackages GetInstalledPackages(NuGet.ProjectModel.LockFile lockFile, NuGet.Frameworks.NuGetFramework targetFramework)
        {
            var librariesForTargetFramework = lockFile.Targets.First(x => x.TargetFramework == targetFramework).Libraries;

            var installedPackages = new List<InstalledPackage>();
            foreach (var targetLibrary in librariesForTargetFramework)
            {
                var library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                var packageFolder = GetPackageFolderForLibrary(lockFile, library);
                var contentFiles = library.Files.Select(x => new ContentFile { PhysicalPath = Path.Combine(packageFolder, x.Replace('/', '\\')), InPackagePath = x.Replace('/', '\\') }).ToList();
                installedPackages.Add(new InstalledPackage(library.Name, library.Version.Version.ToString(), null, null, null, null, contentFiles));
            }

            var packages = librariesForTargetFramework.Select(x => x.Name).ToList();
            var dependencies = librariesForTargetFramework.Select(x => x.Dependencies.Select(y => new Tuple<string, string>(x.Name, y.Id))).SelectMany(x => x);
            Graph.TopologicalSort(packages, dependencies);
            Graph.SortByGivenOrder(installedPackages, packages, x => x.Id);

            return new InstalledPackages { Packages = installedPackages };
        }

        internal static List<string> GetBuildAssemblies(NuGet.ProjectModel.LockFile lockFile, NuGet.Frameworks.NuGetFramework targetFramework)
        {
            var buildAssemblies = new List<string>();
            var librariesForTargetFramework = lockFile.Targets.First(x => x.TargetFramework == targetFramework).Libraries;
            foreach (var targetLibrary in librariesForTargetFramework)
            {
                var library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                var packageFolder = GetPackageFolderForLibrary(lockFile, library);
                buildAssemblies.AddRange(targetLibrary.CompileTimeAssemblies.Select(y => Path.Combine(packageFolder, y.Path.Replace('/', '\\'))));
            }

            return buildAssemblies.Where(x => !x.EndsWith("_._")).ToList();
        }

        internal static string GetPackageFolderForLibrary(NuGet.ProjectModel.LockFile lockFile, NuGet.ProjectModel.LockFileLibrary library)
        {
            //TODO: It should be checked if this is the correct way to resolve the nuget package folder
            var packageFolder = lockFile.PackageFolders.Select(x => Path.Combine(x.Path, library.Path.Replace('/', '\\'))).FirstOrDefault(x => Directory.Exists(x));
            if (packageFolder == null)
                throw new FrameworkException($"Could not locate the folder for package {library.Name};");
            return packageFolder;
        }

    }
}
