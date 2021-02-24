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

using System;
using System.IO;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Runtime configuration settings.
    /// </summary>
    [Options("Rhetos:App")]
    public class RhetosAppOptions : IAssetsOptions
    {
        /// <summary>
        /// Host app folder.
        /// </summary>
        /// <remarks>
        /// The value is automatically set by generated application code. It may be customized by standard runtime configuration.
        /// </remarks>
        [AbsolutePathOption]
        public string RhetosHostFolder { get; set; }

        /// <summary>
        /// Rhetos application's assembly name where the generated code is located icluding the Domain Object Model.
        /// </summary>
        /// <remarks>
        /// The value is automatically set by generated application code. It may be customized by standard runtime configuration.
        /// </remarks>
        public string RhetosAppAssemblyName { get; set; }

        private string _assetsFolder;

        /// <summary>
        /// Run-time assets folder.
        /// </summary>
        /// <remarks>
        /// If not configured, default value is "RhetosAssets" subfolder where <see cref="RhetosHostFolder"/> is located.
        /// </remarks>
        [AbsolutePathOption]
        public string AssetsFolder { get => _assetsFolder ?? GetDirectory(RhetosHostFolder, "RhetosAssets"); set => _assetsFolder = value; }

        private string _cacheFolder;

        /// <summary>
        /// Run-time cache folder.
        /// </summary>
        /// <remarks>
        /// If not configured, default value is the folder where <see cref="RhetosHostFolder"/> is located.
        /// <see cref="AssetsFolder"/> is not practical for runtime cache during development, because it is deleted on each build.
        /// </remarks>
        public string CacheFolder { get => _cacheFolder ?? GetDirectory(RhetosHostFolder, "."); set => _cacheFolder = value; }

        public bool EntityFrameworkUseDatabaseNullSemantics { get; set; } = true;

        public double AuthorizationCacheExpirationSeconds { get; set; } = 30;

        public bool AuthorizationAddUnregisteredPrincipals { get; set; } = false;

        private string GetDirectory(string baseFolderPath, string directoryRelativePath)
        {
            return !string.IsNullOrEmpty(baseFolderPath)
                ? Path.GetFullPath(Path.Combine(baseFolderPath, directoryRelativePath))
                : null;
        }
    }
}
