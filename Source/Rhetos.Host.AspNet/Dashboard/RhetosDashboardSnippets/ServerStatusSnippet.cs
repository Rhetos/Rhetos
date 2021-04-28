using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rhetos.Utilities;

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
