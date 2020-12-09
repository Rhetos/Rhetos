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

namespace Rhetos.Utilities
{
    [Obsolete("Instead of this static configuration, use IConfiguration with dependency injection.")]
    public static class ConfigUtility
    {
        private static IConfiguration _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Reads the application's configuration.
        /// </summary>
        public static string GetAppSetting(string key)
        {
            ThrowIfNotInitialized();
            return _configuration.GetValue<string>(key);
        }

        private static void ThrowIfNotInitialized()
        {
            if (_configuration == null)
                throw new FrameworkException("ConfigUtility is not initialized. Use LegacyUtilities.Initialize() to initialize obsolete static utilities or use the new IConfiguration.");
        }
    }
}
