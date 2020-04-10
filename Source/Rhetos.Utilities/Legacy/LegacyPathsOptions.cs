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

namespace Rhetos.Utilities
{
    /// <summary>
    /// Legacy paths, supporting build process with DeployPackages and obsolete <see cref="Paths"/> class.
    /// Available at both build-time and run-time.
    /// </summary>
    [Options("Legacy:Paths")]
    public class LegacyPathsOptions
    {
        /// <summary>
        /// At build-time, returns target bin folder with Rhetos framework libraries. At run-time returns host assembly folder.
        /// </summary>
        [OptionsPath]
        public string BinFolder { get; set; }

        /// <summary>
        /// Build process with DeployPackages places libraries from NuGet packages in separate plugins folder.
        /// </summary>
        [OptionsPath]
        public string PluginsFolder { get; set; }

        /// <summary>
        /// New plugins and applications should use <see cref="RhetosAppEnvironment.AssetsFolder"/> instead.
        /// </summary>
        [OptionsPath]
        public string ResourcesFolder { get; set; }
    }
}
