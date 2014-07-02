using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeployPackages
{
    class DeployPackagesException : Rhetos.RhetosException
    {
        public DeployPackagesException(string message)
            : base(message)
        {
        }
    }
}
