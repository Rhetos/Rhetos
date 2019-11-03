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
        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly IUserInfo _userInfo;
        private readonly IPluginsContainer<IClaimProvider> _claimProviders;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly bool _allowBuiltinAdminOverride;
        private readonly HashSet<string> _allClaimsForUsers; // Case-insensitive hashset.
        private readonly IAuthorizationProvider _authorizationProvider;
        private readonly ILocalizer _localizer;

        public AuthorizationManager(
            RhetosAppOptions rhetosAppOptions,
            IPluginsContainer<IClaimProvider> claimProviders,
            IUserInfo userInfo,
            ILogProvider logProvider,
            IAuthorizationProvider authorizationProvider,
            IWindowsSecurity windowsSecurity,
            ILocalizer localizer)
        {
            _rhetosAppOptions = rhetosAppOptions;
            _userInfo = userInfo;
            _claimProviders = claimProviders;
            _authorizationProvider = authorizationProvider;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance");
            _allowBuiltinAdminOverride = _rhetosAppOptions.BuiltinAdminOverride;
            _allClaimsForUsers = FromConfigAllClaimsForUsers();
            _localizer = localizer;
        }

        private HashSet<string> FromConfigAllClaimsForUsers()
        {
            try
            {
                var setting = _rhetosAppOptions.Security__AllClaimsForUsers;
                var users = setting.Split(',').Select(u => u.Trim()).Where(u => !string.IsNullOrEmpty(u))
                    .Select(u => u.Split('@'))
                    .Select(u => new { UserName = u[0], HostName = u[1] })
                    .ToList();
                var thisMachineUserNames = users
                    .Where(u => string.Equals(u.HostName, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                    .Select(u => u.UserName)
                    .Distinct();

                return new HashSet<string>(thisMachineUserNames, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                throw new FrameworkException($"Invalid '{nameof(RhetosAppOptions.Security__AllClaimsForUsers)}' parameter format in web.config. Expected comma-separated list of entries formatted as username@servername.", ex);
            }
        }

        public IList<bool> GetAuthorizations(IList<Claim> requiredClaims)
        {
            var sw = Stopwatch.StartNew();

            if (AssumeAllClaims())
            {
                _logger.Trace(() => $"User {ReportUserNameOrAnonymous(_userInfo)} has built-in administrator privileges.");
                return Enumerable.Repeat(true, requiredClaims.Count()).ToArray();
            }

            var authorizations = _authorizationProvider.GetAuthorizations(_userInfo, requiredClaims);
            _performanceLogger.Write(sw, "AuthorizationManager.GetAuthorizations");
            return authorizations;
        }

        private bool AssumeAllClaims()
        {
            return _userInfo.IsUserRecognized
                &&
                (
                    _allClaimsForUsers.Contains(_userInfo.UserName)
                    ||
                    _allowBuiltinAdminOverride
                        && _userInfo is IUserInfoAdmin
                        && ((IUserInfoAdmin)_userInfo).IsBuiltInAdministrator
                );
        }

        public string Authorize(IList<ICommandInfo> commandInfos)
        {
            var sw = Stopwatch.StartNew();

            IList<Claim> requiredClaims = GetRequiredClaims(commandInfos);
            _performanceLogger.Write(sw, "AuthorizationManager.Authorize requiredClaims");

            var claimsAuthorization = requiredClaims.Zip(GetAuthorizations(requiredClaims), (claim, authorized) => new { claim, authorized });
            var unauthorized = claimsAuthorization.FirstOrDefault(ca => !ca.authorized);
            _performanceLogger.Write(sw, "AuthorizationManager.Authorize unauthorizedClaim");

            if (unauthorized != null)
            {
                _logger.Trace(() => $"User {ReportUserNameOrAnonymous(_userInfo)} does not have claim {unauthorized.claim.FullName}.");

                // If user is unauthenticated, assume that the cause is not lack of permissions, but missing authentication.
                // Throwing HttpStatusCode.Unauthorized to direct the user to the standard login process.
                if (!_userInfo.IsUserRecognized)
                    throw new ClientException("User is not authenticated.") { HttpStatusCode = HttpStatusCode.Unauthorized };

                return _localizer["You are not authorized for action '{0}' on resource '{1}', user '{2}'.",
                    unauthorized.claim.Right, unauthorized.claim.Resource, ReportUserNameOrAnonymous(_userInfo)];
            }

            return null;
        }

        private IList<Claim> GetRequiredClaims(IEnumerable<ICommandInfo> commandInfos)
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
    }
}
