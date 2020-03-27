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

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using Rhetos.Deployment;
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
        private readonly string _projectRootFolder;
        private readonly IEnumerable<string> _projectContentFiles;

        public string ProjectName { get { return _lockFile.PackageSpec.Name; } }

        public NuGetUtilities(string projectRootFolder, IEnumerable<string> projectContentFiles, NuGetLogger logger, string target)
        {
            _projectRootFolder = projectRootFolder;
            _projectContentFiles = projectContentFiles;
            var objFolderPath = Path.Combine(_projectRootFolder, "obj");
            if (!Directory.Exists(objFolderPath))
                throw new FrameworkException($"Project object files folder '{objFolderPath}' does not exist. Please make sure that a valid project folder is specified, and run NuGet restore before build.");
            var path = Path.Combine(objFolderPath, LockFileFormat.AssetsFileName);
            if (!File.Exists(path))
                throw new FrameworkException($"The {LockFileFormat.AssetsFileName} file does not exist. Switch to NuGet's PackageReference format type for your project.");
            _lockFile = LockFileUtilities.GetLockFile(path, logger);
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

        internal List<InstalledPackage> GetInstalledPackages()
        {
            var installedPackages = new List<InstalledPackage>();
            var targetLibraries = GetSupportedTargetFrameworkLibraries().ToList();
            foreach (var targetLibrary in targetLibraries)
            {
                var packageFolder = GetFolderForLibrary(targetLibrary);
                var dependencies = targetLibrary.Dependencies.Select(x => new PackageRequest { Id = x.Id, VersionsRange = x.VersionRange.OriginalString });
                var library = _lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                var contentFiles = library.Files.Select(x => new ContentFile { PhysicalPath = Path.Combine(packageFolder, PathUtility.GetPathWithBackSlashes(x)), InPackagePath = PathUtility.GetPathWithBackSlashes(x) }).ToList();
                installedPackages.Add(new InstalledPackage(library.Name, library.Version.Version.ToString(), dependencies, packageFolder, null, null, contentFiles));
            }
            
            var sortedPackages = SortInstalledPackagesByDependencies(targetLibraries, installedPackages);
            sortedPackages.Add(GetProjectAsInstalledPackage());
            return sortedPackages;
        }

        private InstalledPackage GetProjectAsInstalledPackage()
        {
            var contentFiles = _projectContentFiles.Select(f => new ContentFile { PhysicalPath = f, InPackagePath = FilesUtility.AbsoluteToRelativePath(_projectRootFolder, f) }).ToList();
            var dependencies = _lockFile.PackageSpec.TargetFrameworks.Single(x => x.FrameworkName == _targetFramework).Dependencies.Select(x => new PackageRequest { Id = x.Name, VersionsRange = x.LibraryRange.VersionRange.OriginalString });
            return new InstalledPackage(ProjectName, "", dependencies, _projectRootFolder, null, null, contentFiles);
        }

        internal List<string> GetRuntimeAssembliesFromPackages()
        {
            return GetSupportedTargetFrameworkLibraries().Where(x => x.Type == LibraryType.Package)
                .Select(targetLibrary => new { PackageFolder = GetFolderForPackageLibraray(targetLibrary), targetLibrary.RuntimeAssemblies })
                .SelectMany(targetLibrary => targetLibrary.RuntimeAssemblies.Select(libFile => Path.Combine(targetLibrary.PackageFolder, PathUtility.GetPathWithBackSlashes(libFile.Path))))
                .Where(libFile => Path.GetExtension(libFile) == ".dll")
                .ToList();
        }

        private IList<LockFileTargetLibrary> GetSupportedTargetFrameworkLibraries()
        {
            return _lockFile.Targets.Single(x => x.TargetFramework == _targetFramework && x.RuntimeIdentifier == null).Libraries
                .Where(x => x.Type == LibraryType.Package || x.Type == LibraryType.Project).ToList();
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

        private string GetFolderForLibrary(LockFileTargetLibrary targetLibrary)
        {
            if (targetLibrary.Type == LibraryType.Package)
                return GetFolderForPackageLibraray(targetLibrary);
            else if(targetLibrary.Type == LibraryType.Project)
                return GetFolderForProjectLibraray(targetLibrary);
            else
                throw new NotSupportedException($"The only supported library types when parsing {LockFileFormat.AssetsFileName} are {LibraryType.Package} and {LibraryType.Project}.");
        }

        private string GetFolderForPackageLibraray(LockFileTargetLibrary targetLibrary)
        {
            var library = _lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
            var packageFolder = _lockFile.PackageFolders
                .Select(x => Path.Combine(x.Path, PathUtility.GetPathWithBackSlashes(library.Path)))
                .Union(new List<string> { Path.GetFullPath(Path.Combine(_projectRootFolder, library.Path)) })
                .FirstOrDefault(x => Directory.Exists(x));
            if (packageFolder == null)
                throw new FrameworkException($"Could not locate the folder for package '{library.Name}'.");
            return packageFolder;
        }

        private string GetFolderForProjectLibraray(LockFileTargetLibrary targetLibrary)
        {
            var library = _lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
            var csprojFile = Path.GetFullPath(Path.Combine(_projectRootFolder, library.Path));
            if (!File.Exists(csprojFile))
                throw new FrameworkException($"Could not locate the folder for package '{library.Name}'.");
            return Path.GetDirectoryName(csprojFile);
        }
    }
}
