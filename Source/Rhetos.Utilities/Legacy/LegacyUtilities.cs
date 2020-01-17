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
using System.Collections.Generic;
using System.IO;

namespace Rhetos
{
    public static class LegacyUtilities
    {
#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Use to initialize obsolete static utilities <see cref="Paths"/>, <see cref="ConfigUtility"/> and <see cref="SqlUtility"/> 
        /// prior to using any of their methods. This will bind those utilities to configuration source compliant with new configuration convention.
        /// </summary>
        public static void Initialize(IConfigurationProvider configurationProvider)
        {
            var rhetosAppOptions = configurationProvider.GetOptions<RhetosAppOptions>();
            var assetsOptions = configurationProvider.GetOptions<AssetsOptions>();
            Paths.Initialize(configurationProvider.GetValue<string>("RootPath"), rhetosAppOptions, assetsOptions);
            ConfigUtility.Initialize(configurationProvider);
            SqlUtility.Initialize(configurationProvider);
        }

        /// <summary>
        /// Returns list of assemblies that will be scanned for plugin exports.
        /// </summary>
        public static Func<List<string>> GetListAssembliesDelegate()
        {
            return () =>
            {
                // Rhetos framework does not contain plugins exports (only explicit registration), so only plugins and generated files need to be scanned for plugins.
                string[] pluginsPath = new[] { Paths.PluginsFolder, Paths.GeneratedFolder };

                List<string> assemblies = new List<string>();
                foreach (var path in pluginsPath)
                    if (Directory.Exists(path)) // Some paths don't exist in certain phases of build and deployment.
                        assemblies.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly));

                assemblies.Sort();
                return assemblies;
            };
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
