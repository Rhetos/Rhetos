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

using System.Linq;
using System.Reflection;

namespace Rhetos.Utilities
{
    public static class SystemUtility
    {
        private static string _rhetosVersion = null;

        /// <summary>
        /// Rhetos framework version.
        /// Note that it is not compatible with System.Version because Rhetos version may contain
        /// textual pre-release information and build metadata (see Semantic Versioning 2.0.0 for example).
        /// </summary>
        public static string GetRhetosVersion()
        {
            if (_rhetosVersion == null)
            {
                var versionAttributes = typeof(SystemUtility).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).Cast<AssemblyInformationalVersionAttribute>();
                _rhetosVersion = versionAttributes.Single().InformationalVersion;
            }
            return _rhetosVersion;
        }
    }
}
