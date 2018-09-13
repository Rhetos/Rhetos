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
    public class AuthorizationDataLoader : IAuthorizationData
    {
        private readonly ILogger _logger;
        private readonly IQueryableRepository<IPrincipal> _principalRepository;
        private readonly IQueryableRepository<IPrincipalHasRole> _principalRolesRepository;
        private readonly Lazy<GenericRepository<IPrincipal>> _principalGenericRepository; // Lazy because it's rarely used.
        private readonly IQueryableRepository<IRoleInheritsRole> _roleRolesRepository;
        private readonly IQueryableRepository<IPrincipalPermission> _principalPermissionRepository;
        private readonly IQueryableRepository<IRolePermission> _rolePermissionRepository;
        private readonly IQueryableRepository<IRole> _roleRepository;
        private readonly IQueryableRepository<ICommonClaim> _claimRepository;

        public AuthorizationDataLoader(
            ILogProvider logProvider,
            INamedPlugins<IRepository> repositories,
            Lazy<GenericRepository<IPrincipal>> principalGenericRepository)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _principalRepository = (IQueryableRepository<IPrincipal>)repositories.GetPlugin("Common.Principal");
            _principalGenericRepository = principalGenericRepository;
            _principalRolesRepository = (IQueryableRepository<IPrincipalHasRole>)repositories.GetPlugin("Common.PrincipalHasRole");
            _roleRolesRepository = (IQueryableRepository<IRoleInheritsRole>)repositories.GetPlugin("Common.RoleInheritsRole");
            _principalPermissionRepository = (IQueryableRepository<IPrincipalPermission>)repositories.GetPlugin("Common.PrincipalPermission");
            _rolePermissionRepository = (IQueryableRepository<IRolePermission>)repositories.GetPlugin("Common.RolePermission");
            _roleRepository = (IQueryableRepository<IRole>)repositories.GetPlugin("Common.Role");
            _claimRepository = (IQueryableRepository<ICommonClaim>)repositories.GetPlugin("Common.Claim");
        }

        private static bool? _shouldAddUnregisteredPrincipal;

        private static bool ShouldAddUnregisteredPrincipal()
        {
            if (_shouldAddUnregisteredPrincipal == null)
            {
                string setting = ConfigUtility.GetAppSetting("AuthorizationAddUnregisteredPrincipals");
                if (!string.IsNullOrEmpty(setting))
                    _shouldAddUnregisteredPrincipal = bool.Parse(setting);
                else
                    _shouldAddUnregisteredPrincipal = false;
            }
            return _shouldAddUnregisteredPrincipal.Value;
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

            if (principal.ID == default(Guid))
            {
                if (ShouldAddUnregisteredPrincipal())
                {
                    _logger.Info(() => "Adding unregistered principal '" + username + "'. See AuthorizationAddUnregisteredPrincipals in web.config.");
                    var newPrincipal = _principalGenericRepository.Value.CreateInstance();
                    newPrincipal.ID = Guid.NewGuid();
                    newPrincipal.Name = username;
                    try
                    {
                        _principalGenericRepository.Value.Insert(newPrincipal);
                        principal.ID = newPrincipal.ID;
                    }
                    catch (System.Data.SqlClient.SqlException ex)
                    {
                        if (ex.Message.StartsWith("Cannot insert duplicate key row in object 'Common.Principal' with unique index 'IX_Principal_Name'"))
                        {
                            _logger.Info(() => "Ignoring concurrent principal creation: " + ex.GetType().Name + ": " + ex.Message);
                            principal.ID = GetPrincipalID(username);
                            if (principal.ID == default(Guid))
                                throw new FrameworkException("Cannot create the principal record for '" + username + "'.", ex);
                        }
                        else
                            throw;
                    }
                }
                else
                {
                    _logger.Info($"There is no principal with the username '{username}' in Common.Principal.");
                    throw new UserException($"Your account '{username}' is not registered in the system. Please contact the system administrator.");
                }
            }
            return principal;
        }

        public IEnumerable<Guid> GetPrincipalRoles(IPrincipal principal)
        {
            return _principalRolesRepository.Query(principal).Select(pr => pr.RoleID.Value).ToList();
        }

        public IEnumerable<Guid> GetRoleRoles(Guid roleId)
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
        public IEnumerable<PrincipalPermissionInfo> GetPrincipalPermissions(IPrincipal principal, IEnumerable<Guid> claimIds = null)
        {
            CsUtility.Materialize(ref claimIds);
            if (claimIds != null && claimIds.Count() == 0)
                return Enumerable.Empty<PrincipalPermissionInfo>();

            var query = _principalPermissionRepository.Query()
                .Where(principalPermission => principalPermission.IsAuthorized != null
                    && principalPermission.PrincipalID == principal.ID);

            if (claimIds != null && claimIds.Count() < _sqlFilterItemsLimit)
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
        public IEnumerable<RolePermissionInfo> GetRolePermissions(IEnumerable<Guid> roleIds, IEnumerable<Guid> claimIds = null)
        {
            CsUtility.Materialize(ref roleIds);
            CsUtility.Materialize(ref claimIds);
            if (roleIds.Count() == 0 || (claimIds != null && claimIds.Count() == 0))
                return Enumerable.Empty<RolePermissionInfo>();

            var query = _rolePermissionRepository.Query()
                .Where(rolePermission => rolePermission.IsAuthorized != null
                    && roleIds.Contains(rolePermission.RoleID.Value));

            if (claimIds != null && claimIds.Count() < _sqlFilterItemsLimit)
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
        public IDictionary<Guid, string> GetRoles(IEnumerable<Guid> roleIds = null)
        {
            CsUtility.Materialize(ref roleIds);
            if (roleIds != null && roleIds.Count() == 0)
                return new Dictionary<Guid, string>();

            var query = _roleRepository.Query();
            if (roleIds != null)
                query = query.Where(role => roleIds.Contains(role.ID));

            return query
                .Select(role => new { ID = role.ID, Name = role.Name })
                .ToList()
                .ToDictionary(role => role.ID, role => role.Name);
        }

        /// <summary>
        /// The function may return more claims than required.
        /// Note that the result will not include claims that are inactive or do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        public IDictionary<Claim, ClaimInfo> GetClaims(IEnumerable<Claim> requiredClaims = null)
        {
            CsUtility.Materialize(ref requiredClaims);
            if (requiredClaims != null && requiredClaims.Count() == 0)
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
                    .Where(g => g.Count() > 1)
                    .FirstOrDefault();
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
