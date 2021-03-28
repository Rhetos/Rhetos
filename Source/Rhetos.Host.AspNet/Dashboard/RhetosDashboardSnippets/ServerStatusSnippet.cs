using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Rhetos.Host.AspNet.Dashboard.RhetosDashboardSnippets
{
    public class ServerStatusSnippet : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync()
        {
            var result = View("~/Dashboard/RhetosDashboardSnippets/ServerStatus.cshtml");
            return Task.FromResult((IViewComponentResult)result);
        }
    }
}
