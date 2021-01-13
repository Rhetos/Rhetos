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
    [Obsolete("Use RhetosAppOptions and RhetosAppEnvironment for run-time folders. Use RhetosBuildEnvironment for build-time folder. Use IAssetsOptions.AssetsFolder for files generated at build-time and read at run-time.")]
    public static class Paths
    {
        private static string _rhetosServerRootPath;
        private static string _resourcesFolder;
        private static string _environment;

        /// <summary>
        /// Initialize legacy Paths for the Rhetos application.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            var runtimeEnvironment = configuration.GetOptions<RhetosAppEnvironment>();
            var buildEnvironment = configuration.GetOptions<RhetosBuildEnvironment>();

            _rhetosServerRootPath = buildEnvironment.ProjectFolder ?? runtimeEnvironment.ApplicationRootFolder;
            _resourcesFolder = _rhetosServerRootPath != null ? Path.Combine(_rhetosServerRootPath, "Resources") : null;

            if (buildEnvironment?.ProjectFolder != null)
                _environment = "build";
            else if (runtimeEnvironment?.ApplicationRootFolder != null)
                _environment = "run-time";
            else
                _environment = "unspecified";
        }

        public static string RhetosServerRootPath => PathOrError(_rhetosServerRootPath, "RhetosServerRootPath");

        public static string ResourcesFolder => AbsolutePathOrError(_resourcesFolder, "ResourcesFolder");

        private static string PathOrError(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
                throw new FrameworkException($"Paths property '{name}' is not configured in '{_environment}' environment.");
            return path;
        }

        private static string AbsolutePathOrError(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
                throw new FrameworkException($"Paths property '{name}' is not configured in '{_environment}' environment.");
            if (string.IsNullOrEmpty(_rhetosServerRootPath))
            {
                if (path == Path.GetFullPath(path))
                    return path;
                else
                    throw new FrameworkException($"Paths property '{name}' is not configured correctly in '{_environment}' environment." +
                        $" Specified relative path '{path}' without known root folder ({nameof(RhetosServerRootPath)}).");
            }
            return Path.GetFullPath(Path.Combine(_rhetosServerRootPath, path));
        }
    }
}
