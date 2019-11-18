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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos
{
    /// <summary>
    /// Provider necessary to initialize Rhetos framework.
    /// Used before dependency injection is configured and running for all shared resources and context information.
    /// </summary>
    public class InitializationContext
    {
        public ILogProvider LogProvider { get; }
        public IConfigurationProvider ConfigurationProvider { get; }
        public RhetosAppEnvironment RhetosAppEnvironment { get; }

        /// <summary>
        /// Creates a a new context with specified arguments.
        /// </summary>
        public InitializationContext(IConfigurationProvider configurationProvider, ILogProvider logProvider, RhetosAppEnvironment rhetosAppEnvironment)
        {
            this.ConfigurationProvider = configurationProvider;
            this.LogProvider = logProvider;
            this.RhetosAppEnvironment = rhetosAppEnvironment;
        }

        /// <summary>
        /// Creates a context with <see cref="RhetosAppEnvironment"/> automatically resolved and created from provided configuration.
        /// </summary>
        public InitializationContext(IConfigurationProvider configurationProvider, ILogProvider logProvider)
            : this(configurationProvider, logProvider, new RhetosAppEnvironment(configurationProvider.GetOptions<RhetosAppOptions>().RootPath)) 
        { }

    }
}
