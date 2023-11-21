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

namespace Rhetos
{
    /// <summary>
    /// Dependency injection container builder, initialized with <see cref="IConfiguration"/>.
    /// It makes <see cref="ILogProvider"/> and <see cref="IPluginScanner"/> accessible during registration process
    /// (see <see cref="ContainerBuilderExtensions"/> method),
    /// and registers <see cref="IConfiguration"/> to the container.
    /// </summary>
    public static class RhetosContainerBuilder
    {
        /// <summary>
        /// Initializes a dependency injection container with specified <see cref="IConfiguration"/>. 
        /// Registers <see cref="IConfiguration"/> instance to newly created container.
        /// </summary>
        /// <remarks>
        /// <see cref="ILogProvider"/> is not registered to container and is meant to be used during the lifetime of registration and container building process.
        /// </remarks>
        public static ContainerBuilder Create(IConfiguration configuration, ILogProvider logProvider, IPluginScanner pluginScanner)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterInstance(configuration).ExternallyOwned();

            // make properties accessible to modules which are provided with new/unique instance of ContainerBuilder
            containerBuilder.Properties.Add(nameof(IPluginScanner), pluginScanner);
            containerBuilder.Properties.Add(nameof(ILogProvider), logProvider);
            containerBuilder.Properties.Add(nameof(IConfiguration), configuration);

            return containerBuilder;
        }
    }

    /// <summary>
    /// Phases in the Rhetos app development or deployment lifecycle.
    /// </summary>
    public class ExecutionStage
    {
        /// <summary>
        /// The phase when Rhetos parses DSL scripts are generates C#, SQL and other files.
        /// </summary>
        public bool IsBuildTime { get; init; }

        /// <summary>
        /// The phase when Rhetos executes data-migration scripts, and creates/deleted database objects.
        /// This execution stage *does not* include the application initialization phase from the 'rhetos dbupdate' command (RecomputeOnDeploy).
        /// </summary>
        public bool IsDatabaseUpdate { get; init; }

        /// <summary>
        /// The application initialization phase from the 'rhetos dbupdate' (RecomputeOnDeploy, ClaimGenerator and similar).
        /// When <see cref="IsApplicationInitialization"/> is set, then the <see cref="IsRuntime"/> flag is also set
        /// because then application initialization is executed under the full runtime context.
        /// </summary>
        public bool IsApplicationInitialization { get; init; }

        /// <summary>
        /// Runtime includes the final usage of the application by the end user, as well as
        /// the application initialization phase from the 'rhetos dbupdate' command (RecomputeOnDeploy and similar).
        /// </summary>
        public bool IsRuntime { get; init; }
    };
}
