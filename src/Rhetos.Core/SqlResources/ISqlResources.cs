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
using System.Globalization;

namespace Rhetos
{
    /// <summary>
    /// Provides SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
    /// </summary>
    public interface ISqlResources
    {
        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Return <see langword="null"/> if there is no resource with the given <paramref name="key"/>.
        /// </summary>
        string TryGet(string key);
    }

    public static class SqlResourcesExtensions
    {
        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Throw an exception if there is no snippet available with the given <paramref name="key"/>.
        /// </summary>
        public static string Get(this ISqlResources sqlResources, string key)
            => sqlResources.TryGet(key) ?? throw new FrameworkException($"Missing the SQL resource '{key}' for the current database language.");

        /// <summary>
        /// Returns the SQL code snippets from resource files, based on the selected database language in <see cref="DatabaseSettings.DatabaseLanguage"/>.
        /// Throw an exception if there is no snippet available with the given <paramref name="key"/>.
        /// </summary>
        public static string Format(this ISqlResources sqlResources, string key, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, sqlResources.Get(key), args);
    }
}
