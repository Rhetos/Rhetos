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

namespace Rhetos.Utilities.ApplicationConfiguration
{
    /// <summary>
    /// Minimal content of the configuration file <see cref="RhetosAppConfiguration.ConfigurationFileName"/>.
    /// This class is intended for internal initialization of configuration, it should not be used directly as an options class.
    /// Use <see cref="RhetosAppEnvironment"/> instead to read runtime configuration.
    /// </summary>
    public class RhetosAppConfiguration
    {
        public static readonly string ConfigurationFileName = "rhetos-app.settings.json";

        public string RhetosRuntimePath { get; set; }
        public string AssetsFolder { get; set; }
        /// <summary>
        /// Optional.
        /// </summary>
        public string Legacy__PluginsFolders { get; set; }
    }
}
