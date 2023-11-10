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

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Provides SQL code snippets, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
    /// </summary>
    public class SqlResources : ISqlResources
    {
        private readonly string _databaseLanguage;
        private readonly IPluginsContainer<ISqlResourcesPlugin> _plugins;
        private readonly Lazy<Dictionary<string, string>> _resources;
        private readonly ILogger _logger;

        public SqlResources(DatabaseSettings databaseSettings, IPluginsContainer<ISqlResourcesPlugin> plugins, ILogProvider logProvider)
        {
            _databaseLanguage = databaseSettings.DatabaseLanguage;
            _plugins = plugins;
            _resources = new(LoadResources);
            _logger = logProvider.GetLogger(GetType().Name);
        }

        private Dictionary<string, string> LoadResources()
        {
            var result = new Dictionary<string, string>();
            foreach (var plugin in _plugins.GetPlugins())
            {
                var resources = plugin.GetResources();
                if (resources != null)
                {
                    _logger.Trace(() => $"Plugin {plugin.GetType()} supports current database language.");
                    foreach (var resource in resources)
                    {
                        if (string.IsNullOrEmpty(resource.Key))
                            throw new ArgumentException($"{plugin.GetType()} returns an empty resource key.");
                        if (resource.Value == null)
                            throw new ArgumentException($"{plugin.GetType()} returns resource value null.");
                        if (result.ContainsKey(resource.Key))
                            _logger.Trace(() => $"Plugin {plugin.GetType()} overrides previous key {resource.Key}.");
                        result[resource.Key] = resource.Value;
                    }
                }
                else
                    _logger.Trace(() => $"Plugin {plugin.GetType()} does not support current database language.");
            }
            return result;
        }

        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Return <see langword="null"/> if there is no resource with the given <paramref name="key"/>.
        /// </summary>
        public string TryGet(string key) => _resources.Value.GetValueOrDefault(key, null);

        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Throw an exception if there is no snippet available with the given <paramref name="key"/>.
        /// </summary>
        public string Get(string key) => TryGet(key) ?? throw new FrameworkException($"Missing SQL resource '{key}' for database language '{_databaseLanguage}'.");

        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Throw an exception if there is no snippet available with the given <paramref name="key"/>.
        /// </summary>
        public string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }
}
