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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Security;
using Rhetos.Logging;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IAuthorizationProvider))]
    public class CommonAuthorizationProvider : IAuthorizationProvider
    {
        private readonly Lazy<IQueryableRepository<IPrincipal>> _principalRepository;
        private readonly Lazy<IQueryableRepository<IPrincipalHasRole>> _principalRolesRepository;
        private readonly Lazy<IQueryableRepository<IRoleInheritsRole>> _roleRolesRepository;
        private readonly Lazy<IQueryableRepository<IRolePermission>> _rolePermissionRepository;
        private readonly Lazy<IQueryableRepository<IPrincipalPermission>> _principalPermissionRepository;
        private readonly Lazy<IQueryableRepository<IRole>> _roleRepository;
        private readonly Lazy<IQueryableRepository<ICommonClaim>> _claimRepository;
        private readonly ILogger _logger;

        public CommonAuthorizationProvider(
            Lazy<IQueryableRepository<IPrincipal>> principalRepository,
            Lazy<IQueryableRepository<IPrincipalHasRole>> principalRolesRepository,
            Lazy<IQueryableRepository<IRoleInheritsRole>> roleRolesRepository,
            Lazy<IQueryableRepository<IRolePermission>> rolePermissionRepository,
            Lazy<IQueryableRepository<IPrincipalPermission>> principalPermissionRepository,
            Lazy<IQueryableRepository<IRole>> roleRepository,
            Lazy<IQueryableRepository<ICommonClaim>> claimRepository,
            ILogProvider logProvider)
        {
            _principalRepository = principalRepository;
            _principalRolesRepository = principalRolesRepository;
            _roleRolesRepository = roleRolesRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _principalPermissionRepository = principalPermissionRepository;
            _roleRepository = roleRepository;
            _claimRepository = claimRepository;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            IList<Guid> userAllRoles = GetUsersRoles(userInfo.UserName);
            IList<PermissionValue> userPermissions = GetUsersPermissions(requiredClaims, userInfo.UserName, userAllRoles);
            IList<bool> userHasClaims = GetUsersClaims(requiredClaims, userPermissions);

            _logger.Trace(() => ReportRoles(userInfo, userAllRoles));
            _logger.Trace(() => ReportPermissions(userInfo, userPermissions, requiredClaims, userHasClaims));

            return userHasClaims;
        }

        private class PermissionValue
        {
            public Claim Claim;
            public bool IsAuthorized;
            public Guid? RoleID;
            public Guid? PrincipalID;
        }

        /// <summary>Return direct and indirect user's roles.</summary>
        public IList<Guid> GetUsersRoles(string userName)
        {
            IList<Guid> userDirectRoles = _principalRolesRepository.Value.Query().Where(pr => pr.Principal.Name == userName).Select(pr => pr.Role.ID).ToList();
            if (userDirectRoles.Count() == 0)
                ValidateUser(userName);
            IList<Tuple<Guid, Guid>> roleInheritsRole = _roleRolesRepository.Value.Query().Select(rr => Tuple.Create(rr.Derived.ID, rr.InheritsFrom.ID)).ToList();
            IList<Guid> userAllRoles = Graph.IncludeDependents(userDirectRoles, roleInheritsRole);
            return userAllRoles;
        }

        private void ValidateUser(string userName)
        {
            Guid userId = _principalRepository.Value.Query().Where(p => p.Name == userName).Select(p => p.ID).SingleOrDefault();
            if (userId == default(Guid))
            {
                _logger.Error("There is no principal with the given username '" + userName + "' in Common.Principal.");
                throw new ClientException("There is no principal with the given username.");
            }
        }

        private IList<PermissionValue> GetUsersPermissions(IList<Claim> requiredClaims, string principalName, IList<Guid> userAllRoles)
        {
            var claimsResources = requiredClaims.Select(claim => claim.Resource).ToList();
            var claimsRights = requiredClaims.Select(claim => claim.Right).ToList();

            var principalPermissions = _principalPermissionRepository.Value.Query()
                .Where(principalPermission =>
                    principalPermission.Principal.Name == principalName
                    && claimsResources.Contains(principalPermission.Claim.ClaimResource) // Loading more claims than necessary, but using some of the SQL indexes on Claim table.
                    && claimsRights.Contains(principalPermission.Claim.ClaimRight)
                    && principalPermission.Claim.Active == true
                    && principalPermission.IsAuthorized != null)
                .Select(principalPermission => new PermissionValue
                {
                    Claim = new Claim(principalPermission.Claim.ClaimResource, principalPermission.Claim.ClaimRight),
                    IsAuthorized = principalPermission.IsAuthorized.Value,
                    PrincipalID = principalPermission.Principal.ID
                })
                .ToList();

            var rolePermissions = _rolePermissionRepository.Value.Query()
                .Where(rolePermission =>
                    userAllRoles.Contains(rolePermission.Role.ID)
                    && claimsResources.Contains(rolePermission.Claim.ClaimResource) // Loading more claims than necessary, but using some of the SQL indexes on Claim table.
                    && claimsRights.Contains(rolePermission.Claim.ClaimRight)
                    && rolePermission.Claim.Active == true
                    && rolePermission.IsAuthorized != null)
                .Select(rolePermission => new PermissionValue
                    {
                        Claim = new Claim(rolePermission.Claim.ClaimResource, rolePermission.Claim.ClaimRight),
                        IsAuthorized = rolePermission.IsAuthorized.Value,
                        RoleID = rolePermission.Role.ID
                    })
                .ToList();

            return principalPermissions.Concat(rolePermissions).ToList();
        }

        private static IList<bool> GetUsersClaims(IList<Claim> requiredClaims, IList<PermissionValue> userPermissions)
        {
            var userClaims = new HashSet<Claim>();

            foreach (var permission in userPermissions)
                if (permission.IsAuthorized)
                    userClaims.Add(permission.Claim);

            foreach (var permission in userPermissions)
                if (!permission.IsAuthorized)
                    userClaims.Remove(permission.Claim);

            return requiredClaims.Select(requiredClaim => userClaims.Contains(requiredClaim)).ToList();
        }

        private string ReportRoles(IUserInfo userInfo, IList<Guid> userAllRoles)
        {
            // Reporting is done in a function that returns string, to avoid any performance impact when the trace log is not enabled.

            return string.Format("User {0} has {1} roles: {2}.",
                userInfo.UserName,
                userAllRoles.Count,
                string.Join(", ", _roleRepository.Value.Query().Where(role => userAllRoles.Contains(role.ID)).Select(role => role.Name)));
        }

        private string ReportPermissions(IUserInfo userInfo, IList<PermissionValue> userPermissions, IList<Claim> requiredClaims, IList<bool> userHasClaims)
        {
            // Reporting is done in a function that returns string, to avoid any performance impact when the trace log is not enabled.

            var report = new List<string>();

            var permissionsByClaim = new MultiDictionary<Claim, PermissionValue>();
            foreach (var permission in userPermissions)
                permissionsByClaim.Add(permission.Claim, permission);

            // Create an index of role and principal names:

            var roleIds = userPermissions.Where(p => p.RoleID != null).Select(p => p.RoleID.Value).ToList();
            var roleNames = _roleRepository.Value.Query().Where(role => roleIds.Contains(role.ID))
                .Select(role => new { role.ID, role.Name }).ToList()
                .ToDictionary(role => role.ID, role => role.Name);
            foreach (var roleId in roleIds.Except(roleNames.Keys))
                roleNames.Add(roleId, roleId.ToString());

            var principalIds = userPermissions.Where(p => p.PrincipalID != null).Select(p => p.PrincipalID.Value).ToList();
            var principalNames = _principalRepository.Value.Query().Where(principal => principalIds.Contains(principal.ID))
                .Select(principal => new { principal.ID, principal.Name }).ToList()
                .ToDictionary(principal => principal.ID, principal => principal.Name);
            foreach (var principalId in principalIds.Except(principalNames.Keys))
                principalNames.Add(principalId, principalId.ToString());

            // Create an index of inactive claims:

            var missingClaims = requiredClaims.Except(permissionsByClaim.Keys).ToList();
            var missingClaimsResources = missingClaims.Select(claim => claim.Resource).ToList();
            var missingClaimsRights = missingClaims.Select(claim => claim.Right).ToList();

            var inactiveClaims = new HashSet<Claim>(
                _claimRepository.Value.Query()
                    .Where(claim =>
                        missingClaimsResources.Contains(claim.ClaimResource) // Loading more claims than necessary, but using some of the SQL indexes on Claim table.
                        && missingClaimsRights.Contains(claim.ClaimRight)
                        && (claim.Active == null || claim.Active == false))
                    .Select(claim => new Claim(claim.ClaimResource, claim.ClaimRight)));

            // Analyze permissions for required claims:

            foreach (var requiredClaim in requiredClaims.Zip(userHasClaims, (Claim, UserHasIt) => new { Claim, UserHasIt }))
            {
                var claimPermissions = permissionsByClaim.Get(requiredClaim.Claim).Select(permission => new
                {
                    permission.IsAuthorized,
                    PrincipalOrRoleName = permission.PrincipalID != null
                        ? ("principal " + principalNames[permission.PrincipalID.Value])
                        : ("role " + roleNames[permission.RoleID.Value])
                }).ToList();

                var allowedFor = claimPermissions.Where(p => p.IsAuthorized).Select(p => p.PrincipalOrRoleName).ToList();
                var deniedFor = claimPermissions.Where(p => !p.IsAuthorized).Select(p => p.PrincipalOrRoleName).ToList();

                string explanation = "User " + userInfo.UserName + " " + (requiredClaim.UserHasIt ? "has" : "doesn't have") + " claim '" + requiredClaim.Claim.FullName + "'. It is ";

                if (deniedFor.Count != 0)
                    if (allowedFor.Count != 0)
                        explanation += "denied for " + string.Join(", ", deniedFor) + " and allowed for " + string.Join(", ", allowedFor) + " (deny overrides allow).";
                    else
                        explanation += "denied for " + string.Join(", ", deniedFor) + ".";
                else
                    if (allowedFor.Count != 0)
                        explanation += "allowed for " + string.Join(", ", allowedFor) + ".";
                    else
                        if (inactiveClaims.Contains(requiredClaim.Claim))
                            explanation += "denied by default (the claim is no longer active).";
                        else
                            explanation += "denied by default (no permissions defined).";

                report.Add(explanation);
            }

            return string.Join("\r\n", report);
        }
    }
}
