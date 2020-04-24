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

namespace Rhetos.Utilities
{
    /// <summary>
    /// Legacy configuration class.
    /// 
    /// Legacy code that accessed this class indirectly through dependency injection referenced only <see cref="IConfiguration"/> interface.
    /// This interface is now resolved to new implementation: <see cref="ConfigurationProvider"/>.
    /// 
    /// Legacy code that accessed this class directly has used it with default constructor, without dependency injection,
    /// expecting it to read global system configuration. This class is left as a support for that legacy code.
    /// </summary>
    [Obsolete("Use IConfiguration instead.")]
    public class Configuration : IConfiguration
    {
        private static IConfiguration _staticConfigurationProvider;

        internal static void Initialize(IConfiguration configuration)
        {
            _staticConfigurationProvider = configuration;
        }

        public Lazy<string> GetString(string key, string defaultValue)
        {
            return new Lazy<string>(() => _staticConfigurationProvider.GetValue(key, defaultValue));
        }

        public Lazy<int> GetInt(string key, int defaultValue)
        {
            return new Lazy<int>(() => _staticConfigurationProvider.GetValue(key, defaultValue));
        }

        public Lazy<bool> GetBool(string key, bool defaultValue)
        {
            return new Lazy<bool>(() => _staticConfigurationProvider.GetValue(key, defaultValue));
        }

        public Lazy<T> GetEnum<T>(string key, T defaultValue) where T : struct
        {
            return new Lazy<T>(() => _staticConfigurationProvider.GetValue(key, defaultValue));
        }

        string _legacyError => $"Legacy class {nameof(Configuration)} does not implement new {nameof(IConfiguration)} members. Please use {nameof(IConfiguration)} from DI container instead.";

        public IEnumerable<string> AllKeys => throw new FrameworkException(_legacyError);

        public T GetValue<T>(string configurationKey, T defaultValue = default, string configurationPath = "") => throw new FrameworkException(_legacyError);

        public T GetOptions<T>(string configurationPath = "", bool requireAllMembers = false) where T : class => throw new FrameworkException(_legacyError);
    }
}
