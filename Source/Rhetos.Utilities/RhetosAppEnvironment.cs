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
    /// Run-time environment.
    /// </summary>
    public class RhetosAppEnvironment : IRhetosEnvironment
    {
        public static readonly string ConfigurationFileName = "rhetos-app.settings.json";
        public static readonly string LocalConfigurationFileName = "rhetos-app.local.settings.json";

        public string ApplicationRootFolder { get; set; }

        public string RhetosRuntimePath { get; set; }

        public string AssemblyFolder { get; set; }

        public string AssetsFolder { get; set; }
    }
}
