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

using Rhetos.Logging;
using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration;
using Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources;
using System.Collections.Generic;

namespace Rhetos
{
    public class ConfigurationBuilder : IConfigurationBuilder
    {
        private readonly List<IConfigurationSource> configurationSources = new List<IConfigurationSource>();
        private ILogProvider _builderLogProvider;

        public ConfigurationBuilder(ILogProvider builderLogProvider)
        {
            _builderLogProvider = builderLogProvider;
        }

        public IConfigurationBuilder UseBuilderLogProvider(ILogProvider builderLogProvider)
        {
            _builderLogProvider = builderLogProvider;
            return this;
        }

        public IConfigurationBuilder Add(IConfigurationSource source)
        {
            configurationSources.Add(source);
            return this;
        }

        public IConfiguration Build()
        {
            var configurationValues = new Dictionary<string, ConfigurationValue>(new ConfigurationKeyComparer());

            foreach (var configurationSource in configurationSources)
            {
                var sourceValues = configurationSource.Load();

                foreach (var sourceValue in sourceValues)
                {
                    if (string.IsNullOrWhiteSpace(sourceValue.Key))
                        throw new FrameworkException("Trying to add empty or null configuration key.");

                    configurationValues[sourceValue.Key] = sourceValue.Value;
                }
            }
            return new ConfigurationProvider(configurationValues, _builderLogProvider);
        }
    }
}
