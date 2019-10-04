using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public class InitializationContext
    {
        public ILogger Logger { get; }
        public IConfiguration Configuration { get; }
        public RhetosAppEnvironment RhetosAppEnvironment { get; }
    }
}
