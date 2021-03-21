using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Host.AspNet
{
    internal class RhetosHostBuilderOptions
    {
        public List<Action<IRhetosHostBuilder>> ConfigureActions { get; set; } = new List<Action<IRhetosHostBuilder>>();
    }
}
