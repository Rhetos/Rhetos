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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Security
{
    public class AuthorizationManager : IAuthorizationManager
    {
        private readonly IUserInfo _userInfo;
        private readonly IPluginsContainer<IClaimProvider> _claimProviders;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly bool _allowBuiltinAdminOverride;
        private readonly IAuthorizationProvider _authorizationProvider;
        private readonly IWindowsSecurity _windowsSecurity;
        private readonly ILocalizer _localizer;

        public AuthorizationManager(
            IPluginsContainer<IClaimProvider> claimProviders,
            IUserInfo userInfo,
            ILogProvider logProvider,
            IAuthorizationProvider authorizationProvider,
            IWindowsSecurity windowsSecurity,
            ILocalizer localizer)
        {
            _userInfo = userInfo;
            _claimProviders = claimProviders;
            _authorizationProvider = authorizationProvider;
            _windowsSecurity = windowsSecurity;
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance");
            _allowBuiltinAdminOverride = FromConfigAllowBuiltinAdminOverride();
            _localizer = localizer;
        }

        private static bool FromConfigAllowBuiltinAdminOverride()
        {
            var setting = ConfigUtility.GetAppSetting("BuiltinAdminOverride");
            if (setting != null)
            {
                bool allow;
                if (bool.TryParse(setting, out allow))
                    return allow;
                
                throw new FrameworkException("Invalid setting of BuiltinAdminOverride in configuration file. Allowed values are True and False.");
            }
            return false;
        }

        public IList<bool> GetAuthorizations(IList<Claim> requiredClaims)
        {
            var sw = Stopwatch.StartNew();

            if (_allowBuiltinAdminOverride
                && _userInfo is IWindowsUserInfo
                && _windowsSecurity.IsBuiltInAdministrator((IWindowsUserInfo)_userInfo))
            {
                _logger.Trace(() => string.Format("User {0} has builtin administrator privileges.", _userInfo.UserName));
                return Enumerable.Repeat(true, requiredClaims.Count()).ToArray();
            }

            var authorizations = _authorizationProvider.GetAuthorizations(_userInfo, requiredClaims);
            _performanceLogger.Write(sw, "AuthorizationManager.GetAuthorizations");
            return authorizations;
        }

        public string Authorize(IList<ICommandInfo> commandInfos)
        {
            var sw = Stopwatch.StartNew();

            IList<Claim> requiredClaims = GetRequiredClaims(commandInfos);
            _performanceLogger.Write(sw, "AuthorizationManager.Authorize requiredClaims");

            var claimsAuthorization = requiredClaims.Zip(GetAuthorizations(requiredClaims), (claim, authorized) => new { claim, authorized });
            var unauthorized = claimsAuthorization.FirstOrDefault(ca => ca.authorized == false);
            _performanceLogger.Write(sw, "AuthorizationManager.Authorize unauthorizedClaim");

            if (unauthorized != null)
            {
                _logger.Trace(() => string.Format("User {0} does not posses claim {1}.", _userInfo.UserName, unauthorized.claim.FullName));

                return _localizer["You are not authorized for action '{0}' on resource '{1}', user '{2}'.",
                    unauthorized.claim.Right, unauthorized.claim.Resource, _userInfo.UserName];
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
    }
}
