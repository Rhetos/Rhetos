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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Deployment
{
    [DebuggerDisplay("{ProvidedLocation}")]
    public class PackageSource
    {
        /// <summary>
        /// Available location options:
        /// 1. NuGet gallery ("http://..." or "https://...").
        /// 2. Local folder or network folder with NuGet packages.
        /// 3. Package project's source folder (unpacked, useful for package development).
        /// </summary>
        public PackageSource(string rootPath, string location)
        {
            if (string.IsNullOrEmpty(location))
                throw new UserException(
                    $"Invalid configuration file. Provided location is empty. Check the configuration files" +
                    $" {DeploymentConfiguration.SourcesConfigurationFileName} and {DeploymentConfiguration.PackagesConfigurationFileName}.");

            ProvidedLocation = location;

            if (System.Uri.IsWellFormedUriString(location, UriKind.Absolute))
                Uri = location;
            else
            {
                Path = System.IO.Path.Combine(rootPath, location);
                string pathError = null;
                try
                {
                    Path = System.IO.Path.GetFullPath(Path);
                }
                catch (Exception ex)
                {
                    pathError = ex.Message;
                }

                if (pathError != null || (!Directory.Exists(Path) && !File.Exists(Path)))
                    throw new UserException($"Invalid package source configuration." +
                        $" Provided location '{location}' is not a valid URI or an existing path ('{Path}')." +
                        $" Check the configuration files {DeploymentConfiguration.SourcesConfigurationFileName}" +
                        $" and {DeploymentConfiguration.PackagesConfigurationFileName}.");
            }
        }

        /// <summary>The original string that was provided in the configuration.
        /// It may be different from ProcessedLocation it a relative path is resolved, for example.</summary>
        public string ProvidedLocation { get; private set; }

        /// <summary>The processed location where the packages will be looked for.
        /// It may be different from ProvidedLocation it a relative path is resolved, for example.</summary>
        public string ProcessedLocation { get { return Uri ?? Path; } }

        /// <summary>
        /// Null, if the source is not a path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Null, if the source is not an URI.
        /// </summary>
        public string Uri { get; private set; }
    }
}
