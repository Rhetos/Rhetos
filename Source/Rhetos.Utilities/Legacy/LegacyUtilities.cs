using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public static class LegacyUtilities
    {
        public static void Initialize(IConfigurationProvider configurationProvider)
        {
            var rhetosAppOptions = configurationProvider.GetOptions<RhetosAppOptions>();
            var rhetosAppEnvironment = new RhetosAppEnvironment(rhetosAppOptions);
            Paths.Initialize(rhetosAppEnvironment);
            ConfigUtility.Initialize(configurationProvider);
        }
    }
}
