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

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rhetos.Deployment
{
    /// <summary>
    /// "Resources" is a legacy assets folder.
    /// This IGenerator copies all legacy assets files from installed packages (Resources subfolder) to the generated application's assets folder.
    /// It also included files from the current project's folder 'Resources\Rhetos', to avoid conflicts with other usages of the Resources folder.
    /// </summary>
    public class ResourcesGenerator : IGenerator
    {
        private readonly InstalledPackages _installedPackages;
        private readonly ILogProvider _logProvider;
        private readonly BuildOptions _buildOptions;
        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        private static readonly string ResourcesPathPrefix = "Resources" + Path.DirectorySeparatorChar;
        private static readonly string HostApplicationResourcesPathPrefix = ResourcesPathPrefix + "Rhetos" + Path.DirectorySeparatorChar;
        private readonly Lazy<string> _currentProjectResourcesFolder;

        public ResourcesGenerator(
            InstalledPackages installedPackages,
            ILogProvider logProvider,
            BuildOptions buildOptions,
            RhetosBuildEnvironment rhetosBuildEnvironment)
        {
            _installedPackages = installedPackages;
            _logProvider = logProvider;
            _buildOptions = buildOptions;
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);

            // Using Lazy to avoid errors with any missing configuration, if there is no need to build the Resources.
            _currentProjectResourcesFolder = new Lazy<string>(() => Path.Combine(_rhetosBuildEnvironment.ProjectFolder, ResourcesPathPrefix), false);
        }

        public void Generate()
        {
            if (_buildOptions.BuildResourcesFolder)
                CopyResourcesFromPackages();
        }

        private void CopyResourcesFromPackages()
        {
            var stopwatch = Stopwatch.StartNew();
            var _fileSyncer = new FileSyncer(_logProvider);
            _fileSyncer.AddDestinations(_rhetosBuildEnvironment.GeneratedAssetsFolder); // Even if there are no packages, the old folder content must be emptied.

            var resourceFiles = _installedPackages.Packages
                .SelectMany(package => package.ContentFiles
                    .Select(file => (file.InPackagePath, file.PhysicalPath, Subpath: GetResourcesFileSubpath(file)))
                    .Where(file => file.Subpath != null)
                    .Select(file => new
                    {
                        Package = package,
                        Source = file.PhysicalPath,
                        Target = Path.Combine(SimplifyPackageName(package.Id), file.Subpath)
                    }))
                .ToList();

            var similarPackages = resourceFiles.Select(file => file.Package).Distinct()
                .GroupBy(package => SimplifyPackageName(package.Id), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (similarPackages != null)
                throw new UserException($"Incompatible package names, resource files would result in the same target folder '{similarPackages.Key}'."
                    + $"\r\nPackage 1: {similarPackages.First().Report()}"
                    + $"\r\nPackage 2: {similarPackages.Last().Report()}");

            foreach (var file in resourceFiles)
                _fileSyncer.AddFile(file.Source, _rhetosBuildEnvironment.GeneratedAssetsFolder, file.Target);

            _logger.Info($"Copying {resourceFiles.Count} resource files.");
            _fileSyncer.UpdateDestination(false, false);

            _performanceLogger.Write(stopwatch, "Resources generated.");
        }

        /// <summary>
        /// For Resources files returns the relative path within the Resources folder, otherwise returns <see langword="null"/>.
        /// </summary>
        private string GetResourcesFileSubpath(ContentFile file)
        {
            if (!file.PhysicalPath.StartsWith(_currentProjectResourcesFolder.Value))
            {
                // The resource files in referenced project or package are located in the Resources subfolder.
                // This also includes subpackages placed withing the current project with PhysicalPath "currentProject\subpackage\Resources\file".
                if (file.InPackagePath.StartsWith(ResourcesPathPrefix))
                    return file.InPackagePath.Substring(ResourcesPathPrefix.Length);
                else
                    return null;
            }
            else
            {
                // We also want to add the possibility that a Rhetos resource file can be added directly in the current project by the application developer,
                // but files in current project's "Resources" folder must be placed in "Resources\Rhetos" instead, to avoid conflicts with other usages of the Resources folder.
                if (file.InPackagePath.StartsWith(HostApplicationResourcesPathPrefix))
                    return file.InPackagePath.Substring(HostApplicationResourcesPathPrefix.Length);
                else
                    return null;
            }
        }

        private string SimplifyPackageName(string packageId)
        {
            const string removablePrefix = "Rhetos.";
            if (packageId.StartsWith(removablePrefix))
                packageId = packageId.Substring(removablePrefix.Length);
            return packageId;
        }

        public IEnumerable<string> Dependencies => Array.Empty<string>();
    }
}
