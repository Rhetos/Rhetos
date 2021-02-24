using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rhetos.Utilities;

namespace Rhetos.Extensions.AspNetCore
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
