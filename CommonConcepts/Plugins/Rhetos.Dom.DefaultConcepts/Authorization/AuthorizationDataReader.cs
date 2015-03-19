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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
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
            var query = _principalPermissionRepository.Query()
                .Where(principalPermission => principalPermission.IsAuthorized != null
                    && principalPermission.Principal.ID == principal.ID);

            CsUtility.Materialize(ref claimIds);
            bool useSqlFilter = claimIds.Count() < 500; // Avoiding performance issue with large query parameter.
            if (useSqlFilter)
                query = query.Where(principalPermission => claimIds.Contains(principalPermission.Claim.ID));
                
            var loaded = query.Select(principalPermission => new PrincipalPermissionInfo
                    {
                        ID = principalPermission.ID,
                        PrincipalID = principalPermission.Principal.ID,
                        ClaimID = principalPermission.Claim.ID,
                        IsAuthorized = principalPermission.IsAuthorized.Value,
                    })
                .ToList();

            if (!useSqlFilter)
            {
                var claimsIndex = new HashSet<Guid>(claimIds);
                loaded = loaded.Where(principalPermission => claimsIndex.Contains(principalPermission.ClaimID)).ToList();
            }

            return loaded;
        }

        public IList<RolePermissionInfo> GetRolePermissions(IEnumerable<Guid> roleIds, IEnumerable<Guid> claimIds)
        {
            var query = _rolePermissionRepository.Query()
                .Where(rolePermission => rolePermission.IsAuthorized != null
                    && roleIds.Contains(rolePermission.Role.ID));

            CsUtility.Materialize(ref claimIds);
            bool useSqlFilter = claimIds.Count() < 500; // Avoiding performance issue with large query parameter.
            if (useSqlFilter)
                query = query.Where(rolePermission => claimIds.Contains(rolePermission.Claim.ID));

            var loaded = query.Select(rolePermission => new RolePermissionInfo
                    {
                        ID = rolePermission.ID,
                        RoleID = rolePermission.Role.ID,
                        ClaimID = rolePermission.Claim.ID,
                        IsAuthorized = rolePermission.IsAuthorized.Value,
                    })
                .ToList();

            if (!useSqlFilter)
            {
                var claimsIndex = new HashSet<Guid>(claimIds);
                loaded = loaded.Where(rolePermission => claimsIndex.Contains(rolePermission.ClaimID)).ToList();
            }

            return loaded;
        }

        /// <summary>
        /// The result will always have same number and arrangement of elements as the requiredClaims parameter.
        /// Inactive or nonexistent claims will have ID set to null.
        /// </summary>
        public IList<ClaimInfo> GetClaims(IEnumerable<Claim> requiredClaims)
        {
            var claimsResources = requiredClaims.Select(claim => claim.Resource).Distinct().ToList();
            var claimsRights = requiredClaims.Select(claim => claim.Right).Distinct().ToList();

            // Loading more claims then necessary because of using the cartesian product of ClaimResource and ClaimRight,
            // in order to utilize an SQL index on Common.Claim.

            var queryClaims = _claimRepository.Query().Where(claim => claim.Active != null && claim.Active.Value);
            if (claimsResources.Count < 500 && claimsRights.Count < 500) // Avoiding performance issue with large query parameter.
                queryClaims = queryClaims.Where(claim => claimsResources.Contains(claim.ClaimResource) && claimsRights.Contains(claim.ClaimRight));
            var loadedClaims = queryClaims
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
