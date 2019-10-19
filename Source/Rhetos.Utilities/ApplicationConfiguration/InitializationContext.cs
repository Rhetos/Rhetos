using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration
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

        public InitializationContext(IConfigurationProvider configurationProvider, ILogProvider logProvider, RhetosAppEnvironment rhetosAppEnvironment)
        {
            this.ConfigurationProvider = configurationProvider;
            this.LogProvider = logProvider;
            this.RhetosAppEnvironment = rhetosAppEnvironment;
        }

        /// <summary>
        /// Creates a context with RhetosAppEnvironment automatically resolved and created from provided configuration.
        /// </summary>
        public InitializationContext(IConfigurationProvider configurationProvider, ILogProvider logProvider)
            : this(configurationProvider, logProvider, new RhetosAppEnvironment(configurationProvider.GetOptions<RhetosAppOptions>())) 
        { }

    }
}
