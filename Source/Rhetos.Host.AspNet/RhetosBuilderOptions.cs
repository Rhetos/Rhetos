using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Host.AspNet
{
    public class RhetosBuilderOptions
    {
        public List<Action<IRhetosHostBuilder>> rhetosHostBuilderConfigureActions = new List<Action<IRhetosHostBuilder>>();
    }
}
