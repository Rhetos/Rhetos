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
using System.IO;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    public class JsonFileSource : IConfigurationSource
    {
        private readonly string _filePath;
        private readonly bool _optional;

        public JsonFileSource(string filePath, bool optional = false)
        {
            _filePath = Path.GetFullPath(filePath);
            _optional = optional;
        }

        public string BaseFolder => Path.GetDirectoryName(_filePath);

        public IDictionary<string, IConfigurationValue> Load()
        {
            if (_optional && !File.Exists(_filePath))
                return new Dictionary<string, IConfigurationValue>();

            var jsonText = File.ReadAllText(_filePath);

            try
            {
                var jsonSource = new JsonSource(jsonText, Path.GetDirectoryName(_filePath));
                return jsonSource.Load();
            }
            catch (Exception e)
            {
                throw new FrameworkException($"Error parsing JSON contents from '{_filePath}'.", e);
            }
        }
    }
}
