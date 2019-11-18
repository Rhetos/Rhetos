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

namespace Rhetos.TestCommon
{
    public class MockConfiguration : Dictionary<string, object>, IConfiguration
    {
        private readonly bool _defaultToSystemConfiguration;
        private readonly Configuration _systemConfiguration = new Configuration(new ConfigurationBuilder().AddConfigurationManagerConfiguration().Build());

        public MockConfiguration()
        {
            _defaultToSystemConfiguration = false;
        }

        public MockConfiguration(bool defaultToSystemConfiguration)
        {
            _defaultToSystemConfiguration = defaultToSystemConfiguration;
        }

        public Lazy<bool> GetBool(string key, bool defaultValue) => Get(key, defaultValue, _systemConfiguration.GetBool);

        public Lazy<T> GetEnum<T>(string key, T defaultValue) where T : struct => Get(key, defaultValue, _systemConfiguration.GetEnum<T>);

        public Lazy<int> GetInt(string key, int defaultValue) => Get(key, defaultValue, _systemConfiguration.GetInt);

        public Lazy<string> GetString(string key, string defaultValue) => Get(key, defaultValue, _systemConfiguration.GetString);

        private Lazy<T> Get<T>(string key, T defaultValue, Func<string, T, Lazy<T>> systemConfigurationGetter)
        {
            object value;
            if (!TryGetValue(key, out value))
                if (_defaultToSystemConfiguration)
                    return systemConfigurationGetter(key, defaultValue);
                else
                    value = defaultValue;
            return new Lazy<T>(() => (T)value);
        }
    }
}
