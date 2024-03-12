﻿/*
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
    public class AuthorizationDataLoader : IAuthorizationData
    {
        private readonly ILogger _logger;
        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly Lazy<PrincipalWriter> _principalWriter; // Lazy because it's rarely used.
        private readonly IQueryableRepository<IPrincipal> _principalRepository;
        private readonly IQueryableRepository<IPrincipalHasRole> _principalRolesRepository;
        private readonly IQueryableRepository<IRoleInheritsRole> _roleRolesRepository;
        private readonly IQueryableRepository<IPrincipalPermission> _principalPermissionRepository;
        private readonly IQueryableRepository<IRolePermission> _rolePermissionRepository;
        private readonly IQueryableRepository<IRole> _roleRepository;
        private readonly IQueryableRepository<ICommonClaim> _claimRepository;

        public AuthorizationDataLoader(
            ILogProvider logProvider,
            RhetosAppOptions rhetosAppOptions,
            Lazy<PrincipalWriter> principalWriter,
            INamedPlugins<IRepository> repositories)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _rhetosAppOptions = rhetosAppOptions;
            _principalWriter = principalWriter;
            _principalRepository = (IQueryableRepository<IPrincipal>)repositories.GetPlugin("Common.Principal");
            _principalRolesRepository = (IQueryableRepository<IPrincipalHasRole>)repositories.GetPlugin("Common.PrincipalHasRole");
            _roleRolesRepository = (IQueryableRepository<IRoleInheritsRole>)repositories.GetPlugin("Common.RoleInheritsRole");
            _principalPermissionRepository = (IQueryableRepository<IPrincipalPermission>)repositories.GetPlugin("Common.PrincipalPermission");
            _rolePermissionRepository = (IQueryableRepository<IRolePermission>)repositories.GetPlugin("Common.RolePermission");
            _roleRepository = (IQueryableRepository<IRole>)repositories.GetPlugin("Common.Role");
            _claimRepository = (IQueryableRepository<ICommonClaim>)repositories.GetPlugin("Common.Claim");
        }

        private Guid GetPrincipalID(string username)
        {
            return _principalRepository.Query()
                .Where(p => p.Name == username)
                .Select(p => p.ID)
                .SingleOrDefault();
        }

        public PrincipalInfo GetPrincipal(string username)
        {
            var principal = new PrincipalInfo { ID = GetPrincipalID(username), Name = username };

            if (principal.ID != default)
            {
                return principal;
            }
            else if (_rhetosAppOptions.AuthorizationAddUnregisteredPrincipals)
            {
                _principalWriter.Value.SafeInsertPrincipal(ref principal);
                return principal;
            }
            else
            {
                _logger.Warning($"There is no principal with the username '{username}' in Common.Principal.");
                throw new UserException("Your account '{0}' is not registered in the system. Please contact the system administrator.", new object[] { username });
            }
        }

        public IReadOnlyCollection<Guid> GetPrincipalRoles(IPrincipal principal)
        {
            return _principalRolesRepository.Query(principal).Select(pr => pr.RoleID.Value).ToList();
        }

        public IReadOnlyCollection<Guid> GetRoleRoles(Guid roleId)
        {
            return _roleRolesRepository.Query().Where(rr => rr.UsersFromID == roleId).Select(rr => rr.PermissionsFromID.Value).ToList();
        }

        /// <summary>
        /// Avoiding performance issue with large query parameter.
        /// </summary>
        private const int _sqlFilterItemsLimit = 500;

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        public IReadOnlyCollection<PrincipalPermissionInfo> GetPrincipalPermissions(IPrincipal principal, IReadOnlyCollection<Guid> claimIds = null)
        {
            if (claimIds != null && claimIds.Count == 0)
                return [];

            var query = _principalPermissionRepository.Query()
                .Where(principalPermission => principalPermission.IsAuthorized != null
                    && principalPermission.PrincipalID == principal.ID);

            if (claimIds != null && claimIds.Count < _sqlFilterItemsLimit)
                query = query.Where(principalPermission => claimIds.Contains(principalPermission.ClaimID.Value));
                
            return query.Select(principalPermission => new PrincipalPermissionInfo
                    {
                        ID = principalPermission.ID,
                        PrincipalID = principalPermission.PrincipalID.Value,
                        ClaimID = principalPermission.ClaimID.Value,
                        IsAuthorized = principalPermission.IsAuthorized.Value,
                    })
                .ToList();
        }

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        public IReadOnlyCollection<RolePermissionInfo> GetRolePermissions(IReadOnlyCollection<Guid> roleIds, IReadOnlyCollection<Guid> claimIds = null)
        {
            if (roleIds.Count == 0 || (claimIds != null && claimIds.Count == 0))
                return [];

            var query = _rolePermissionRepository.Query()
                .Where(rolePermission => rolePermission.IsAuthorized != null
                    && roleIds.Contains(rolePermission.RoleID.Value));

            if (claimIds != null && claimIds.Count < _sqlFilterItemsLimit)
                query = query.Where(rolePermission => claimIds.Contains(rolePermission.ClaimID.Value));

            return query.Select(rolePermission => new RolePermissionInfo
                    {
                        ID = rolePermission.ID,
                        RoleID = rolePermission.RoleID.Value,
                        ClaimID = rolePermission.ClaimID.Value,
                        IsAuthorized = rolePermission.IsAuthorized.Value,
                    })
                .ToList();
        }

        /// <summary>
        /// Note that the result will not include roles that do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        public IDictionary<Guid, string> GetRoles(IReadOnlyCollection<Guid> roleIds = null)
        {
            if (roleIds != null && roleIds.Count == 0)
                return new Dictionary<Guid, string>();

            var query = _roleRepository.Query();
            if (roleIds != null)
                query = query.Where(role => roleIds.Contains(role.ID));

            return query
                .Select(role => new { role.ID, role.Name }) // This select avoids loading extra columns from database.
                .ToDictionary(role => role.ID, role => role.Name);
        }

        public IDictionary<SystemRole, Guid> GetSystemRoles()
        {
            string[] roleNames = Enum.GetNames(typeof(SystemRole));

            return _roleRepository.Query()
                .Where(role => roleNames.Contains(role.Name))
                .Select(role => new { role.ID, role.Name }) // This select avoids loading extra columns from database.
                .AsEnumerable()
                .Where(role => roleNames.Contains(role.Name)) // Ignore any extra names returned by case-insensitive database search.
                .ToDictionary(role => (SystemRole)Enum.Parse(typeof(SystemRole), role.Name), role => role.ID);
        }

        /// <summary>
        /// The function may return more claims than required.
        /// Note that the result will not include claims that are inactive or do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        public IDictionary<Claim, ClaimInfo> GetClaims(IReadOnlyCollection<Claim> requiredClaims = null)
        {
            if (requiredClaims != null && requiredClaims.Count == 0)
                return new Dictionary<Claim, ClaimInfo>();

            var queryClaims = _claimRepository.Query().Where(claim => claim.Active != null && claim.Active.Value);

            if (requiredClaims != null)
            {
                var claimsResources = requiredClaims.Select(claim => claim.Resource).Distinct().ToList();
                var claimsRights = requiredClaims.Select(claim => claim.Right).Distinct().ToList();

                if (claimsResources.Count < _sqlFilterItemsLimit && claimsRights.Count < _sqlFilterItemsLimit)
                    queryClaims = queryClaims.Where(claim => claimsResources.Contains(claim.ClaimResource) && claimsRights.Contains(claim.ClaimRight));
            }

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
                    .FirstOrDefault(g => g.Count() > 1);
                if (duplicates != null)
                    throw new FrameworkException(string.Format("Loaded duplicate claims: '{0} {1}' and '{2} {3}'.",
                        duplicates.First().Resource, duplicates.First().Right,
                        duplicates.Last().Resource, duplicates.Last().Right));
                throw;
            }

            return claimsIndex;
        }
    }
}
