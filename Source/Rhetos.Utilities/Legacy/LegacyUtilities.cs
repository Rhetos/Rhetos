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
using System.Linq;

namespace Rhetos
{
    public static class LegacyUtilities
    {
#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Use to initialize obsolete static utilities <see cref="Paths"/>, <see cref="ConfigUtility"/>, <see cref="Configuration"/> and <see cref="SqlUtility"/> 
        /// prior to using any of their methods. This will bind those utilities to configuration source compliant with new configuration convention.
        /// </summary>
        public static void Initialize(IConfigurationProvider configurationProvider)
        {
            Paths.Initialize(configurationProvider.GetOptions<RhetosAppEnvironment>());
            ConfigUtility.Initialize(configurationProvider);
            SqlUtility.Initialize(configurationProvider);
            Configuration.Initialize(configurationProvider);
        }

        /// <summary>
        /// Returns list of assemblies that will be scanned for plugin exports.
        /// </summary>
        public static Func<string[]> GetListAssembliesDelegate(IConfigurationProvider configurationProvider)
        {
            var rhetosAppEnvironment = configurationProvider.GetOptions<RhetosAppEnvironment>();

            return () =>
            {
                string[] pluginsPaths = new[]
                {
                    rhetosAppEnvironment.LegacyPluginsFolder ?? rhetosAppEnvironment.AssemblyFolder, // When using separate LegacyPluginsFolder, there is no need to scan BinFolder, because Rhetos framework binaries do not contain plugins exports (only explicit registrations).
                    rhetosAppEnvironment.AssetsFolder // TODO: Remove AssetsFolder after modifying AssemblyGenerator to not build DLLs.
                };

                var assemblyFiles = pluginsPaths
                    .Where(folder => Directory.Exists(folder)) // Some paths don't exist in certain phases of build and deployment.
                    .SelectMany(folder => Directory.GetFiles(folder, "*.dll", SearchOption.TopDirectoryOnly))
                    .Distinct()
                    .OrderBy(file => file)
                    .ToArray();

                return assemblyFiles;
            };
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
