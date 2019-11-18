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

namespace Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources
{
    public class CommandLineArgumentsSource : IConfigurationSource
    {
        private readonly string[] args;
        private readonly string argumentPrefix;
        private readonly string configurationPath;

        public CommandLineArgumentsSource(string[] args, string argumentPrefix, string configurationPath = "")
        {
            this.args = args;
            this.argumentPrefix = argumentPrefix;
            this.configurationPath = configurationPath;
        }

        public IDictionary<string, object> Load()
        {
            var argsTrimmed = args.Select(arg => arg.TrimStart(argumentPrefix.ToCharArray())).Where(arg => !string.IsNullOrWhiteSpace(arg));
            if (!string.IsNullOrEmpty(configurationPath))
                argsTrimmed = argsTrimmed.Select(arg => $"{configurationPath}{ConfigurationProvider.ConfigurationPathSeparator}{arg}").ToArray();
            
            return argsTrimmed
                .ToDictionary(arg => arg, _ => (object)true);
        }
    }
}
