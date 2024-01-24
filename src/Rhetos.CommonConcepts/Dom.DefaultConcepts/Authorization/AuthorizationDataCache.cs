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

using Rhetos.Dom.DefaultConcepts.Authorization;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    public class AuthorizationDataCache : IAuthorizationData
    {
        private readonly ILogger _logger;
        private readonly Lazy<AuthorizationDataLoader> _authorizationDataReader; // Lazy because requests that use all cached data do not need the reader. The reader requires multiple repository instances created.
        private readonly RequestAndGlobalCache _cache;

        public AuthorizationDataCache(
            ILogProvider logProvider,
            Lazy<AuthorizationDataLoader> authorizationDataReader,
            RequestAndGlobalCache cache)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _authorizationDataReader = authorizationDataReader;
            _cache = cache;
        }

        public PrincipalInfo GetPrincipal(string username)
        {
            return _cache.GetOrAdd("AuthorizationDataCache.Principal." + username.ToLower(),
                () => _authorizationDataReader.Value.GetPrincipal(username));
        }

        public IEnumerable<Guid> GetPrincipalRoles(IPrincipal principal)
        {
            return _cache.GetOrAdd("AuthorizationDataCache.PrincipalRoles." + principal.Name.ToLower() + "." + principal.ID.ToString(),
                () => _authorizationDataReader.Value.GetPrincipalRoles(principal));
        }

        public IEnumerable<Guid> GetRoleRoles(Guid roleId)
        {
            return _cache.GetOrAdd("AuthorizationDataCache.RoleRoles." + roleId.ToString(),
                () => _authorizationDataReader.Value.GetRoleRoles(roleId));
        }

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        public IEnumerable<PrincipalPermissionInfo> GetPrincipalPermissions(IPrincipal principal, IEnumerable<Guid> claimIds = null)
        {
            return _cache.GetOrAdd("AuthorizationDataCache.PrincipalPermissions." + principal.Name.ToLower() + "." + principal.ID.ToString(),
                () => _authorizationDataReader.Value.GetPrincipalPermissions(principal, null));
        }

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        public IEnumerable<RolePermissionInfo> GetRolePermissions(IEnumerable<Guid> roleIds, IEnumerable<Guid> claimIds = null)
        {
            List<IEnumerable<RolePermissionInfo>> results = new();

            static string cacheKey(Guid roleId) => "AuthorizationDataCache.RolePermissions." + roleId.ToString();

            var missingCache = new List<Guid>();

            // This method does not use _cache.GetOrAdd directly.
            // Instead, it uses _cache.Get and _cache.Set separately, in order to allow reading role permissions
            // from database in a single query for multiple roles.

            foreach (Guid roleId in roleIds)
            {
                string key = cacheKey(roleId);
                var rolePermissions = _cache.Get<IEnumerable<RolePermissionInfo>>(key);
                if (rolePermissions != null)
                    results.Add(rolePermissions);
                else
                {
                    missingCache.Add(roleId);
                    _logger.Trace(() => "Cache miss: " + key + ".");
                }
            }

            var freshPermissions = missingCache.Count == 0
                ? Enumerable.Empty<RolePermissionInfo>()
                : _authorizationDataReader.Value.GetRolePermissions(missingCache, null);

            results.Add(freshPermissions);

            var freshPermissionsByRole = freshPermissions.GroupBy(p => p.RoleID).ToDictionary(group => group.Key, group => group.ToList());
            foreach (Guid roleId in missingCache)
                if (!freshPermissionsByRole.ContainsKey(roleId))
                    freshPermissionsByRole.Add(roleId, new List<RolePermissionInfo>());

            foreach (var rolePermissions in freshPermissionsByRole)
                _cache.Set(cacheKey(rolePermissions.Key), rolePermissions.Value);

            return CsUtility.Concatenate(results);
        }

        /// <summary>
        /// The function may return more roles than required.
        /// Note that the result will not include roles that do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        public IDictionary<Guid, string> GetRoles(IEnumerable<Guid> roleIds = null)
        {
            return _cache.GetOrAdd("AuthorizationDataCache.Roles",
                () => _authorizationDataReader.Value.GetRoles());
        }

        public IDictionary<SystemRole, Guid> GetSystemRoles()
        {
            return _cache.GetOrAdd("AuthorizationDataCache.SystemRoles",
                () => _authorizationDataReader.Value.GetSystemRoles());
        }

        /// <summary>
        /// The function may return more claims than required.
        /// Note that the result will not include claims that are inactive or do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        public IDictionary<Claim, ClaimInfo> GetClaims(IEnumerable<Claim> requiredClaims = null)
        {
            return _cache.GetOrAdd("AuthorizationDataCache.Claims",
                () => _authorizationDataReader.Value.GetClaims(null),
                immutable: true); // Claims do not expire. They cannot be modified at run-time, only at deploy-time.
        }

        /// <summary>
        /// Clears global authorization cache, for <b>unit testing</b>.
        /// </summary>
        /// <remarks>
        /// Note that it does not clear the current scope cache from existing instances of AuthorizationDataCache.
        /// See <see cref="RequestAndGlobalCache"/> for more info.
        /// </remarks>
        public static void ClearCacheAll()
        {
            RequestAndGlobalCache.ClearGlobalCacheAll();
        }

        /// <summary>
        /// Clears the principals' authorization data from the cache: Principal, PrincipalRoles and PrincipalPermissions.
        /// </summary>
        public void ClearCachePrincipals(IEnumerable<IPrincipal> principals)
        {
            CsUtility.Materialize(ref principals);
            _logger.Trace(() => "ClearCachePrincipals: " + string.Join(", ", principals.Select(p => p.Name + " " + p.ID.ToString())) + ".");

            var deleteKeys =
                principals.Select(principal => "AuthorizationDataCache.Principal." + principal.Name.ToLower())
                .Concat(principals.Select(principal => "AuthorizationDataCache.PrincipalRoles." + principal.Name.ToLower() + "." + principal.ID.ToString()))
                .Concat(principals.Select(principal => "AuthorizationDataCache.PrincipalPermissions." + principal.Name.ToLower() + "." + principal.ID.ToString()))
                .Distinct();

            foreach (string key in deleteKeys)
                _cache.RemoveFromBothCaches(key);
        }

        /// <summary>
        /// Clears the roles' authorization data from the cache: RoleRoles, RolePermissions and Role (full list).
        /// </summary>
        public void ClearCacheRoles(IEnumerable<Guid> roleIds)
        {
            CsUtility.Materialize(ref roleIds);
            _logger.Trace(() => "ClearCacheRoles: " + string.Join(", ", roleIds) + ".");

            var deleteKeys =
                roleIds.Distinct()
                .Select(roleId => "AuthorizationDataCache.RoleRoles." + roleId.ToString())
                .Concat(roleIds.Select(roleId => "AuthorizationDataCache.RolePermissions." + roleId.ToString()))
                .Concat(new[] { "AuthorizationDataCache.Roles" });

            foreach (string key in deleteKeys)
                _cache.RemoveFromBothCaches(key);

            var systemRoles = _cache.GetAllDataFromBothCaches<IDictionary<SystemRole, Guid>>("AuthorizationDataCache.SystemRoles");
            var invalidatedSystemRoles = systemRoles.SelectMany(sr => sr.Values).Intersect(roleIds);
            if (invalidatedSystemRoles.Any())
            {
                _logger.Trace(() => "ClearCacheRoles: SystemRoles.");
                _cache.RemoveFromBothCaches("AuthorizationDataCache.SystemRoles");
            }
        }
    }
}
