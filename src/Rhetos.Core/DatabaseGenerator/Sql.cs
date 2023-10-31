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
using System.Globalization;
using System.Resources;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Provides SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
    /// </summary>
    public static class Sql
    {
        private static string _databaseLanguage;
        private static readonly Lazy<ResourceManager> _resourceManager = new(() => CreateResourceManager(typeof(Sql)));

        /// <summary>
        /// Initialization of the static members.
        /// This class provides static features in order to simplify usage in large number of class.
        /// </summary>
        public static void Initialize(DatabaseSettings databaseSettings)
        {
            if (databaseSettings != null)
            {
                if (_databaseLanguage != null && _databaseLanguage != databaseSettings.DatabaseLanguage)
                    throw new ArgumentException($"{typeof(Sql)} class has already been initialized with database language '{_databaseLanguage}'. The new value is '{databaseSettings.DatabaseLanguage}'.");

                _databaseLanguage = databaseSettings.DatabaseLanguage;
            }
            else
            {
                // TODO: Currently it is not supported to remove _resourceManager and later initialize it with a different language,
                // because it would not cover ResourceManager in plugins, such as CommonConcepts.
                // This may be changed later if we implement a different method of SQL language support plugins.
            }
        }

        public static ResourceManager CreateResourceManager(Type type)
        {
            if (_databaseLanguage == null)
                throw new FrameworkException($"{typeof(Sql)} class has not been initialized. Note that it is intended to be used at build-time, not at runtime.");

            string resourceName = type.Namespace + ".Sql." + _databaseLanguage;
            var resourceAssembly = type.Assembly;
            return new ResourceManager(resourceName, resourceAssembly);
        }

        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Return <see langword="null"/> if there is no resource with the given <paramref name="key"/>.
        /// </summary>
        public static string TryGet(string key) => _resourceManager.Value.GetString(key);

        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Throw an exception if there is no snippet available with the given <paramref name="key"/>.
        /// </summary>
        public static string Get(string key) => TryGet(key) ?? throw new FrameworkException($"Missing SQL resource '{key}' for database language '{_databaseLanguage}'.");

        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Throw an exception if there is no snippet available with the given <paramref name="key"/>.
        /// </summary>
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }
}
