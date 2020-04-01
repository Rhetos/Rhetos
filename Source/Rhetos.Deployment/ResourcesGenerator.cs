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
    /// This IGenerator copies all resource files from installed packages to the generated application.
    /// </summary>
    public class ResourcesGenerator : IGenerator
    {
        private readonly InstalledPackages _installedPackages;
        private readonly ILogProvider _logProvider;
        private readonly BuildOptions _buildOptions;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public ResourcesGenerator(InstalledPackages installedPackages, ILogProvider logProvider, BuildOptions buildOptions)
        {
            _installedPackages = installedPackages;
            _logProvider = logProvider;
            _buildOptions = buildOptions;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        public void Generate()
        {
            if (_buildOptions.Legacy__UseResourcesFolder)
                CopyResourcesFromPackages();
        }

        private void CopyResourcesFromPackages()
        {
            var stopwatch = Stopwatch.StartNew();
            var _fileSyncer = new FileSyncer(_logProvider);
            _fileSyncer.AddDestinations(Paths.ResourcesFolder); // Even if there are no packages, the old folder content must be emptied.

            const string ResourcesPathPrefix = @"Resources\";

            var resourceFiles = _installedPackages.Packages
                .SelectMany(package => package.ContentFiles
                    .Where(file => file.InPackagePath.StartsWith(ResourcesPathPrefix))
                    .Where(file => !file.PhysicalPath.StartsWith(Paths.ResourcesFolder)) // Prevent from including the generated output folder as in input.
                    .Select(file => new
                    {
                        Package = package,
                        Source = file.PhysicalPath,
                        Target = Path.Combine(SimplifyPackageName(package.Id), file.InPackagePath.Substring(ResourcesPathPrefix.Length))
                    }))
                .ToList();

            var similarPackages = resourceFiles.Select(file => file.Package).Distinct()
                .GroupBy(package => SimplifyPackageName(package.Id), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (similarPackages != null)
                throw new UserException($"Incompatible package names, resource files would result in the same target folder '{similarPackages.Key}'."
                    + $"\r\nPackage 1: {similarPackages.First().ReportIdVersionRequestSource()}"
                    + $"\r\nPackage 2: {similarPackages.Last().ReportIdVersionRequestSource()}");

            foreach (var file in resourceFiles)
                _fileSyncer.AddFile(file.Source, Paths.ResourcesFolder, file.Target);

            _logger.Info($"Copying {resourceFiles.Count} resource files.");
            _fileSyncer.UpdateDestination();

            _performanceLogger.Write(stopwatch, "ResourcesGenerator.");
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
