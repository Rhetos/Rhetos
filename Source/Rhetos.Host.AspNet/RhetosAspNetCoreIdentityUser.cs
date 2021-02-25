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

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rhetos.Utilities;

namespace Rhetos.Host.AspNet
{
    public class RhetosAspNetCoreIdentityUser : IUserInfo
    {
        public bool IsUserRecognized => !string.IsNullOrEmpty(_userName.Value);

        /// <remarks>
        /// The exception prevents custom code to accidentally use an unauthenticated username
        /// as an empty string in authorization logic, database queries and similar features.
        /// <see cref="IsUserRecognized"/> should be check first where unauthenticated users
        /// are supported.
        /// </remarks>
        public string UserName => IsUserRecognized ? _userName.Value : throw new ClientException("User is not authenticated.");

        public string Workstation => _workstation.Value;

        private readonly Lazy<string> _userName;
        private readonly Lazy<string> _workstation;

        public RhetosAspNetCoreIdentityUser(IHttpContextAccessor httpContextAccessor)
        {
            _workstation = new Lazy<string>(() => GetWorkstation(httpContextAccessor.HttpContext), false);
            _userName = new Lazy<string>(() => GetUserName(httpContextAccessor.HttpContext), false);
        }

        private static string GetUserName(HttpContext httpContext)
        {
            var identity = httpContext?.User?.Identity;
            if (identity?.IsAuthenticated == true)
                return identity.Name;
            else
                return null;
        }

        private static string GetWorkstation(HttpContext httpContext)
        {
            var ipAddress = httpContext?.Connection?.RemoteIpAddress;
            if (ipAddress != null)
                return ipAddress.ToString() + " port " + httpContext.Connection.RemotePort;
            else
                return null;
        }

        public string Report() => ReportUserNameOrAnonymous() + "," + _workstation.Value;

        private string ReportUserNameOrAnonymous() => IsUserRecognized ? _userName.Value : "<anonymous>";
    }
}
