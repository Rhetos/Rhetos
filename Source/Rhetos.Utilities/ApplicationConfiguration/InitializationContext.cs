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

        public InitializationContext(ILogProvider logProvider, IConfigurationProvider configurationProvider, RhetosAppEnvironment rhetosAppEnvironment)
        {
            this.LogProvider = logProvider;
            this.ConfigurationProvider = configurationProvider;
            this.RhetosAppEnvironment = rhetosAppEnvironment;
        }
    }
}
