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

using Autofac;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos
{
    /// <summary>
    /// Container builder initialized with <see cref="InitializationContext"/>.
    /// It makes the <see cref="InitializationContext"/> and <see cref="IPluginScanner"/> accessible during registration process and
    /// registers <see cref="IConfigurationProvider"/> and <see cref="RhetosAppOptions"/> to the container.
    /// </summary>
    public class RhetosContainerBuilder : ContainerBuilder
    {
        /// <summary>
        /// Initializes a container with specified <see cref="InitializationContext"/>. 
        /// Registers <see cref="IConfigurationProvider"/> and <see cref="RhetosAppOptions"/> instances to newly created container.
        /// <see cref="ILogProvider"/> is not registered and is meant to be used during the lifetime of registration and container building process.
        /// <see cref="LegacyUtilities"/> will also be initialized with the given configuration.
        /// </summary>
        public RhetosContainerBuilder(IConfigurationProvider configurationProvider, ILogProvider logProvider, List<string> assemblies)
        {
            this.RegisterInstance(configurationProvider);

            var pluginScanner = new MefPluginScanner(assemblies, logProvider);
            this.RegisterInstance(pluginScanner).As<IPluginScanner>();

            // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
            this.Properties.Add(nameof(IPluginScanner), pluginScanner);
            this.Properties.Add(nameof(ILogProvider), logProvider);

            // this is a patch/mock to provide backward compatibility for all usages of old static classes
            LegacyUtilities.Initialize(configurationProvider);

            Plugins.Initialize(builder => builder.GetPluginRegistration());
        }

        public RhetosContainerBuilder(IConfigurationProvider configurationProvider, ILogProvider logProvider) :
            this(configurationProvider, logProvider, SearchForAssemblies(configurationProvider))
        {
        }

        private static List<string> SearchForAssemblies(IConfigurationProvider configurationProvider)
        {
            var rhetosAppOptions = configurationProvider.GetOptions<RhetosAppOptions>();
            if (string.IsNullOrEmpty(rhetosAppOptions.BinFolder))
                throw new FrameworkException("");
            return Directory.GetFiles(rhetosAppOptions.BinFolder, "*.dll").Union(Directory.GetFiles(rhetosAppOptions.BinFolder, "*.exe")).ToList();
        }
    }
}
