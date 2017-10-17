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

namespace Rhetos.Dom.DefaultConcepts
{
    public class CommonAuthorizationProvider : IAuthorizationProvider
    {
        private readonly ILogger _logger;
        private readonly IAuthorizationData _authorizationData;

        public CommonAuthorizationProvider(ILogProvider logProvider, IAuthorizationData authorizationData)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _authorizationData = authorizationData;
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            PrincipalInfo principal = _authorizationData.GetPrincipal(userInfo.UserName);
            IEnumerable<Guid> userRoles = GetUsersRoles(principal);

            IEnumerable<ClaimInfo> claims = GetClaims(requiredClaims);
            IEnumerable<Permission> userPermissions = GetUsersPermissions(claims, principal, userRoles);
            IList<bool> userHasClaims = GetUserHasClaims(claims, userPermissions);

            var roleNamesIndex = new Lazy<IDictionary<Guid, string>>(() => _authorizationData.GetRoles(userRoles));
            _logger.Trace(() => ReportRoles(userInfo, userRoles, roleNamesIndex));
            _logger.Trace(() => ReportPermissions(userInfo, principal, roleNamesIndex, userPermissions, claims, userHasClaims));

            return userHasClaims;
        }

        /// <summary>Return direct and indirect user's roles.</summary>
        public IEnumerable<Guid> GetUsersRoles(IPrincipal principal)
        {
            IEnumerable<Guid> directUserRoles = _authorizationData.GetPrincipalRoles(principal);
            IEnumerable<Guid> allUserRoles = Graph.IncludeDependents(directUserRoles, _authorizationData.GetRoleRoles);
            return allUserRoles;
        }

        /// <summary>Inactive or nonexistent claims will have ID set to null.</summary>
        private IEnumerable<ClaimInfo> GetClaims(IEnumerable<Claim> requiredClaims)
        {
            var claimsIndex = _authorizationData.GetClaims(requiredClaims);
            return requiredClaims
                .Select(claim =>
                {
                    ClaimInfo claimInfo;
                    if (claimsIndex.TryGetValue(claim, out claimInfo))
                        return claimInfo;
                    return new ClaimInfo { ID = null, Resource = claim.Resource, Right = claim.Right };
                })
                .ToList();
        }

        private class Permission
        {
            public Guid? PrincipalID { get; set; }
            public Guid? RoleID { get; set; }
            public Guid ClaimID { get; set; }
            public bool IsAuthorized { get; set; }
        }

        private IEnumerable<Permission> GetUsersPermissions(IEnumerable<ClaimInfo> claims, IPrincipal principal, IEnumerable<Guid> userRoles)
        {
            var claimIdsIndex = new HashSet<Guid>(claims.Where(claim => claim.ID != null).Select(claim => claim.ID.Value));

            var principalPermissions = _authorizationData.GetPrincipalPermissions(principal, claimIdsIndex)
                .Where(pp => claimIdsIndex.Contains(pp.ClaimID))
                .Select(pp => new Permission { PrincipalID = pp.PrincipalID, ClaimID = pp.ClaimID, IsAuthorized = pp.IsAuthorized });

            var rolePermissions = _authorizationData.GetRolePermissions(userRoles, claimIdsIndex)
                .Where(rp => claimIdsIndex.Contains(rp.ClaimID))
                .Select(rp => new Permission { RoleID = rp.RoleID, ClaimID = rp.ClaimID, IsAuthorized = rp.IsAuthorized });

            // GetPrincipalPermissions and GetRolePermissions may return permissions for more claims than required.
            return principalPermissions.Concat(rolePermissions)
                .ToList();
        }

        private static IList<bool> GetUserHasClaims(IEnumerable<ClaimInfo> claims, IEnumerable<Permission> userPermissions)
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
        private string ReportRoles(IUserInfo userInfo, IEnumerable<Guid> userRoles, Lazy<IDictionary<Guid, string>> roleNamesIndex)
        {
            var roleNames = userRoles.Select(roleId => GetRoleNameSafe(roleId, roleNamesIndex)).ToList();
            roleNames.Sort();
            return string.Format("User {0} has {1} roles: {2}.", userInfo.UserName, roleNames.Count, string.Join(", ", roleNames));
        }

        /// <summary>
        /// Reporting is done in a function that returns a string, to avoid any performance impact when the trace log is not enabled.
        /// </summary>
        private string ReportPermissions(IUserInfo userInfo, PrincipalInfo principal, Lazy<IDictionary<Guid, string>> roleNamesIndex,
            IEnumerable<Permission> userPermissions, IEnumerable<ClaimInfo> claims, IEnumerable<bool> userHasClaims)
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
                                : ("role " + GetRoleNameSafe(permission.RoleID.Value, roleNamesIndex))
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

        /// <summary>Returns role ID instead of the role name, if the role does not exist in the index.</summary>
        private string GetRoleNameSafe(Guid roleId, Lazy<IDictionary<Guid, string>> roleNamesIndex)
        {
            string roleName;
            if (roleNamesIndex.Value.TryGetValue(roleId, out roleName))
                return roleName;
            return roleId.ToString();
        }
    }
}
