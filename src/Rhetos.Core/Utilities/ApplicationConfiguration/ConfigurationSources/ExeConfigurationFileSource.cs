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

using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    public class ExeConfigurationFileSource : IConfigurationSource
    {
        private readonly string _filePath;

        public ExeConfigurationFileSource(string filePath)
        {
            _filePath = filePath;
        }

        public IDictionary<string, ConfigurationValue> Load(string rootDirectory)
        {
            string fullPath = Path.GetFullPath(Path.Combine(rootDirectory, _filePath));
            var configMap = new ExeConfigurationFileMap { ExeConfigFilename = fullPath };
            var exeConfiguration = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

            var configurationSource = new ConfigurationFileSource(exeConfiguration);
            return configurationSource.Load(rootDirectory);
        }

        public override string ToString()
        {
            return $"Exe configuration file '{_filePath}'";
        }
    }
}
