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

            // Make sure that the created subpackages are ordered by their dependencies.
            var subpackagesNames = subpackagesOptions.Subpackages.Select(p => p.Name).ToList();
            var dependencies = subpackagesOptions.Subpackages.Where(p => p.Dependencies != null).SelectMany(p => p.Dependencies.Select(d => Tuple.Create(d, p.Name))).ToList();
            Graph.TopologicalSort(subpackagesNames, dependencies);
            var subpackagesSorted = subpackagesOptions.Subpackages.ToList(); // Clone.
            Graph.SortByGivenOrder(subpackagesSorted, subpackagesNames, p => p.Name);

            // Extract files from the main project into the subpackages.
            var projectPackage = packages.Where(p => p.Folder.StartsWith(_projectRootFolder, StringComparison.OrdinalIgnoreCase)).Single();
            var createdPackages = subpackagesSorted.Select(subpackage => projectPackage.ExtractSubpackage(subpackage)).ToList();
            foreach (var createdPackage in createdPackages)
                _logger.Trace(() => $"Project files moved to virtual subpackage '{createdPackage.Id}':{string.Concat(createdPackage.ContentFiles.Select(f => $"{Environment.NewLine}  {f.InPackagePath}"))}");
            projectPackage.AddDependencies(createdPackages);

            // Add the subpackages before the main project, because any other remaining files in the main project are assumed to depend on the subpackages
            // (as if the subpackages were implemented in separate libraries, referenced by the main project).
            packages.InsertRange(packages.IndexOf(projectPackage), createdPackages);
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
