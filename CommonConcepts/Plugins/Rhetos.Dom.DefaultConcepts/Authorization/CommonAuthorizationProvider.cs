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

using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts.Authorization
{
    public class CommonAuthorizationProvider : IAuthorizationProvider
    {
        private readonly ILogger _logger;
        private readonly AuthorizationDataReader _authorizationData;

        public CommonAuthorizationProvider(ILogProvider logProvider, AuthorizationDataReader authorizationData)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _authorizationData = authorizationData;
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            PrincipalInfo principal = _authorizationData.GetPrincipal(userInfo.UserName);
            IList<Guid> userRoles = GetUsersRoles(principal);

            IList<ClaimInfo> claims = _authorizationData.GetClaims(requiredClaims);
            IList<Permission> userPermissions = GetUsersPermissions(claims, principal, userRoles);
            IList<bool> userHasClaims = GetUsersClaims(claims, userPermissions);

            var roleNamesIndex = new Lazy<Dictionary<Guid, string>>(() => GetRoleNamesIndex(userRoles));
            _logger.Trace(() => ReportRoles(userInfo, roleNamesIndex));
            _logger.Trace(() => ReportPermissions(userInfo, principal, roleNamesIndex, userPermissions, claims, userHasClaims));

            return userHasClaims;
        }

        /// <summary>Return direct and indirect user's roles.</summary>
        public IList<Guid> GetUsersRoles(IPrincipal principal)
        {
            IList<Guid> directUserRoles = _authorizationData.GetPrincipalRoles(principal);
            IList<Guid> allUserRoles = Graph.IncludeDependents(directUserRoles, _authorizationData.GetRoleRoles);
            return allUserRoles;
        }

        private class Permission
        {
            public Guid? PrincipalID { get; set; }
            public Guid? RoleID { get; set; }
            public Guid ClaimID { get; set; }
            public bool IsAuthorized { get; set; }
        }

        private IList<Permission> GetUsersPermissions(IList<ClaimInfo> claims, IPrincipal principal, IList<Guid> userRoles)
        {
            var claimIds = claims.Where(claim => claim.ID != null).Select(claim => claim.ID.Value).ToList();

            var principalPermissions = _authorizationData.GetPrincipalPermissions(principal, claimIds)
                .Select(pp => new Permission { PrincipalID = pp.PrincipalID, ClaimID = pp.ClaimID, IsAuthorized = pp.IsAuthorized });
            var rolePermissions = _authorizationData.GetRolePermissions(userRoles, claimIds)
                .Select(rp => new Permission { RoleID = rp.RoleID, ClaimID = rp.ClaimID, IsAuthorized = rp.IsAuthorized });

            return principalPermissions.Concat(rolePermissions).ToList();
        }

        private static IList<bool> GetUsersClaims(IList<ClaimInfo> claims, IList<Permission> userPermissions)
        {
            var userClaims = new HashSet<Guid>();

            foreach (var permission in userPermissions)
                if (permission.IsAuthorized)
                    userClaims.Add(permission.ClaimID);

            foreach (var permission in userPermissions)
                if (!permission.IsAuthorized)
                    userClaims.Remove(permission.ClaimID);

            return claims.Select(claim => claim.ID != null && userClaims.Contains(claim.ID.Value)).ToList();
        }

        /// <summary>
        /// Reporting is done in a function that returns a string, to avoid any performance impact when the trace log is not enabled.
        /// </summary>
        private string ReportRoles(IUserInfo userInfo, Lazy<Dictionary<Guid, string>> roleNamesIndex)
        {
            var roleNames = roleNamesIndex.Value.Values.ToList();
            roleNames.Sort();
            return string.Format("User {0} has {1} roles: {2}.", userInfo.UserName, roleNames.Count(), string.Join(", ", roleNames));
        }

        /// <summary>
        /// Reporting is done in a function that returns a string, to avoid any performance impact when the trace log is not enabled.
        /// </summary>
        private string ReportPermissions(IUserInfo userInfo, PrincipalInfo principal, Lazy<Dictionary<Guid, string>> roleNamesIndex,
            IList<Permission> userPermissions, IList<ClaimInfo> claims, IList<bool> userHasClaims)
        {
            var report = new List<string>();

            // Create an index of permissions:

            var permissionsByClaim = new MultiDictionary<Guid, Permission>();
            foreach (var permission in userPermissions)
                permissionsByClaim.Add(permission.ClaimID, permission);

            // Analyze permissions for required claims:

            foreach (var claimResult in claims.Zip(userHasClaims, (Claim, UserHasIt) => new { Claim, UserHasIt }))
            {
                var claimPermissions = claimResult.Claim.ID != null
                    ? permissionsByClaim.Get(claimResult.Claim.ID.Value)
                    : new Permission[] { };

                var permissionsDescription = claimPermissions
                    .Select(permission => new
                        {
                            permission.IsAuthorized,
                            PrincipalOrRoleName = permission.PrincipalID != null
                                ? ("principal " + (permission.PrincipalID.Value == principal.ID ? principal.Name : permission.PrincipalID.Value.ToString()))
                                : ("role " + roleNamesIndex.Value[permission.RoleID.Value])
                        })
                    .ToList();

                var allowedFor = permissionsDescription.Where(p => p.IsAuthorized).Select(p => p.PrincipalOrRoleName).ToList();
                var deniedFor = permissionsDescription.Where(p => !p.IsAuthorized).Select(p => p.PrincipalOrRoleName).ToList();

                string explanation = "User " + userInfo.UserName + " " + (claimResult.UserHasIt ? "has" : "doesn't have")
                    + " claim '" + claimResult.Claim.Resource + " " + claimResult.Claim.Right + "'. It is ";

                if (deniedFor.Count != 0)
                    if (allowedFor.Count != 0)
                        explanation += "denied for " + string.Join(", ", deniedFor) + " and allowed for " + string.Join(", ", allowedFor) + " (deny overrides allow).";
                    else
                        explanation += "denied for " + string.Join(", ", deniedFor) + ".";
                else
                    if (allowedFor.Count != 0)
                        explanation += "allowed for " + string.Join(", ", allowedFor) + ".";
                    else
                        if (claimResult.Claim.ID == null)
                            explanation += "denied by default (the claim does not exist or is no longer active).";
                        else
                            explanation += "denied by default (no permissions defined).";

                report.Add(explanation);
            }

            return string.Join("\r\n", report);
        }

        /// <summary>
        /// The index returns role ID instead of role Name for roles that no longer exist, to achieve more robust implementaiton.
        /// </summary>
        private Dictionary<Guid, string> GetRoleNamesIndex(IList<Guid> userRoles)
        {
            return _authorizationData.GetRoles(userRoles)
                .ToDictionary(role => role.ID, role => role.Name ?? role.ID.ToString());
        }
    }
}
