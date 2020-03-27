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
using System.IO;
using System.Text;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public static class RhetosAppEnvironmentProvider
    {
        private const string RhetosAppEnvironmentFileName = "RhetosAppEnvironment.json";

        /// <summary>
        /// Saving build-time configuration to be used later at run-time.
        /// </summary>
        public static void SaveForRuntime(RhetosAppEnvironment rhetosAppEnvironment, string assetsFolderDestination)
        {
            string saveFolder = rhetosAppEnvironment.RootFolder;
            var rhetosAppEnvironmentToSave = new RhetosAppEnvironment
            {
                RootFolder = FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.RootFolder),
                BinFolder = FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.BinFolder),
                AssetsFolder = FilesUtility.AbsoluteToRelativePath(saveFolder, assetsFolderDestination),
                LegacyPluginsFolder = FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.LegacyPluginsFolder),
                LegacyAssetsFolder = FilesUtility.AbsoluteToRelativePath(saveFolder, rhetosAppEnvironment.LegacyAssetsFolder),
                AssemblyName = rhetosAppEnvironment.AssemblyName
            };

            string serialized = JsonConvert.SerializeObject(rhetosAppEnvironmentToSave, Formatting.Indented);
            string filePath = Path.Combine(saveFolder, RhetosAppEnvironmentFileName);
            File.WriteAllText(filePath, serialized, Encoding.UTF8);
        }

        /// <summary>
        /// Loading application environment configuration at run-time.
        /// </summary>
        public static RhetosAppEnvironment Load(string rhetosAppRootPath)
        {
            rhetosAppRootPath = Path.GetFullPath(rhetosAppRootPath); // For better error reporting.

            var filePath = Path.Combine(rhetosAppRootPath, RhetosAppEnvironmentFileName);
            if (!File.Exists(filePath))
                throw new FrameworkException($"Please verify that the Rhetos build have passed successfully," +
                    $" and that the specified folder contains a valid Rhetos application." +
                    $" Missing file '{RhetosAppEnvironmentFileName}' in folder '{rhetosAppRootPath}'.");

            string saveFolder = Path.GetDirectoryName(filePath);
            var serialized = File.ReadAllText(filePath, Encoding.UTF8);
            var rhetosAppEnvironment = JsonConvert.DeserializeObject<RhetosAppEnvironment>(serialized);

            rhetosAppEnvironment.RootFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, rhetosAppEnvironment.RootFolder);
            rhetosAppEnvironment.BinFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, rhetosAppEnvironment.BinFolder);
            rhetosAppEnvironment.AssetsFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, rhetosAppEnvironment.AssetsFolder);
            rhetosAppEnvironment.LegacyPluginsFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, rhetosAppEnvironment.LegacyPluginsFolder);
            rhetosAppEnvironment.LegacyAssetsFolder = FilesUtility.RelativeToAbsolutePath(saveFolder, rhetosAppEnvironment.LegacyAssetsFolder);

            return rhetosAppEnvironment;
        }

        public static bool IsRhetosApplicationRootFolder(string rhetosAppRootPath)
        {
            var filePath = Path.Combine(rhetosAppRootPath, RhetosAppEnvironmentFileName);
            return File.Exists(filePath);
        }
    }
}
