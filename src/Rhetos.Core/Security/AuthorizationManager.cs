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

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Processing;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Rhetos.Security
{
    public class AuthorizationManager : IAuthorizationManager
    {
        private readonly IUserInfo _userInfo;
        private readonly IPluginsContainer<IClaimProvider> _claimProviders;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        /// <summary>
        /// Case-insensitive HashSet.
        /// </summary>
        private readonly HashSet<string> _allClaimsForUsers;
        private readonly IAuthorizationProvider _authorizationProvider;
        private readonly ILocalizer _localizer;
        private readonly AppSecurityOptions _appSecurityOptions;

        public AuthorizationManager(
            IPluginsContainer<IClaimProvider> claimProviders,
            IUserInfo userInfo,
            ILogProvider logProvider,
            IAuthorizationProvider authorizationProvider,
            AppSecurityOptions appSecurityOptions,
            ILocalizer localizer)
        {
            _appSecurityOptions = appSecurityOptions;
            _userInfo = userInfo;
            _claimProviders = claimProviders;
            _authorizationProvider = authorizationProvider;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _allClaimsForUsers = FromConfigAllClaimsForUsers();
            _localizer = localizer;
        }

        private HashSet<string> FromConfigAllClaimsForUsers()
        {
            try
            {
                var setting = _appSecurityOptions.AllClaimsForUsers;
                var users = SplitUserList(setting);
                var thisMachineUserNames = users
                    .Where(u => string.Equals(u.HostName, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                    .Select(u => u.UserName)
                    .Distinct();

                return new HashSet<string>(thisMachineUserNames, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                throw new FrameworkException($"Invalid '{OptionsAttribute.GetConfigurationPath<AppSecurityOptions>()}:{nameof(AppSecurityOptions.AllClaimsForUsers)}' in configuration files. Expected comma-separated list of entries formatted as username@servername.", ex);
            }
        }

        private static List<(string UserName, string HostName)> SplitUserList(string users)
        {
            return users.Split(',').Select(u => u.Trim()).Where(u => !string.IsNullOrEmpty(u))
                .SelectMany(SplitUserName)
                .ToList();
        }

        private static IEnumerable<(string UserName, string HostName)> SplitUserName(string u)
        {
            yield return (UserName: u, HostName: Environment.MachineName);
            int at = u.LastIndexOf('@');
            if (at > 0 && at < u.Length - 1)
                yield return (UserName: u[..at], HostName: u[(at + 1)..]);
        }

        public IList<bool> GetAuthorizations(IList<Claim> requiredClaims)
        {
            var sw = Stopwatch.StartNew();

            if (AssumeAllClaims())
            {
                _logger.Trace(() => $"User {ReportUserNameOrAnonymous(_userInfo)} has built-in administrator privileges.");
                return Enumerable.Repeat(true, requiredClaims.Count).ToArray();
            }

            var authorizations = _authorizationProvider.GetAuthorizations(_userInfo, requiredClaims);
            _performanceLogger.Write(sw, "GetAuthorizations");
            return authorizations;
        }

        private bool AssumeAllClaims()
        {
            if (_appSecurityOptions.AllClaimsForAnonymous && _userInfo.IsUserRecognized)
                throw new FrameworkException($"Invalid security configuration settings. Both anonymous access and user-level security should not be active at the same time." +
                    $" Disable '{OptionsAttribute.GetConfigurationPath<AppSecurityOptions>()}:{nameof(AppSecurityOptions.AllClaimsForAnonymous)}' option.");

            return _userInfo.IsUserRecognized
                && _allClaimsForUsers.Contains(_userInfo.UserName)
                || _appSecurityOptions.AllClaimsForAnonymous;
        }

        public string Authorize(IList<ICommandInfo> commandInfos)
        {
            Authorize(commandInfos, out string message, out string[] messageParameters);
            if (message != null)
                return _localizer[message, messageParameters];
            else
                return null;
        }

        public void Authorize(IList<ICommandInfo> commandInfos, out string message, out string[] messageParameters)
        {
            var sw = Stopwatch.StartNew();

            List<Claim> requiredClaims = GetRequiredClaims(commandInfos);
            _performanceLogger.Write(sw, "Authorize requiredClaims");

            var claimsAuthorization = requiredClaims.Zip(GetAuthorizations(requiredClaims), (claim, authorized) => new { claim, authorized });
            var unauthorized = claimsAuthorization.FirstOrDefault(ca => !ca.authorized);
            _performanceLogger.Write(sw, "Authorize unauthorizedClaim");

            if (unauthorized != null)
            {
                _logger.Trace(() => $"User {ReportUserNameOrAnonymous(_userInfo)} does not have claim {unauthorized.claim.FullName}. ({SafeGet(() => _userInfo.Report())})");

                // If user is unauthenticated, assume that the cause is not lack of permissions, but missing authentication.
                // Throwing HttpStatusCode.Unauthorized to direct the user to the standard login process.
                if (!_userInfo.IsUserRecognized)
                    throw new ClientException("User is not authenticated.") { HttpStatusCode = HttpStatusCode.Unauthorized };

                message = "You are not authorized for action '{0}' on resource '{1}', user '{2}'.";
                messageParameters = new[] { unauthorized.claim.Right, unauthorized.claim.Resource, ReportUserNameOrAnonymous(_userInfo) };
            }
            else
            {
                message = null;
                messageParameters = null;
            }
        }

        private List<Claim> GetRequiredClaims(IEnumerable<ICommandInfo> commandInfos)
        {
            List<Claim> requiredClaims = new List<Claim>();
            foreach (ICommandInfo commandInfo in commandInfos)
            {
                var providers = _claimProviders.GetImplementations(commandInfo.GetType());
                foreach (var provider in providers)
                    requiredClaims.AddRange(provider.GetRequiredClaims(commandInfo));
            }
            return requiredClaims;
        }

        private static string ReportUserNameOrAnonymous(IUserInfo userInfo) => userInfo.IsUserRecognized ? userInfo.UserName : "<anonymous>";

        private static string SafeGet(Func<string> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
