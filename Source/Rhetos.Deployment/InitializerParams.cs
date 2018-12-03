using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Deployment
{
    /// <summary>
    /// This class contains condition for executing initializers
    /// </summary>
    public class InitializerParams
    {
        public bool SkipRecompute { get; }
        public InitializerParams(Arguments arguments)
        {
            SkipRecompute = arguments != null ? arguments.SkipRecompute : false;
        }
    }
}
