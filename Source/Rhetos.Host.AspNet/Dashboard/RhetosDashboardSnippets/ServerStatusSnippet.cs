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

using Microsoft.AspNetCore.Http;
using Rhetos.Utilities;
using System;

namespace Rhetos.Host.AspNet.Dashboard.RhetosDashboardSnippets
{
    public class ServerStatusSnippet : IDashboardSnippet
    {
        public string DisplayName => "Server Status";
        public int Order => 100;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserInfo _userInfo;
    
        public ServerStatusSnippet(IHttpContextAccessor httpContextAccessor, IUserInfo userInfo)
        {
            _httpContextAccessor = httpContextAccessor;
            _userInfo = userInfo;
        }

        public string RenderHtml()
        {
            var rendered = string.Format(_html,
                DateTime.Now,
                System.Diagnostics.Process.GetCurrentProcess().StartTime,
                Environment.Is64BitProcess,
                _httpContextAccessor.HttpContext?.User?.Identity?.AuthenticationType,
                _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                SafeReportUserInfo()
            );
            return rendered;
        }

        private string SafeReportUserInfo()
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

        private static readonly string _html =
            @"
<table>
	<thead></thead>
	<tbody>
	<tr>
		<td>Local server time:</td>
		<td>{0}</td>
	</tr>
	<tr>
		<td>Process start time:</td>
		<td>{1}</td>
	</tr>
	<tr>
		<td>Is 64-bit process:</td>
		<td>{2}</td>
	</tr>
	<tr>
		<td>User authentication type:</td>
		<td>{3}</td>
	</tr>
	<tr>
		<td>User identity:</td>
		<td>{4}</td>
	</tr>
	<tr>
		<td>User info:</td>
		<td>{5}</td>
	</tr>
	</tbody>
</table>
";
    }
}
