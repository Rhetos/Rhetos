using Autofac.Integration.WebApi;
using Rhetos;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace RhetosWebApi.Controllers
{
    [System.Web.Mvc.Authorize]
    public class HomeController : Controller
    {
        // GET: Home
        public HomeController()
        {
        }
        public ActionResult Index()
        {
            var dependencyResolver = (AutofacWebApiDependencyResolver)GlobalConfiguration.Configuration.DependencyResolver;
            var container = dependencyResolver.Container;
            
            var snippets = Autofac.ResolutionExtensions.Resolve<IPluginsContainer<IHomePageSnippet>>(container);

            string htmlSnippet = String.Empty;
            foreach (var snippet in snippets.GetPlugins())
                htmlSnippet += snippet.Html;
            ViewBag.Snippet = htmlSnippet;

            string htmlPackage = String.Empty;
            var installedPackages = Autofac.ResolutionExtensions.Resolve<Rhetos.Deployment.IInstalledPackages>(container);
            foreach (var package in installedPackages.Packages)
                htmlPackage += "        <tr><td>" + Server.HtmlEncode(package.Id) + "</td><td>" + Server.HtmlEncode(package.Version) + "</td></tr>\r\n";
            ViewBag.Package = htmlPackage;
            
            return View();
        }
    }
}