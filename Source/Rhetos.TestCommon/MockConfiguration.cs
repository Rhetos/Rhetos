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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.TestCommon
{
    public class MockConfiguration : Dictionary<string, object>, IConfiguration
    {
        public Lazy<bool> GetBool(string key, bool defaultValue) => Get(key, defaultValue);

        public Lazy<T> GetEnum<T>(string key, T defaultValue) where T : struct => Get(key, defaultValue);

        public Lazy<int> GetInt(string key, int defaultValue) => Get(key, defaultValue);

        public Lazy<string> GetString(string key, string defaultValue) => Get(key, defaultValue);

        private Lazy<T> Get<T>(string key, T defaultValue)
        {
            object value;
            if (!TryGetValue(key, out value))
                value = defaultValue;
            return new Lazy<T>(() => (T)value);
        }
    }
}
