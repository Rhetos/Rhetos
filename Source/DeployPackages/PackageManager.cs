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

using Rhetos;
using Rhetos.Deployment;
using Rhetos.Logging;
using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployPackages
{
    public class PackageManager
    {
        private readonly ILogger _logger;
        private readonly FilesUtility _filesUtility;
        private readonly DeployOptions _deployOptions;
        private readonly InitializationContext _initializationContext;

        public PackageManager(InitializationContext initializationContext)
        {
            _logger = initializationContext.LogProvider.GetLogger("DeployPackages");
            _filesUtility = new FilesUtility(initializationContext.LogProvider);
            _deployOptions = initializationContext.ConfigurationProvider.GetOptions<DeployOptions>();
            _initializationContext = initializationContext;
        }

        public void InitialCleanup()
        {
            ThrowOnObsoleteFolders();
            DeleteObsoleteGeneratedFiles();

            // Backup and delete generated files:
            if (!_deployOptions.DatabaseOnly)
            {
                _logger.Trace("Moving old generated files to cache.");
                new GeneratedFilesCache(_initializationContext.LogProvider).MoveGeneratedFilesToCache();
                _filesUtility.SafeCreateDirectory(Paths.GeneratedFolder);
            }
            else
            {
                var missingFile = Paths.DomAssemblyFiles.FirstOrDefault(f => !File.Exists(f));
                if (missingFile != null)
                    throw new UserException($"'/DatabaseOnly' switch cannot be used if the server have not been deployed successfully before. Run a regular deployment instead. Missing '{missingFile}'.");

                _logger.Info("Skipped deleting old generated files (DeployDatabaseOnly).");
            }
        }

        public void DownloadPackages()
        {
            if (_deployOptions.DatabaseOnly)
            {
                _logger.Info("Skipped download packages (DeployDatabaseOnly).");
                return;
            }

            _logger.Trace("Getting packages.");
            var config = new DeploymentConfiguration(_initializationContext.LogProvider);
            var packageDownloaderOptions = new PackageDownloaderOptions { IgnorePackageDependencies = _deployOptions.IgnoreDependencies };
            var packageDownloader = new PackageDownloader(config, _initializationContext.LogProvider, packageDownloaderOptions);
            var packages = packageDownloader.GetPackages();

            InstalledPackages.Save(packages);
        }

        private void ThrowOnObsoleteFolders()
        {
            var obsoleteFolders = new string[]
            {
                Path.Combine(Paths.RhetosServerRootPath, "DslScripts"),
                Path.Combine(Paths.RhetosServerRootPath, "DataMigration")
            };
            var obsoleteFolder = obsoleteFolders.FirstOrDefault(folder => Directory.Exists(folder));
            if (obsoleteFolder != null)
                throw new UserException("Please backup all Rhetos server folders and delete obsolete folder '" + obsoleteFolder + "'. It is no longer used.");
        }

        private void DeleteObsoleteGeneratedFiles()
        {
            var deleteObsoleteFiles = new string[]
            {
                Path.Combine(Paths.BinFolder, "ServerDom.cs"),
                Path.Combine(Paths.BinFolder, "ServerDom.dll"),
                Path.Combine(Paths.BinFolder, "ServerDom.pdb")
            };

            foreach (var path in deleteObsoleteFiles)
                if (File.Exists(path))
                {
                    _logger.Info($"Deleting obsolete file '{path}'.");
                    _filesUtility.SafeDeleteFile(path);
                }
        }
    }
}
