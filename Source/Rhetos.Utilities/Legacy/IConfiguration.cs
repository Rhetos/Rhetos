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

namespace Rhetos.Utilities
{
    public interface IConfiguration
    {
        T GetValue<T>(string configurationKey, T defaultValue = default(T), string configurationPath = "");

        T GetOptions<T>(string configurationPath = "", bool requireAllMembers = false) where T : class;

        IEnumerable<string> AllKeys { get; }

        [Obsolete("Use GetValue or GetOptions instead")]
        Lazy<string> GetString(string key, string defaultValue);

        [Obsolete("Use GetValue or GetOptions instead")]
        Lazy<int> GetInt(string key, int defaultValue);

        [Obsolete("Use GetValue or GetOptions instead")]
        Lazy<bool> GetBool(string key, bool defaultValue);

        [Obsolete("Use GetValue or GetOptions instead")]
        Lazy<T> GetEnum<T>(string key, T defaultValue) where T : struct;
    }
}
