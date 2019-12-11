using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Deployment
{
    public class SimpleInstalledPackages : IInstalledPackages
    {
        private readonly List<InstalledPackage> _packages;

        public IEnumerable<InstalledPackage> Packages => _packages;

        public SimpleInstalledPackages(List<InstalledPackage> packages)
        {
            _packages = packages;
        }
    }
}
