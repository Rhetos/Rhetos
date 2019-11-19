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

using NuGet;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace Rhetos.Deployment
{
    /// <summary>
    /// Downloads and unpacks Rhetos packages, if not already downloaded or unpacked.
    /// Downloads dependent packages, where declare in the package's metadata file.
    /// </summary>
    public class PackageDownloader
    {
        private readonly DeploymentConfiguration _deploymentConfiguration;
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly ILogProvider _logProvider;
        private readonly Rhetos.Logging.ILogger _logger;
        private readonly Rhetos.Logging.ILogger _packagesLogger;
        private readonly Rhetos.Logging.ILogger _performanceLogger;
        private readonly Rhetos.Logging.ILogger _deployPackagesLogger;
        private readonly PackageDownloaderOptions _options;
        private readonly FilesUtility _filesUtility;

        public PackageDownloader(
            DeploymentConfiguration deploymentConfiguration,
            RhetosAppEnvironment rhetosAppEnvironment,
            ILogProvider logProvider,
            PackageDownloaderOptions options)
        {
            _deploymentConfiguration = deploymentConfiguration;
            _rhetosAppEnvironment = rhetosAppEnvironment;
            _logProvider = logProvider;
            _logger = logProvider.GetLogger(GetType().Name);
            _packagesLogger = logProvider.GetLogger("Packages");
            _performanceLogger = logProvider.GetLogger("Performance");
            _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
            _options = options;
            _filesUtility = new FilesUtility(logProvider);
        }

        /// <summary>
        /// Downloads the packages from the provided sources, if not already downloaded.
        /// Unpacks the packages, if not already unpacked.
        /// </summary>
        public List<InstalledPackage> GetPackages()
        {
            var sw = Stopwatch.StartNew();
            var installedPackages = new List<InstalledPackage>();
            installedPackages.Add(new InstalledPackage("Rhetos", SystemUtility.GetRhetosVersion(), new List<PackageRequest>(), _rhetosAppEnvironment.RootPath,
                new PackageRequest { Id = "Rhetos", VersionsRange = "", Source = "", RequestedBy = "Rhetos framework" }, ".", new List<ContentFile> { }));

            var binFileSyncer = new FileSyncer(_logProvider);
            binFileSyncer.AddDestinations(_rhetosAppEnvironment.PluginsFolder, _rhetosAppEnvironment.ResourcesFolder); // Even if there are no packages, those folders must be created and emptied.

            _filesUtility.SafeCreateDirectory(_rhetosAppEnvironment.PackagesCacheFolder);
            var packageRequests = _deploymentConfiguration.PackageRequests;
            while (packageRequests.Any())
            {
                var newDependencies = new List<PackageRequest>();
                foreach (var request in packageRequests)
                {
                    if (!CheckAlreadyDownloaded(request, installedPackages))
                    {
                        var installedPackage = GetPackage(request, binFileSyncer);
                        ValidatePackage(installedPackage, request, installedPackages);
                        installedPackages.Add(installedPackage);
                        newDependencies.AddRange(installedPackage.Dependencies);
                    }
                }
                packageRequests = newDependencies;
            }

            DeleteObsoletePackages(installedPackages);
            SortByDependencies(installedPackages);

            binFileSyncer.UpdateDestination();

            foreach (var package in installedPackages)
                _packagesLogger.Trace(() => package.Report());

            _performanceLogger.Write(sw, "PackageDownloader.GetPackages.");

            return installedPackages;
        }

        private static void SortByDependencies(List<InstalledPackage> installedPackages)
        {
            installedPackages.Sort((a, b) => string.Compare(a.Id, b.Id, true));

            var packagesById = installedPackages.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
            var dependencies = installedPackages.SelectMany(p => p.Dependencies.Select(d => Tuple.Create(packagesById[d.Id], p))).ToList();
            Graph.TopologicalSort(installedPackages, dependencies);
        }

        private bool CheckAlreadyDownloaded(PackageRequest request, List<InstalledPackage> installedPackages)
        {
            var existing = installedPackages.FirstOrDefault(op => string.Equals(op.Id, request.Id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
                return false;

            var requestVersionsRange = VersionUtility.ParseVersionSpec(request.VersionsRange);
            var existingVersion = SemanticVersion.Parse(existing.Version);

            if (!requestVersionsRange.Satisfies(existingVersion))
                DependencyError(string.Format(
                    "Incompatible package version '{0}, version {1}, requested by {2}' conflicts with previously downloaded package '{3}, version {4}, requested by {5} ({6})'.",
                    request.Id, request.VersionsRange ?? "not specified", request.RequestedBy,
                    existing.Id, existing.Version, existing.Request.RequestedBy, existing.Request.VersionsRange));

            return true;
        }

        private void DependencyError(string errorMessage)
        {
            if (_options.IgnorePackageDependencies)
                _logger.Error(errorMessage);
            else
                throw new UserException(errorMessage);
        }

        private InstalledPackage GetPackage(PackageRequest request, FileSyncer binFileSyncer)
        {
            var installedPackage = TryGetPackageFromNuGetCache(request, binFileSyncer);
            if (installedPackage != null)
                return installedPackage;

            var packageSources = SelectPackageSources(request);
            foreach (var source in packageSources)
            {
                installedPackage = TryGetPackage(source, request, binFileSyncer);
                if (installedPackage != null)
                    return installedPackage;
            }

            throw new UserException("Cannot download package " + request.ReportIdVersionsRange()
                + ". Looked at " + packageSources.Count() + " sources:"
                + string.Concat(packageSources.Select(source => "\r\n" + source.ProcessedLocation)));
        }

        private IEnumerable<PackageSource> SelectPackageSources(PackageRequest request)
        {
            if (request.Source != null)
                return new List<PackageSource> { new PackageSource(_rhetosAppEnvironment.RootPath, request.Source) };
            else
                return _deploymentConfiguration.PackageSources;
        }

        private InstalledPackage TryGetPackage(PackageSource source, PackageRequest request, FileSyncer binFileSyncer)
        {
            return TryGetPackageFromUnpackedSourceFolder(source, request, binFileSyncer)
                ?? TryGetPackageFromLegacyZipPackage(source, request, binFileSyncer)
                ?? TryGetPackageFromNuGet(source, request, binFileSyncer);
        }

        private void ValidatePackage(InstalledPackage installedPackage, PackageRequest request, List<InstalledPackage> installedPackages)
        {
            if (request.Id != null
                && !string.Equals(installedPackage.Id, request.Id, StringComparison.OrdinalIgnoreCase))
                throw new UserException(string.Format(
                    "Package ID '{0}' at location '{1}' does not match package ID '{2}' requested from {3}.",
                    installedPackage.Id, installedPackage.Source, request.Id, request.RequestedBy));

            if (request.VersionsRange != null
                && !VersionUtility.ParseVersionSpec(request.VersionsRange).Satisfies(SemanticVersion.Parse(installedPackage.Version)))
                DependencyError(string.Format(
                    "Incompatible package version '{0}, version {1}'. Version {2} is requested from {3}'.",
                    installedPackage.Id, installedPackage.Version,
                    request.VersionsRange, request.RequestedBy));

            var similarOldPackage = installedPackages.FirstOrDefault(oldPackage => !string.Equals(oldPackage.Id, installedPackage.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(SimplifyPackageName(oldPackage.Id), SimplifyPackageName(installedPackage.Id), StringComparison.OrdinalIgnoreCase));
            if (similarOldPackage != null)
                throw new UserException(string.Format(
                    "Incompatible package names '{0}' (requested from {1}) and '{2}' (requested from {3}).",
                    installedPackage.Id, installedPackage.Request.RequestedBy,
                    similarOldPackage.Id, similarOldPackage.Request.RequestedBy));
        }

        private string SimplifyPackageName(string packageId)
        {
            const string removablePrefix = "Rhetos.";
            if (packageId.StartsWith(removablePrefix))
                packageId = packageId.Substring(removablePrefix.Length);
            return packageId;
        }

        //================================================================
        #region Getting the package from unpacked source folder

        private InstalledPackage TryGetPackageFromUnpackedSourceFolder(PackageSource source, PackageRequest request, FileSyncer binFileSyncer)
        {
            if (request.Source == null) // Unpacked source folder must be explicitly set in the package request.
                return null;
            if (source.Path == null || !Directory.Exists(source.Path))
                return null;

            var packageFilePatterns = new[] { "*.nupkg", GetExpectedZipPackage(request)?.FileName };
            var existingPackageFiles = packageFilePatterns.Where(pattern => pattern != null)
                .SelectMany(ext => Directory.GetFiles(source.Path, ext));

            var existingMetadataFiles = Directory.GetFiles(source.Path, "*.nuspec");
            if (existingMetadataFiles.Length > 1)
            {
                // If any nuspec file exactly matches the packages name, use that one.
                var standardFileName = existingMetadataFiles.Where(f => string.Equals(Path.GetFileName(f), request.Id + ".nuspec", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (standardFileName.Length == 1)
                    existingMetadataFiles = standardFileName;
            }

            var packageSourceSubfolders = new[] { "DslScripts", "DataMigration", "Plugins", "Resources" };
            var existingSourceSubfolders = packageSourceSubfolders.Where(subfolder => Directory.Exists(Path.Combine(source.Path, subfolder)));

            if (existingPackageFiles.Any())
            {
                string ambiguousAlternative = null;
                if (existingMetadataFiles.Any())
                    ambiguousAlternative = $".nuspec file '{Path.GetFileName(existingMetadataFiles.First())}'";
                else if (existingSourceSubfolders.Any())
                    ambiguousAlternative = $"source folder '{existingSourceSubfolders.First()}'";

                if (ambiguousAlternative != null)
                    throw new FrameworkException($"Ambiguous source for package {request.Id}. Source folder '{source.Path}' contains both" +
                        $" package file '{Path.GetFileName(existingPackageFiles.First())}' and {ambiguousAlternative}.");

                _logger.Trace(() => $"Package {request.Id} source folder is not considered as unpacked source because" +
                    $" it contains a package file '{Path.GetFileName(existingPackageFiles.First())}'.");
                return null;
            }
            else if (existingMetadataFiles.Length > 1)
            {
                _logger.Info(() => "Package " + request.Id + " source folder '" + source.ProvidedLocation + "' contains multiple .nuspec metadata files.");
                return null;
            }
            else if (existingMetadataFiles.Length == 1)
            {
                _logger.Trace(() => "Reading package " + request.Id + " from unpacked source folder with metadata " + Path.GetFileName(existingMetadataFiles.Single()) + ".");
                _deployPackagesLogger.Trace(() => "Reading " + request.Id + " from source.");
                return UseFilesFromUnpackedSourceWithMetadata(existingMetadataFiles.Single(), request, binFileSyncer);
            }
            else if (existingSourceSubfolders.Any())
            {
                _logger.Trace(() => "Reading package " + request.Id + " from unpacked source folder without metadata file.");
                _deployPackagesLogger.Trace(() => "Reading " + request.Id + " from source without metadata.");
                return UseFilesFromUnpackedSourceWithoutMetadata(source.Path, request, binFileSyncer);
            }
            else
                return null;
        }

        private InstalledPackage UseFilesFromUnpackedSourceWithMetadata(string metadataFile, PackageRequest request, FileSyncer binFileSyncer)
        {
            var properties = new SimplePropertyProvider
            {
                { "Configuration", "Debug" },
            };
            var packageBuilder = new PackageBuilder(metadataFile, properties, includeEmptyDirectories: false);

            string sourceFolder = Path.GetDirectoryName(metadataFile);

            // Copy binary files and resources:

            foreach (var file in FilterCompatibleLibFiles(packageBuilder.Files).Cast<PhysicalPackageFile>())
                binFileSyncer.AddFile(file.SourcePath, _rhetosAppEnvironment.PluginsFolder, Path.Combine(_rhetosAppEnvironment.PluginsFolder, file.EffectivePath));

            foreach (var file in packageBuilder.Files.Cast<PhysicalPackageFile>())
                if (file.Path.StartsWith(@"Plugins\")) // Obsolete bin folder; lib should be used instead.
                    binFileSyncer.AddFile(file.SourcePath, _rhetosAppEnvironment.PluginsFolder);
                else if (file.Path.StartsWith(@"Resources\"))
                    binFileSyncer.AddFile(file.SourcePath, _rhetosAppEnvironment.ResourcesFolder, Path.Combine(SimplifyPackageName(packageBuilder.Id), file.Path.Substring(@"Resources\".Length)));

            var contentFiles = packageBuilder.Files.Cast<PhysicalPackageFile>()
                .Select(file => new ContentFile { PhysicalPath = file.SourcePath, InPackagePath = file.Path })
                .ToList();

            return new InstalledPackage(packageBuilder.Id, packageBuilder.Version.ToString(), GetNuGetPackageDependencies(packageBuilder),
                sourceFolder, request, sourceFolder, contentFiles);
        }

        private IEnumerable<IPackageFile> FilterCompatibleLibFiles(IEnumerable<IPackageFile> files)
        {
            IEnumerable<IPackageFile> compatibleLibFiles;
            var allLibFiles = files.Where(file => file.Path.StartsWith(@"lib\"));
            if (VersionUtility.TryGetCompatibleItems(SystemUtility.GetTargetFramework(), allLibFiles, out compatibleLibFiles))
                return compatibleLibFiles;
            else
                return Enumerable.Empty<IPackageFile>();
        }

        private List<PackageRequest> GetNuGetPackageDependencies(IPackageMetadata package)
        {
            var dependencies = package.GetCompatiblePackageDependencies(SystemUtility.GetTargetFramework())
                .Select(dependency => new PackageRequest
                {
                    Id = dependency.Id,
                    VersionsRange = dependency.VersionSpec?.ToString(),
                    RequestedBy = "package " + package.Id
                }).ToList();

            if (!dependencies.Any(p => string.Equals(p.Id, "Rhetos", StringComparison.OrdinalIgnoreCase)))
            {
                // FrameworkAssembly is an obsolete way of marking package dependency on a specific Rhetos version:
                var rhetosFrameworkAssemblyRegex = new Regex(@"^Rhetos\s*,\s*Version\s*=\s*(\S+)$");
                var parseFrameworkAssembly = package.FrameworkAssemblies
                    .Select(assembly => rhetosFrameworkAssemblyRegex.Match(assembly.AssemblyName.Trim()))
                    .SingleOrDefault(match => match.Success);
                if (parseFrameworkAssembly != null)
                    dependencies.Add(new PackageRequest
                    {
                        Id = "Rhetos",
                        VersionsRange = parseFrameworkAssembly.Groups[1].Value,
                        RequestedBy = "package " + package.Id
                    });
            }

            return dependencies;
        }

        private class SimplePropertyProvider : Dictionary<string, string>, IPropertyProvider
        {
            public dynamic GetPropertyValue(string propertyName)
            {
                string value;
                if (TryGetValue(propertyName, out value))
                    return value;
                return null;
            }
        }

        private InstalledPackage UseFilesFromUnpackedSourceWithoutMetadata(string sourceFolder, PackageRequest request, FileSyncer binFileSyncer)
        {
            string sourcePluginsFolder = Path.Combine(sourceFolder, "Plugins");
            if (Directory.Exists(sourcePluginsFolder)
                && Directory.EnumerateFiles(sourcePluginsFolder, "*.dll", SearchOption.AllDirectories).FirstOrDefault() != null
                && Directory.EnumerateFiles(sourcePluginsFolder, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault() == null)
                _logger.Error(() => "Package " + request.Id + " source folder contains Plugins that will not be installed to the Rhetos server. Add '" + request.Id + ".nuspec' file to define the which plugin files should be installed.");

            binFileSyncer.AddFolderContent(Path.Combine(sourceFolder, "Plugins"), _rhetosAppEnvironment.PluginsFolder, recursive: false);
            binFileSyncer.AddFolderContent(Path.Combine(sourceFolder, "Resources"), _rhetosAppEnvironment.ResourcesFolder, SimplifyPackageName(request.Id), recursive: true);

            string defaultVersion = CreateVersionInRangeOrZero(request);
            return new InstalledPackage(request.Id, defaultVersion, new List<PackageRequest> { }, sourceFolder, request, sourceFolder);
        }

        #endregion
        //================================================================
        #region Getting the package from legacy zip file

        private InstalledPackage TryGetPackageFromLegacyZipPackage(PackageSource source, PackageRequest request, FileSyncer binFileSyncer)
        {
            if (source.Path == null)
                return null;

            var zipPackage = GetExpectedZipPackage(request);
            if (zipPackage == null)
                return null;

            string zipPackagePath = Path.Combine(source.Path, zipPackage.FileName);

            if (!File.Exists(zipPackagePath))
            {
                _logger.Trace(() => $"There is no legacy zip package at {zipPackagePath}.");
                return null;
            }

            _logger.Trace(() => $"Reading package {request.Id} from legacy file {zipPackagePath}.");
            _deployPackagesLogger.Trace(() => $"Reading {request.Id} from legacy zip file.");

            string targetFolder = GetTargetFolder(request.Id, zipPackage.Version);
            _filesUtility.EmptyDirectory(targetFolder);
            ZipFile.ExtractToDirectory(zipPackagePath, targetFolder);

            binFileSyncer.AddFolderContent(Path.Combine(targetFolder, "Plugins"), _rhetosAppEnvironment.PluginsFolder, recursive: false);
            binFileSyncer.AddFolderContent(Path.Combine(targetFolder, "Resources"), _rhetosAppEnvironment.ResourcesFolder, SimplifyPackageName(request.Id), recursive: true);

            return new InstalledPackage(request.Id, zipPackage.Version, new List<PackageRequest> { }, targetFolder, request, source.ProcessedLocation);
        }

        public class ExpectedZipPackage
        {
            public string FileName { get; set; }
            public string Version { get; set; }
        }

        /// <summary>
        /// Returns null if the zip package is not supported for this request.
        /// </summary>
        private ExpectedZipPackage GetExpectedZipPackage(PackageRequest request)
        {
            if (request.VersionsRange == null)
                return null;

            var requestVersionsRange = VersionUtility.ParseVersionSpec(request.VersionsRange);
            if (!SpecifiedMinVersion(requestVersionsRange))
                return null;

            string version = requestVersionsRange.MinVersion.ToString();

            return new ExpectedZipPackage { FileName = $"{request.Id}_{version?.Replace('.', '_')}.zip", Version = version };
        }

        private static bool SpecifiedMinVersion(IVersionSpec requestVersionsRange)
        {
            return requestVersionsRange.MinVersion != null && !requestVersionsRange.MinVersion.Equals(new SemanticVersion("0.0"));
        }

        #endregion
        //================================================================
        #region Getting the package from NuGet

        private InstalledPackage TryGetPackageFromNuGetCache(PackageRequest request, FileSyncer binFileSyncer)
        {
            var sw = Stopwatch.StartNew();

            // Use cache only if not deploying from source and an exact version is specified:

            if (request.Source != null)
                return null;

            var requestVersionsRange = !string.IsNullOrEmpty(request.VersionsRange)
                ? VersionUtility.ParseVersionSpec(request.VersionsRange)
                : new VersionSpec();

            // Default NuGet behavior is to download the smallest version in the given range, so only MinVersion is checked here:
            if (!SpecifiedMinVersion(requestVersionsRange))
            {
                _logger.Trace(() => $"Not looking for {request.ReportIdVersionsRange()} in packages cache because the request does not specify an exact version.");
                return null;
            }

            // Find the NuGet package:

            var nugetRepository = new LocalPackageRepository(_rhetosAppEnvironment.PackagesCacheFolder, enableCaching: false);
            IPackage package = nugetRepository.FindPackage(request.Id, requestVersionsRange, allowPrereleaseVersions: true, allowUnlisted: true);

            _performanceLogger.Write(sw, () => $"PackageDownloader: {(package == null ? "Did not find" : "Found")} the NuGet package {request.ReportIdVersionsRange()} in cache.");
            if (package == null)
                return null;

            // Copy binary files and resources:

            string packageSubfolder = nugetRepository.PathResolver.GetPackageDirectory(request.Id, package.Version);
            _logger.Trace(() => $"Reading package {request.Id} from cache '{packageSubfolder}'.");
            _deployPackagesLogger.Trace(() => $"Reading {request.Id} from cache.");
            string targetFolder = Path.Combine(_rhetosAppEnvironment.PackagesCacheFolder, packageSubfolder);

            foreach (var file in FilterCompatibleLibFiles(package.GetFiles()))
                binFileSyncer.AddFile(Path.Combine(targetFolder, file.Path), _rhetosAppEnvironment.PluginsFolder, Path.Combine(_rhetosAppEnvironment.PluginsFolder, file.EffectivePath));

            binFileSyncer.AddFolderContent(Path.Combine(targetFolder, "Plugins"), _rhetosAppEnvironment.PluginsFolder, recursive: false); // Obsolete bin folder; lib should be used instead.
            binFileSyncer.AddFolderContent(Path.Combine(targetFolder, "Resources"), _rhetosAppEnvironment.ResourcesFolder, SimplifyPackageName(package.Id), recursive: true);

            return new InstalledPackage(package.Id, package.Version.ToString(), GetNuGetPackageDependencies(package), targetFolder, request, _rhetosAppEnvironment.PackagesCacheFolder);
        }

        private InstalledPackage TryGetPackageFromNuGet(PackageSource source, PackageRequest request, FileSyncer binFileSyncer)
        {
            var sw = Stopwatch.StartNew();

            // Find the NuGet package:

            var nugetRepository = (source.Path != null && IsLocalPath(source.Path))
                ? new LocalPackageRepository(source.Path, enableCaching: false) // When developer rebuilds a package, the package version does not need to be increased every time.
                : PackageRepositoryFactory.Default.CreateRepository(source.ProcessedLocation);
            var requestVersionsRange = !string.IsNullOrEmpty(request.VersionsRange)
                ? VersionUtility.ParseVersionSpec(request.VersionsRange)
                : new VersionSpec();
            IEnumerable<IPackage> packages = nugetRepository.FindPackages(request.Id, requestVersionsRange, allowPrereleaseVersions: true, allowUnlisted: true).ToList();

            if (SpecifiedMinVersion(requestVersionsRange))
                packages = packages.OrderBy(p => p.Version); // Find the lowest compatible version if the version is specified (default NuGet behavior).
            else
                packages = packages.OrderByDescending(p => p.Version);

            var package = packages.FirstOrDefault();

            _performanceLogger.Write(sw, () => $"PackageDownloader: {(package == null ? "Did not find" : "Found")} the NuGet package {request.ReportIdVersionsRange()} at {source.ProcessedLocation}.");
            if (package == null)
                return null;

            // Download the NuGet package:

            _logger.Trace("Downloading NuGet package " + package.Id + " " + package.Version + " from " + source.ProcessedLocation + ".");
            var packageManager = new PackageManager(nugetRepository, _rhetosAppEnvironment.PackagesCacheFolder)
            {
                Logger = new LoggerForNuget(_logProvider)
            };
            packageManager.LocalRepository.PackageSaveMode = PackageSaveModes.Nuspec;

            packageManager.InstallPackage(package, ignoreDependencies: true, allowPrereleaseVersions: true);
            _performanceLogger.Write(sw, () => "PackageDownloader: Installed NuGet package " + request.Id + ".");

            string targetFolder = packageManager.PathResolver.GetInstallPath(package);

            // Copy binary files and resources:

            foreach (var file in FilterCompatibleLibFiles(package.GetFiles()))
                binFileSyncer.AddFile(Path.Combine(targetFolder, file.Path), _rhetosAppEnvironment.PluginsFolder, Path.Combine(_rhetosAppEnvironment.PluginsFolder, file.EffectivePath));

            binFileSyncer.AddFolderContent(Path.Combine(targetFolder, "Plugins"), _rhetosAppEnvironment.PluginsFolder, recursive: false); // Obsolete bin folder; lib should be used instead.
            binFileSyncer.AddFolderContent(Path.Combine(targetFolder, "Resources"), _rhetosAppEnvironment.ResourcesFolder, SimplifyPackageName(package.Id), recursive: true);

            return new InstalledPackage(package.Id, package.Version.ToString(), GetNuGetPackageDependencies(package), targetFolder, request, source.ProcessedLocation);
        }

        private static bool IsLocalPath(string path)
        {
            var driveRegex = new Regex(@"^[a-z]\:\\$", RegexOptions.IgnoreCase);
            return driveRegex.IsMatch(Path.GetPathRoot(path));
        }

        #endregion
        //================================================================

        private static string CreateVersionInRangeOrZero(PackageRequest request)
        {
            if (request.VersionsRange != null)
            {
                var versionSpec = VersionUtility.ParseVersionSpec(request.VersionsRange);
                if (versionSpec.MinVersion != null)
                    if (versionSpec.IsMinInclusive)
                        return versionSpec.MinVersion.ToString();
                    else
                    {
                        var v = versionSpec.MinVersion.Version;
                        return new Version(v.Major, v.Minor, v.Build + 1).ToString();
                    }
            }
            return "0.0";
        }

        private static HashSet<char> _invalidPackageChars = new HashSet<char>(Path.GetInvalidFileNameChars().Concat(new[] { ' ' }));

        private string GetTargetFolder(string packageId, string packageVersion)
        {
            char invalidChar = packageId.FirstOrDefault(c => _invalidPackageChars.Contains(c));
            if (invalidChar != default(char))
                throw new UserException("Invalid character '" + invalidChar + "' in package id '" + packageId + "'.");

            invalidChar = packageVersion.FirstOrDefault(c => _invalidPackageChars.Contains(c));
            if (invalidChar != default(char))
                throw new UserException("Invalid character '" + invalidChar + "' in package version. Package " + packageId + ", version '" + packageVersion + "'.");

            return Path.Combine(_rhetosAppEnvironment.PackagesCacheFolder, packageId + "." + packageVersion);
        }

        private void DeleteObsoletePackages(List<InstalledPackage> installedPackages)
        {
            var sw = Stopwatch.StartNew();

            var obsoletePackages = Directory.GetDirectories(_rhetosAppEnvironment.PackagesCacheFolder)
                .Except(installedPackages.Select(p => p.Folder), StringComparer.OrdinalIgnoreCase);

            foreach (var folder in obsoletePackages)
                _filesUtility.SafeDeleteDirectory(folder);

            _performanceLogger.Write(sw, "PackageDownloader.DeleteObsoletePackages");
        }
    }
}
