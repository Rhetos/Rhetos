﻿/*
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
using Rhetos.Logging;
using Rhetos.Utilities;
using System.IO;

namespace Rhetos.Deployment
{
    public class InstalledPackagesProvider
    {
        private readonly ILogger _logger;
        private readonly string _packagesFilePath;

        public InstalledPackagesProvider(ILogProvider logProvider, IAssetsOptions assetsOptions)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _packagesFilePath = Path.Combine(assetsOptions.AssetsFolder, PackagesFileName);
        }

        private const string PackagesFileName = "InstalledPackages.json";

        public InstalledPackages Load()
        {
            var installedPackages = JsonUtility.DeserializeFromFile<InstalledPackages>(_packagesFilePath, _serializerSettings);

            foreach (var package in installedPackages.Packages)
                _logger.Trace(() => package.Report());

            // Removing the folder path because it is a build feature and any plugin that is trying to use it should get an exception.
            // Package content files are not available at runtime, they are considered as a part of local cache on build machine.
            foreach (var package in installedPackages.Packages)
                package.RemoveBuildTimePaths();

            return installedPackages;
        }

        internal void Save(InstalledPackages installedPackages)
        {
            JsonUtility.SerializeToFile(installedPackages, _packagesFilePath, _serializerSettings);
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Formatting = Formatting.Indented
        };
    }
}
