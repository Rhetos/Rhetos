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
    internal static class NugetUtilities
    {
        internal static NuGet.ProjectModel.LockFile FindLockFile(string projectRootFolder)
        {
            var path = Path.Combine(projectRootFolder, "obj", "project.assets.json");
            if (!File.Exists(path))
                throw new FrameworkException("The project.assets.json file does not exist. Switch to Nuget's PackageReference format type for your project.");
            return NuGet.ProjectModel.LockFileUtilities.GetLockFile(path, null);
        }

        internal static NuGet.Frameworks.NuGetFramework ResolveTargetFramework(NuGet.ProjectModel.LockFile lockFile, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                var targets = lockFile.Targets.Select(x => x.TargetFramework).Distinct();
                if (targets.Count() > 1)
                {
                    //TODO: Add the option name with which the target framework should be pass to  RhetosCli after it is defined
                    throw new FrameworkException("There are multiple targets set. Pass the target version with the command line option.");
                }
                if (targets.Count() == 0)
                    throw new FrameworkException("No target framework found for the selected project.");

                return targets.First();
            }
            else
            {
                return NuGet.Frameworks.NuGetFramework.Parse(target);
            }
        }

        internal static List<string> GetBuildAssemblies(NuGet.ProjectModel.LockFile lockFile, NuGet.Frameworks.NuGetFramework targetFramework)
        {
            return GetTargetFrameworkLibraries(lockFile, targetFramework).Select(x => new { PackageFolder = GetPackageFolderForLibrary(lockFile, x), x.CompileTimeAssemblies })
                .SelectMany(x => x.CompileTimeAssemblies.Select(y => Path.Combine(x.PackageFolder, GetNormalizedNugetPaths(y.Path))))
                .Where(x => Path.GetExtension(x) == ".dll").ToList();
        }

        internal static InstalledPackages GetInstalledPackages(NuGet.ProjectModel.LockFile lockFile, NuGet.Frameworks.NuGetFramework targetFramework)
        {
            var installedPackages = new List<InstalledPackage>();
            var targetLibraries = GetTargetFrameworkLibraries(lockFile, targetFramework);
            foreach (var targetLibrary in targetLibraries)
            {
                var packageFolder = GetPackageFolderForLibrary(lockFile, targetLibrary);
                var library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                var contentFiles = library.Files.Select(x => new ContentFile { PhysicalPath = Path.Combine(packageFolder, GetNormalizedNugetPaths(x)), InPackagePath = GetNormalizedNugetPaths(x) }).ToList();
                installedPackages.Add(new InstalledPackage(library.Name, library.Version.Version.ToString(), null, null, null, null, contentFiles));
            }

            return new InstalledPackages { Packages = SortInstalledPackagesByDependencies(targetLibraries, installedPackages) };
        }

        private static IList<NuGet.ProjectModel.LockFileTargetLibrary> GetTargetFrameworkLibraries(NuGet.ProjectModel.LockFile lockFile, NuGet.Frameworks.NuGetFramework targetFramework) => lockFile.Targets.Single(x => x.TargetFramework == targetFramework && x.RuntimeIdentifier == null).Libraries;

        private static List<InstalledPackage> SortInstalledPackagesByDependencies(IList<NuGet.ProjectModel.LockFileTargetLibrary> targetLibraries, List<InstalledPackage> installedPackages)
        {
            var packages = targetLibraries.Select(x => x.Name).ToList();
            var dependencies = targetLibraries.Select(x => x.Dependencies.Select(y => new Tuple<string, string>(x.Name, y.Id))).SelectMany(x => x);
            Graph.TopologicalSort(packages, dependencies);
            packages.Reverse();
            Graph.SortByGivenOrder(installedPackages, packages, x => x.Id);
            return installedPackages;
        }

        private static string GetPackageFolderForLibrary(NuGet.ProjectModel.LockFile lockFile, NuGet.ProjectModel.LockFileTargetLibrary targetLibrary)
        {
            var library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
            var packageFolder = lockFile.PackageFolders.Select(x => Path.Combine(x.Path, GetNormalizedNugetPaths(library.Path)))
                .FirstOrDefault(x => Directory.Exists(x));
            if (packageFolder == null)
                throw new FrameworkException($"Could not locate the folder for package {library.Name};");
            return packageFolder;
        }

        private static string GetNormalizedNugetPaths(string nugetPath) => nugetPath.Replace('/', '\\');
    }
}
