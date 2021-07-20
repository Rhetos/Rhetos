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
using System.Reflection;

namespace Rhetos
{
    /// <summary>
    /// Class marked with this attribute represent a Rhetos options class, with property binding to the specified section in the application's configuration.
    /// </summary>
    /// <remarks>
    /// Note that this attribute is not required for Rhetos options classes; the default <see cref="ConfigurationPath"/> is an empty string.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class OptionsAttribute : Attribute
    {
        /// <param name="configurationPath">
        /// Default configuration path that will be applied in GetOptions and AddOptions methods.
        /// Use <see cref="ConfigurationProviderOptions.ConfigurationPathSeparator"/> as a separator in the path.
        /// </param>
        public OptionsAttribute(string configurationPath)
        {
            ConfigurationPath = configurationPath;
        }

        /// <summary>
        /// Default configuration path that will be applied in GetOptions and AddOptions methods.
        /// Use <see cref="ConfigurationProviderOptions.ConfigurationPathSeparator"/> as a separator in the path.
        /// </summary>
        public string ConfigurationPath { get; }

        public static string GetConfigurationPath(Type optionsType)
            => optionsType.GetCustomAttribute<OptionsAttribute>()?.ConfigurationPath ?? "";

        public static string GetConfigurationPath<T>()
            => GetConfigurationPath(typeof(T));
    }
}
