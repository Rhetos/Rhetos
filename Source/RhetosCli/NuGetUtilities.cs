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

using NuGet.Frameworks;
using NuGet.ProjectModel;
using Rhetos.Deployment;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos
{
    internal class NuGetUtilities
    {
        private readonly LockFile _lockFile;
        private readonly NuGetFramework _targetFramework;

        public NuGetUtilities(string projectRootFolder, ILogProvider logProvider, string target)
        {
            var objFolderPath = Path.Combine(projectRootFolder, "obj");
            if (!Directory.Exists(objFolderPath))
                throw new FrameworkException($"Project object files folder '{objFolderPath}' does not exist. Please make sure that a valid project folder is specified, and run NuGet restore before build.");
            var path = Path.Combine(objFolderPath, "project.assets.json");
            if (!File.Exists(path))
                throw new FrameworkException("The project.assets.json file does not exist. Switch to NuGet's PackageReference format type for your project.");
            _lockFile = LockFileUtilities.GetLockFile(path, new NuGetLogger(logProvider));
            _targetFramework = ResolveTargetFramework(target);
        }

        private NuGetFramework ResolveTargetFramework(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                var targets = _lockFile.Targets.Select(x => x.TargetFramework).Distinct();
                if (targets.Count() > 1)
                {
                    //TODO: Add the option name with which the target framework should be pass to  RhetosCli after it is defined
                    throw new FrameworkException("There are multiple targets set. Pass the target version with the command line option.");
                }
                if (!targets.Any())
                    throw new FrameworkException("No target framework found for the selected project.");

                return targets.First();
            }
            else
            {
                return NuGetFramework.Parse(target);
            }
        }

        internal List<string> GetBuildAssemblies()
        {
            return GetTargetFrameworkLibraries()
                .Select(targetLibrary => new { PackageFolder = GetPackageFolderForLibrary(targetLibrary), targetLibrary.CompileTimeAssemblies })
                .SelectMany(targetLibrary => targetLibrary.CompileTimeAssemblies.Select(libFile => Path.Combine(targetLibrary.PackageFolder, GetNormalizedNugetPaths(libFile.Path))))
                .Where(libFile => Path.GetExtension(libFile) == ".dll")
                .ToList();
        }

        internal InstalledPackages GetInstalledPackages()
        {
            var installedPackages = new List<InstalledPackage>();
            var targetLibraries = GetTargetFrameworkLibraries();
            foreach (var targetLibrary in targetLibraries)
            {
                var packageFolder = GetPackageFolderForLibrary(targetLibrary);
                var library = _lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                var contentFiles = library.Files.Select(x => new ContentFile { PhysicalPath = Path.Combine(packageFolder, GetNormalizedNugetPaths(x)), InPackagePath = GetNormalizedNugetPaths(x) }).ToList();
                installedPackages.Add(new InstalledPackage(library.Name, library.Version.Version.ToString(), null, null, null, null, contentFiles));
            }

            return new InstalledPackages { Packages = SortInstalledPackagesByDependencies(targetLibraries, installedPackages) };
        }

        private IList<LockFileTargetLibrary> GetTargetFrameworkLibraries()
        {
            return _lockFile.Targets.Single(x => x.TargetFramework == _targetFramework && x.RuntimeIdentifier == null).Libraries.Where(x => x.Type == "package").ToList();
        }

        private List<InstalledPackage> SortInstalledPackagesByDependencies(IList<LockFileTargetLibrary> targetLibraries, List<InstalledPackage> installedPackages)
        {
            var packages = targetLibraries.Select(x => x.Name).ToList();
            var dependencies = targetLibraries.SelectMany(x => x.Dependencies.Select(y => new Tuple<string, string>(x.Name, y.Id)));
            Graph.TopologicalSort(packages, dependencies);
            packages.Reverse();
            Graph.SortByGivenOrder(installedPackages, packages, x => x.Id);
            return installedPackages;
        }

        private string GetPackageFolderForLibrary(LockFileTargetLibrary targetLibrary)
        {
            var library = _lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
            var packageFolder = _lockFile.PackageFolders
                .Select(x => Path.Combine(x.Path, GetNormalizedNugetPaths(library.Path)))
                .FirstOrDefault(x => Directory.Exists(x));
            if (packageFolder == null)
                throw new FrameworkException($"Could not locate the folder for package '{library.Name}'.");
            return packageFolder;
        }

        private string GetNormalizedNugetPaths(string nugetPath) => nugetPath.Replace('/', '\\');
    }
}
