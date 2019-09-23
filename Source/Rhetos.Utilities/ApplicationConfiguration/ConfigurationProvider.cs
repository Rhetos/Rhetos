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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private readonly Dictionary<string, object> configurationValues;

        public ConfigurationProvider(Dictionary<string, object> configurationValues)
        {
            this.configurationValues = configurationValues;
        }

        public T ConfigureOptions<T>(string configurationPath = "")
        {
            throw new NotImplementedException();
        }

        public T GetValue<T>(string configurationKey, T defaultValue, string configurationPath = "")
        {
            if (!string.IsNullOrEmpty(configurationPath))
                configurationKey = $"{configurationPath}__{configurationKey}";
            
            if (!configurationValues.TryGetValue(configurationKey, out var value))
                return defaultValue;

            return Convert<T>(value);
        }

        private T Convert<T>(object value)
        {
            throw new NotImplementedException();
        }
    }
}
