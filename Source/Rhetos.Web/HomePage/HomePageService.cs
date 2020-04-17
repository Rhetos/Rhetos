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
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Web;

namespace Rhetos.HomePage
{
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class HomePageService
    {
        private readonly IUserInfo _userInfo;
        private readonly InstalledPackages _installedPackages;
        private readonly IPluginsContainer<IHomePageSnippet> _snippets;

        public HomePageService(IUserInfo userInfo, InstalledPackages installedPackages, IPluginsContainer<IHomePageSnippet> snippets)
        {
            _userInfo = userInfo;
            _installedPackages = installedPackages;
            _snippets = snippets;
        }

        [OperationContract]
        [WebGet(UriTemplate = "", BodyStyle = WebMessageBodyStyle.Bare)]
        public Stream PageLoad()
        {
            string html =
$@"<html>
<head>
    <title>Rhetos</title>
</head>
<body>
    <h1>Rhetos</h1>
    <div>
{GetHomePageSnippets()}    </div>
    <h2>Installed packages</h2>
    <table><tbody>
{GetInstalledPackages()}    </tbody></table>
    <h2>Server status</h2>
    <p>
        Local server time: {DateTime.Now}<br />
        Process start time: {System.Diagnostics.Process.GetCurrentProcess().StartTime}<br />
        Is 64-bit process: {Environment.Is64BitProcess}<br />
        User authentication type: {HttpContext.Current.User?.Identity?.AuthenticationType}<br />
        User identity: {HttpContext.Current?.User?.Identity?.Name}<br />
        User info: {SafeReportUserInfo()}<br />
    </p>
</body>
</html>";
            byte[] resultBytes = Encoding.UTF8.GetBytes(html);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        public string SafeReportUserInfo()
        {
            try
            {
                return _userInfo?.Report();
            }
            catch (Exception e)
            {
                return e.GetType().FullName;
            }
        }

        private string GetHomePageSnippets()
        {
            var html = new StringBuilder();
            foreach (var snippet in _snippets.GetPlugins())
                html.AppendLine(snippet.Html);
            return html.ToString();
        }

        private string GetInstalledPackages()
        {
            var html = new StringBuilder();
            foreach (var package in _installedPackages.Packages)
                html.AppendLine("        <tr><td>" + HttpContext.Current?.Server?.HtmlEncode(package.Id) + "</td><td>" + HttpContext.Current?.Server?.HtmlEncode(package.Version) + "</td></tr>");
            return html.ToString();
        }
    }
}