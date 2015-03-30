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

using Rhetos.Dom;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Security;
using System.Diagnostics;
using Rhetos.Logging;
using Rhetos.Extensibility;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.SimpleWindowsAuth
{
    public class SimpleWindowsAuthorizationProvider : IAuthorizationProvider
    {
        private readonly Lazy<IPermissionLoader> _permissionLoader;
        private readonly Lazy<IQueryableRepository<IRole>> _roleRepository;
        private readonly IWindowsSecurity _windowsSecurity;
        private readonly ILogger _logger;

        public SimpleWindowsAuthorizationProvider(
            Lazy<IPermissionLoader> permissionLoader,
            Lazy<IQueryableRepository<IRole>> roleRepository,
            IWindowsSecurity windowsSecurity,
            ILogProvider logProvider)
        {
            _permissionLoader = permissionLoader;
            _roleRepository = roleRepository;
            _windowsSecurity = windowsSecurity;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            if (!(userInfo is IWindowsUserInfo))
                throw new FrameworkException("Unexpected userInfo type '" + userInfo.GetType().FullName + "'.");
            if (_roleRepository.Value.Query().Take(1).Select(role => role.ID).ToList().Count > 0)
                throw new FrameworkException("SimpleWindowsAuth does not support roles. Please delete roles from Common.Role or use a different security package.");

            var userMembership = (IList<string>)_windowsSecurity.GetIdentityMembership(userInfo.UserName);
            var userPermissions = _permissionLoader.Value.LoadPermissions(requiredClaims, userMembership);

            _logger.Trace(() => "User " + userInfo.UserName + " has roles: " + string.Join(", ", userMembership) + ".");
            _logger.Trace(() => ReportPermissions(userInfo, userPermissions, requiredClaims));

            HashSet<string> hasClaims = new HashSet<string>();
            foreach (IPermissionBrowse permission in userPermissions)
                if (permission.IsAuthorized.Value)
                    hasClaims.Add(permission.ClaimResource + "." + permission.ClaimRight);
            foreach (IPermissionBrowse permission in userPermissions)
                if (!permission.IsAuthorized.Value)
                    hasClaims.Remove(permission.ClaimResource + "." + permission.ClaimRight);

            return requiredClaims.Select(requiredClaim => hasClaims.Contains(requiredClaim.FullName)).ToArray();
        }

        private string ReportPermissions(IUserInfo userInfo, IList<IPermissionBrowse> userPermissions, IList<Claim> requiredClaims)
        {
            // Reporting is done in a function that returns string, to avoid any performance impact when the trace log is not enabled.

            var report = new List<string>();

            var permissionsByClaim = new MultiDictionary<string, IPermissionBrowse>();
            foreach (var permission in userPermissions)
                permissionsByClaim.Add(permission.ClaimResource + "." + permission.ClaimRight, permission);

            foreach (var claim in requiredClaims)
            {
                string claimName = claim.FullName;
                var claimPermissions = permissionsByClaim.Get(claimName);
                var allowedForRoles = claimPermissions.Where(p => p.IsAuthorized.Value).Select(p => p.Principal).ToList();
                var deniedForRoles = claimPermissions.Where(p => !p.IsAuthorized.Value).Select(p => p.Principal).ToList();

                if (deniedForRoles.Count != 0)
                    if (allowedForRoles.Count != 0)
                        report.Add("User " + userInfo.UserName + " claim '" + claimName + "' is denied for role " + string.Join(", ", deniedForRoles) + " and allowed for role " + string.Join(", ", allowedForRoles) + " (deny overrides allow).");
                    else
                        report.Add("User " + userInfo.UserName + " claim '" + claimName + "' is denied for role " + string.Join(", ", deniedForRoles) + ".");
                else
                    if (allowedForRoles.Count != 0)
                        report.Add("User " + userInfo.UserName + " claim '" + claimName + "' is allowed for role " + string.Join(", ", allowedForRoles) + ".");
                    else
                        report.Add("User " + userInfo.UserName + " claim '" + claimName + "' is denied by default (no permissions defined).");
            }

            return string.Join("\r\n", report);
        }
    }
}
