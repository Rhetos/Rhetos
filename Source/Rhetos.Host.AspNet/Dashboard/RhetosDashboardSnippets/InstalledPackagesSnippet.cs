using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Rhetos.Deployment;

namespace Rhetos.Host.AspNet.Dashboard.RhetosDashboardSnippets
{
    public class InstalledPackagesSnippet : IDashboardSnippet
    {
        public string DisplayName => "Installed Packages";
        public int Order => 200;

        private readonly IRhetosComponent<InstalledPackages> _installedPackages;

        public InstalledPackagesSnippet(IRhetosComponent<InstalledPackages> installedPackages)
        {
            _installedPackages = installedPackages;
        }

        public string RenderHtml()
        {
            var stringBuilder = new StringBuilder();
            foreach (var package in _installedPackages.Value.Packages)
            {
                stringBuilder.Append($"<tr>\n<td>{package.Id}</td>\n<td style=\"text-align: right\">{package.Version}</td>\n</tr>");
            }

            var rendered = string.Format(_html, stringBuilder);
            return rendered;
        }

        private static readonly string _html =
@"
<table>
	<thead>
	</thead>
	<tbody>
{0}
    </tbody>
    </table>
";
    }
}
