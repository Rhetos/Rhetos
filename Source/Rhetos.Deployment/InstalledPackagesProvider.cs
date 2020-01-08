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
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rhetos.Deployment
{
    public class InstalledPackagesProvider
    {
        private readonly ILogger _logger;

        public InstalledPackagesProvider(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
        }

        private const string PackagesFileName = "InstalledPackages.json";

        public InstalledPackages Load()
        {
            string serialized = File.ReadAllText(PackagesFilePath, Encoding.UTF8);
            var installedPackages = JsonConvert.DeserializeObject<InstalledPackages>(serialized, _serializerSettings);

            // Package folder is saved as relative path, to allow moving the deployed folder.
            foreach (var package in installedPackages.Packages)
                package.SetAbsoluteFolderPath(Paths.RhetosServerRootPath);

            foreach (var package in installedPackages.Packages)
                _logger.Trace(() => package.Report());

            return installedPackages;
        }

        public void Save(InstalledPackages installedPackages)
        {
            // Package folder is saved as relative path, to allow moving the deployed folder.
            foreach (var package in installedPackages.Packages)
                package.SetRelativeFolderPath(Paths.RhetosServerRootPath);

            string serialized = JsonConvert.SerializeObject(installedPackages, _serializerSettings);

            foreach (var package in installedPackages.Packages)
                package.SetAbsoluteFolderPath(Paths.RhetosServerRootPath);

            File.WriteAllText(PackagesFilePath, serialized, Encoding.UTF8);
        }

        private static string PackagesFilePath => Path.Combine(Paths.PluginsFolder, PackagesFileName);

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Formatting = Formatting.Indented
        };
    }
}
