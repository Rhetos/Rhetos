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
using Rhetos.Security;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts.Authorization
{
    public class AuthorizationDataReader
    {
        private readonly ILogger _logger;
        private readonly IQueryableRepository<IPrincipal> _principalRepository;
        private readonly IQueryableRepository<IPrincipalHasRole, IPrincipal> _principalRolesRepository;
        private readonly IQueryableRepository<IRoleInheritsRole> _roleRolesRepository;
        private readonly IQueryableRepository<IPrincipalPermission> _principalPermissionRepository;
        private readonly IQueryableRepository<IRolePermission> _rolePermissionRepository;
        private readonly IQueryableRepository<IRole> _roleRepository;
        private readonly IQueryableRepository<ICommonClaim> _claimRepository;

        public AuthorizationDataReader(
            ILogProvider logProvider,
            INamedPlugins<IRepository> repositories,
            IQueryableRepository<IPrincipal> principalRepository,
            IQueryableRepository<IRoleInheritsRole> roleRolesRepository,
            IQueryableRepository<IPrincipalPermission> principalPermissionRepository,
            IQueryableRepository<IRolePermission> rolePermissionRepository,
            IQueryableRepository<IRole> roleRepository,
            IQueryableRepository<ICommonClaim> claimRepository)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _principalRepository = principalRepository;
            _principalRolesRepository = (IQueryableRepository<IPrincipalHasRole, IPrincipal>)repositories.GetPlugin("Common.PrincipalHasRole");
            _roleRolesRepository = roleRolesRepository;
            _principalPermissionRepository = principalPermissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _roleRepository = roleRepository;
            _claimRepository = claimRepository;
        }

        public PrincipalInfo GetPrincipal(string username)
        {
            var principal = _principalRepository.Query()
                .Where(p => p.Name == username)
                .Select(p => new PrincipalInfo { ID = p.ID, Name = p.Name })
                .SingleOrDefault();

            if (principal == null)
            {
                _logger.Error("There is no principal with the given username '" + username + "' in Common.Principal.");
                throw new ClientException("There is no principal with the given username.");
            }
            return principal;
        }

        public IList<Guid> GetPrincipalRoles(IPrincipal principal)
        {
            return _principalRolesRepository.Query(principal).Select(pr => pr.Role.ID).ToList();
        }

        public IList<Guid> GetRoleRoles(Guid roleId)
        {
            return _roleRolesRepository.Query().Where(rr => rr.UsersFrom.ID == roleId).Select(rr => rr.PermissionsFrom.ID).ToList();
        }

        public IList<PrincipalPermissionInfo> GetPrincipalPermissions(IPrincipal principal, IEnumerable<Guid> claimIds)
        {
            return _principalPermissionRepository.Query()
                .Where(principalPermission => principalPermission.IsAuthorized != null
                    && principalPermission.Principal.ID == principal.ID
                    && claimIds.Contains(principalPermission.Claim.ID))
                .Select(principalPermission => new PrincipalPermissionInfo
                    {
                        ID = principalPermission.ID,
                        PrincipalID = principalPermission.Principal.ID,
                        ClaimID = principalPermission.Claim.ID,
                        IsAuthorized = principalPermission.IsAuthorized.Value,
                    })
                .ToList();
        }

        public IList<RolePermissionInfo> GetRolePermissions(IEnumerable<Guid> roleIds, IEnumerable<Guid> claimIds)
        {
            return _rolePermissionRepository.Query()
                .Where(rolePermission => rolePermission.IsAuthorized != null
                    && roleIds.Contains(rolePermission.Role.ID)
                    && claimIds.Contains(rolePermission.Claim.ID))
                .Select(rolePermission => new RolePermissionInfo
                    {
                        ID = rolePermission.ID,
                        RoleID = rolePermission.Role.ID,
                        ClaimID = rolePermission.Claim.ID,
                        IsAuthorized = rolePermission.IsAuthorized.Value,
                    })
                .ToList();
        }

        /// <summary>
        /// The result will always have same number and arrangement of elements as the requiredClaims parameter.
        /// Inactive or nonexistent claims will have ID set to null.
        /// </summary>
        public IList<ClaimInfo> GetClaims(IEnumerable<Claim> requiredClaims)
        {
            var claimsResources = requiredClaims.Select(claim => claim.Resource).ToList();
            var claimsRights = requiredClaims.Select(claim => claim.Right).ToList();

            // Loading more claims then necessary because of using the cartesian product of ClaimResource and ClaimRight,
            // in order to utilize an SQL index on Common.Claim.
            var loadedClaims = _claimRepository.Query()
                .Where(claim => claim.Active == true
                    && claimsResources.Contains(claim.ClaimResource)
                    && claimsRights.Contains(claim.ClaimRight))
                .Select(claim => new ClaimInfo
                    {
                        ID = claim.ID,
                        Resource = claim.ClaimResource,
                        Right = claim.ClaimRight
                    })
                .ToList();

            Dictionary<Claim, ClaimInfo> claimsIndex;
            try
            {
                claimsIndex = loadedClaims.ToDictionary(item => new Claim(item.Resource, item.Right));
            }
            catch
            {
                var duplicates = loadedClaims.GroupBy(item => new Claim(item.Resource, item.Right))
                    .Where(g => g.Count() > 1)
                    .FirstOrDefault();
                if (duplicates != null)
                    throw new FrameworkException(string.Format("Loaded duplicate claims: '{0} {1}' and '{2} {3}'.",
                        duplicates.First().Resource, duplicates.First().Right,
                        duplicates.Last().Resource, duplicates.Last().Right));
                throw;
            }

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

        /// <summary>
        /// The result will always have same number and arrangement of elements as the roleIds parameter.
        /// Nonexistent roles will have Name set to null.
        /// </summary>
        public IList<RoleInfo> GetRoles(IEnumerable<Guid> roleIds)
        {
            var loadedRoles = _roleRepository.Query()
                .Where(role => roleIds.Contains(role.ID))
                .Select(role => new { role.ID, role.Name })
                .ToList()
                .ToDictionary(role => role.ID, role => role.Name);

            return roleIds.Select(id =>
                {
                    string name;
                    if (!loadedRoles.TryGetValue(id, out name))
                        name = null;
                    return new RoleInfo { ID = id, Name = name };
                })
                .ToList();
        }
    }
}
