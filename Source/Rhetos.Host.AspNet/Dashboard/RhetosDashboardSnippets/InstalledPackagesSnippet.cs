/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Deployment;
using Rhetos.Security;
using System.Linq;
using System.Text;

namespace Rhetos.Host.AspNet.Dashboard.RhetosDashboardSnippets
{
    public class InstalledPackagesSnippet : IDashboardSnippet
    {
        public string DisplayName => "Installed Packages";

        public int Order => 1000;

        public static readonly Claim ShowInstalledPackagesClaim = new Claim("Dashboard.InstalledPackages", "Show");

        private readonly IRhetosComponent<InstalledPackages> _installedPackages;
        private readonly IRhetosComponent<IAuthorizationManager> _authorizationManager;

        public InstalledPackagesSnippet(IRhetosComponent<InstalledPackages> installedPackages, IRhetosComponent<IAuthorizationManager> authorizationManager)
        {
            _installedPackages = installedPackages;
            _authorizationManager = authorizationManager;
        }

        public string RenderHtml()
        {
            bool isUserAllowed = _authorizationManager.Value.GetAuthorizations(new[] { ShowInstalledPackagesClaim }).Single();

            if (isUserAllowed)
            {
                var stringBuilder = new StringBuilder();
                foreach (var package in _installedPackages.Value.Packages.OrderBy(p => p.Id))
                {
                    stringBuilder.AppendLine($"        <tr><td>{package.Id}</td><td style=\"text-align: right\">{package.Version}</td></tr>");
                }

                var rendered = string.Format(_html, stringBuilder);
                return rendered;
            }
            else
                return $"<p>You are not authorized for action '{ShowInstalledPackagesClaim.Right}' on resource '{ShowInstalledPackagesClaim.Resource}'.</p>";
        }

        private const string _html =
@"    <table>
      <thead>
      </thead>
      <tbody>
{0}      </tbody>
    </table>
";
    }
}
