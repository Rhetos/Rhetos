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

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public class ConfigurationKeyComparer : IEqualityComparer<string>
    {
        public bool Equals(string key1, string key2)
        {
            if (key1 == null && key2 == null) return true;
            if (key1 == null || key2 == null) return false;

            return StringComparer.InvariantCultureIgnoreCase.Equals(Normalize(key1), Normalize(key2));
        }

        public int GetHashCode(string key)
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(Normalize(key));
        }

        private static string Normalize(string key)
            => key.Replace(ConfigurationProviderOptions.ConfigurationPathSeparatorAlternative, ConfigurationProviderOptions.ConfigurationPathSeparator);
    }
}
