using System;
using Microsoft.AspNetCore.Mvc;

namespace Rhetos.Host.AspNet.Dashboard
{
    public class RhetosDashboardController : Controller
    {
        public RhetosDashboardController()
        {
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View("~/Dashboard/RhetosDashboardSnippets/Dashboard.cshtml");
        }
    }
}
