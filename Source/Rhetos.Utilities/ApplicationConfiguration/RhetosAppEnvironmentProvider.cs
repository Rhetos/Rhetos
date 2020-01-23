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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public static class RhetosAppEnvironmentProvider
    {
        private const string RhetosAppEnvironmentFileName = "RhetosAppEnvironment.json";

        public static void Save(RhetosAppEnvironment rhetosAppEnvironment)
        {
            string saveFolder = rhetosAppEnvironment.RootFolder;
            var relativePaths = new Dictionary<string, string>
            {
                // No need to save RootFolder, the file is in that folder.
                { nameof(RhetosAppEnvironment.BinFolder), FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.BinFolder) },
                { nameof(RhetosAppEnvironment.AssetsFolder), FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.AssetsFolder) },
                { nameof(RhetosAppEnvironment.LegacyPluginsFolder), FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.LegacyPluginsFolder) },
                { nameof(RhetosAppEnvironment.LegacyAssetsFolder), FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.LegacyAssetsFolder) },
            };

            string serialized = JsonConvert.SerializeObject(relativePaths, Formatting.Indented);
            string filePath = Path.Combine(saveFolder, RhetosAppEnvironmentFileName);
            File.WriteAllText(filePath, serialized, Encoding.UTF8);
        }

        public static RhetosAppEnvironment Load(string rhetosAppRootPath)
        {
            rhetosAppRootPath = Path.GetFullPath(rhetosAppRootPath); // For better error reporting.

            var filePath = Path.Combine(rhetosAppRootPath, RhetosAppEnvironmentFileName);
            if (!File.Exists(filePath))
                throw new FrameworkException($"Missing file '{RhetosAppEnvironmentFileName}' in folder '{rhetosAppRootPath}' or subfolders." +
                    $" Please verify that the specified folder contains a valid Rhetos application, and that the build have passed successfully.");

            var serialized = File.ReadAllText(filePath, Encoding.UTF8);
            var relativePaths = JsonConvert.DeserializeObject<Dictionary<string, string>>(serialized);

            string saveFolder = Path.GetDirectoryName(filePath);
            return new RhetosAppEnvironment
            {
                RootFolder = rhetosAppRootPath,
                BinFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, relativePaths[nameof(RhetosAppEnvironment.BinFolder)]),
                AssetsFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, relativePaths[nameof(RhetosAppEnvironment.AssetsFolder)]),
                LegacyPluginsFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, relativePaths[nameof(RhetosAppEnvironment.LegacyPluginsFolder)]),
                LegacyAssetsFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, relativePaths[nameof(RhetosAppEnvironment.LegacyAssetsFolder)]),
            };
        }

        public static bool IsRhetosApplicationRootFolder(string rhetosAppRootPath)
        {
            var filePath = Path.Combine(rhetosAppRootPath, RhetosAppEnvironmentFileName);
            return File.Exists(filePath);
        }
    }
}
