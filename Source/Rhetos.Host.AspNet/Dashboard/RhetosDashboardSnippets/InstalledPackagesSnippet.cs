using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rhetos.Deployment;

namespace Rhetos.Host.AspNet.Dashboard.RhetosDashboardSnippets
{
    public class InstalledPackagesSnippet : ViewComponent
    {
        private readonly IRhetosComponent<InstalledPackages> _installedPackages;

        public InstalledPackagesSnippet(IRhetosComponent<InstalledPackages> installedPackages)
        {
            _installedPackages = installedPackages;
        }

        public Task<IViewComponentResult> InvokeAsync()
        {
            var result = View("~/Dashboard/RhetosDashboardSnippets/InstalledPackages.cshtml", _installedPackages.Value.Packages);
            return Task.FromResult((IViewComponentResult)result);
        }
    }
}
