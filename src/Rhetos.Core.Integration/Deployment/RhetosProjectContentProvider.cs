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

using Newtonsoft.Json;
using Rhetos.Deployment;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos
{
    /// <summary>
    /// Used at Rhetos-MSBuild integration to save the list of project files (including referenced packages).
    /// Used at build-time to load the list of project files.
    /// </summary>
    public class RhetosProjectContentProvider
    {
        private const string ProjectAssetsFileName = "rhetos-project.assets.json";

        public string ProjectAssetsFilePath { get; }

        private readonly FilesUtility _filesUtility;
        private readonly ILogger _logger;
        private readonly string _projectRootFolder;

        public RhetosProjectContentProvider(string projectRootFolder, ILogProvider logProvider)
        {
            ProjectAssetsFilePath = Path.Combine(projectRootFolder, "obj", "Rhetos", ProjectAssetsFileName);
            _filesUtility = new FilesUtility(logProvider);
            _logger = logProvider.GetLogger(GetType().Name);
            _projectRootFolder = projectRootFolder;
        }

        public RhetosProjectContent Load()
        {
            if (!File.Exists(ProjectAssetsFilePath))
            {
                if (Directory.Exists(_projectRootFolder) && Directory.EnumerateFiles(_projectRootFolder, "*.csproj").Any())
                    throw new FrameworkException($"Missing file '{ProjectAssetsFileName}' required for build." +
                        $" The project must include Rhetos NuGet package." +
                        $" If manually running Rhetos build, MSBuild should pass first to create this file.");
                else
                    throw new FrameworkException($"Missing file '{ProjectAssetsFilePath}' required for build." +
                        $" Make sure to specify a valid project folder ({_projectRootFolder}).");
            }

            string serialized = File.ReadAllText(ProjectAssetsFilePath, Encoding.UTF8);
            var rhetosProjectContent = JsonConvert.DeserializeObject<RhetosProjectContent>(serialized, _serializerSettings);
            return rhetosProjectContent;
        }

        /// <summary>
        /// At build-time, the list of packages may be modified to split the current project into multiple "virtual" packages,
        /// if the "Subpackages" configuration option is used.
        /// This feature can be used to control the execution order of DataMigrations scripts within the project,
        /// by specifying dependencies between the subfolders (subpackages).
        /// </summary>
        public void SplitProjectToSubpackages(List<InstalledPackage> packages, SubpackagesOptions subpackagesOptions)
        {
            if (subpackagesOptions.Subpackages == null || !subpackagesOptions.Subpackages.Any())
                return;
            var projectPackage = packages.Where(p => p.Folder.StartsWith(_projectRootFolder, StringComparison.OrdinalIgnoreCase)).Single();

            // Make sure that the created subpackages are ordered by their dependencies.
            List<SubpackageInfo> subpackagesSorted = GetSubpackagesInfoSorted(projectPackage, subpackagesOptions);

            // Extract files from the main project into the subpackages.
            var createdPackages = subpackagesSorted.Select(subpackage => ExtractSubpackage(projectPackage, subpackage)).ToList();
            foreach (var createdPackage in createdPackages)
                _logger.Trace(() => $"Project files moved to virtual subpackage '{createdPackage.Id}':{string.Concat(createdPackage.ContentFiles.Select(f => $"{Environment.NewLine}  {f.InPackagePath}"))}");
            AddDependencies(projectPackage, createdPackages);

            // Add the subpackages before the main project, because any other remaining files in the main project are assumed to depend on the subpackages
            // (as if the subpackages were implemented in separate libraries, referenced by the main project).
            packages.InsertRange(packages.IndexOf(projectPackage), createdPackages);
        }

        [DebuggerDisplay("{ID}")]
        private class SubpackageInfo
        {
            public string ID;
            public string Name;
            public string Folder;
            public List<SubpackageInfo> Dependencies;
        }

        private List<SubpackageInfo> GetSubpackagesInfoSorted(InstalledPackage projectPackage, SubpackagesOptions subpackagesOptions)
        {
            var subpackagesByName = subpackagesOptions.Subpackages
                .Select(p => new
                {
                    Subpackage = p,
                    SubpackageInfo = new SubpackageInfo { ID = p.GetPackageId(projectPackage.Id), Name = p.Name, Folder = p.Folder, Dependencies = [] }
                })
                .ToDictionary(p => p.Subpackage.Name);
            foreach (var p in subpackagesByName.Values)
                if (p.Subpackage.Dependencies != null)
                    foreach (string d in p.Subpackage.Dependencies)
                        if (subpackagesByName.TryGetValue(d, out var dp))
                            p.SubpackageInfo.Dependencies.Add(dp.SubpackageInfo);
                        else
                            _logger.Warning($"Subpackage '{p.Subpackage.Name}' dependency '{d}' not found in the list of subpackages.");

            var subpackagesInfo = subpackagesByName.Values.Select(p => p.SubpackageInfo).ToList();
            var allDependencies = subpackagesInfo.SelectMany(p => p.Dependencies.Select(d => Tuple.Create(d, p))).ToList();
            Graph.TopologicalSort(subpackagesInfo, allDependencies);
            return subpackagesInfo;
        }

        private const string DslScriptsSubfolder = "DslScripts";
        private static readonly string DslScriptsSubfolderPrefix = DslScriptsSubfolder + Path.DirectorySeparatorChar;

        /// <summary>
        /// Extracts all files from a given subfolder into a new (virtual) package, and removes them from the current package.
        /// </summary>
        private InstalledPackage ExtractSubpackage(InstalledPackage projectPackage, SubpackageInfo subpackage)
        {
            string subpackageFolder = Path.GetFullPath(Path.Combine(projectPackage.Folder, subpackage.Folder));
            if (subpackageFolder.Last() != Path.DirectorySeparatorChar)
                subpackageFolder += Path.DirectorySeparatorChar; // Makes sure to avoid selecting files from a subfolder which Name begins with the wanted subfolder name.

            var subpackageDependencies = projectPackage.Dependencies.Concat(subpackage.Dependencies.Select(dependency => new PackageRequest { Id = dependency.ID, VersionsRange = "" })).ToList();
            var subpackageFiles = projectPackage.ContentFiles.Where(f => f.PhysicalPath.StartsWith(subpackageFolder, StringComparison.OrdinalIgnoreCase)).ToList();
            var virtualPackage = new InstalledPackage(subpackage.ID, "", subpackageDependencies, subpackageFolder, subpackageFiles);

            projectPackage.ContentFiles.RemoveAll(subpackageFiles.Contains);

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

        private void AddDependencies(InstalledPackage projectPackage, List<InstalledPackage> createdPackages)
        {
            projectPackage.Dependencies.AddRange(createdPackages.Select(p => new PackageRequest { Id = p.Id, VersionsRange = "" }));
        }

        public void Save(RhetosBuildEnvironment rhetosBuildEnvironment, RhetosProjectAssets rhetosProjectAssets)
        {
            var rhetosProjectContent = new RhetosProjectContent
            {
                RhetosBuildEnvironment = rhetosBuildEnvironment,
                RhetosProjectAssets = rhetosProjectAssets
            };
            string serialized = JsonConvert.SerializeObject(rhetosProjectContent, _serializerSettings);
            string oldSerializedData = File.Exists(ProjectAssetsFilePath) ? File.ReadAllText(ProjectAssetsFilePath, Encoding.UTF8) : "";

            if (!Directory.Exists(Path.GetDirectoryName(ProjectAssetsFilePath)))
                _filesUtility.SafeCreateDirectory(Path.GetDirectoryName(ProjectAssetsFilePath));

            if (oldSerializedData != serialized)
            {
                File.WriteAllText(ProjectAssetsFilePath, serialized, Encoding.UTF8);
                _logger.Info($"{nameof(RhetosProjectAssets)} updated.");
            }
            else
            {
                _logger.Info($"{nameof(RhetosProjectAssets)} is already up-to-date.");
            }
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
    }
}
