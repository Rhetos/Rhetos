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
        public bool IsUserRecognized => !string.IsNullOrEmpty(UserName);
        public string UserName => userNameValueGenerator.Value;
        public string Workstation => workstationValueGenerator.Value;

        private readonly Lazy<string> userNameValueGenerator;
        private readonly Lazy<string> workstationValueGenerator;

        public RhetosAspNetCoreIdentityUser(IHttpContextAccessor httpContextAccessor)
        {
            workstationValueGenerator = new Lazy<string>(() => GetWorkstation(httpContextAccessor.HttpContext));
            userNameValueGenerator = new Lazy<string>(() => GetUserName(httpContextAccessor.HttpContext?.User));
        }

        private string GetUserName(ClaimsPrincipal httpContextUser)
        {
            var userNameFromContext = httpContextUser?.Identity?.Name;
            if (string.IsNullOrEmpty(userNameFromContext))
                throw new InvalidOperationException($"No username found while trying to resolve user from HttpContext.");

            return userNameFromContext;
        }

        private string GetWorkstation(HttpContext httpContext)
        {
            return httpContext.Connection?.RemoteIpAddress?.ToString();
        }

        public string Report()
        {
            return $"{nameof(RhetosAspNetCoreIdentityUser)}(UserName='{UserName}')";
        }
    }
}
