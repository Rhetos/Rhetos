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

using Rhetos.Utilities;
using System;

namespace Rhetos.Dsl.Test
{
    internal class NullConfiguration : IConfiguration
    {
        public Lazy<bool> GetBool(string key, bool defaultValue)
        {
            return new Lazy<bool>(() => defaultValue);
        }

        public Lazy<T> GetEnum<T>(string key, T defaultValue) where T : struct
        {
            return new Lazy<T>(() => defaultValue);
        }

        public Lazy<int> GetInt(string key, int defaultValue)
        {
            return new Lazy<int>(() => defaultValue);
        }

        public Lazy<string> GetString(string key, string defaultValue)
        {
            return new Lazy<string>(() => defaultValue);
        }
    }
}
