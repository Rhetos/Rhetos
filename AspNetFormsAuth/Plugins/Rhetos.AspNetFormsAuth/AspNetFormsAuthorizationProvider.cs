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

namespace Rhetos.AspNetFormsAuth
{
    [Export(typeof(IAuthorizationProvider))]
    public class AspNetFormsAuthorizationProvider : IAuthorizationProvider
    {
        private readonly Lazy<IQueryableRepository<IPrincipal>> _principalRepository;
        private readonly Lazy<IQueryableRepository<IPrincipalHasRole>> _principalRolesRepository;
        private readonly Lazy<IQueryableRepository<IRoleInheritsRole>> _roleRolesRepository;
        private readonly Lazy<IQueryableRepository<IPermission>> _permissionRepository;
        private readonly ILogger _logger;

        public AspNetFormsAuthorizationProvider(
            Lazy<IQueryableRepository<IPrincipal>> principalRepository,
            Lazy<IQueryableRepository<IPrincipalHasRole>> principalRolesRepository,
            Lazy<IQueryableRepository<IRoleInheritsRole>> roleRolesRepository,
            Lazy<IQueryableRepository<IPermission>> permissionRepository,
            ILogProvider logProvider)
        {
            _principalRepository = principalRepository;
            _principalRolesRepository = principalRolesRepository;
            _roleRolesRepository = roleRolesRepository;
            _permissionRepository = permissionRepository;
            _logger = logProvider.GetLogger("AspNetFormsAuthorizationProvider");
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            IList<Guid> userAllRoles = GetUsersRoles(userInfo);
            IList<PermissionValue> userPermissions = GetUsersPermissions(requiredClaims, userAllRoles);
            return GetUsersClaims(requiredClaims, userPermissions);
        }

        private class PermissionValue
        {
            public Claim Claim;
            public bool IsAuthorized;
        }

        private IList<Guid> GetUsersRoles(IUserInfo userInfo)
        {
            string userName = userInfo.UserName;
            IList<Guid> userDirectRoles = _principalRolesRepository.Value.Query().Where(pr => pr.Principal.Name == userName).Select(pr => pr.Role.ID).ToList();
            if (userDirectRoles.Count() == 0)
                ValidateUser(userInfo);
            IList<Tuple<Guid, Guid>> roleInheritsRole = _roleRolesRepository.Value.Query().Select(rr => Tuple.Create(rr.Derived.ID, rr.InheritsFrom.ID)).ToList();
            IList<Guid> userAllRoles = Graph.IncludeDependents(userDirectRoles, roleInheritsRole);
            return userAllRoles;
        }

        private void ValidateUser(IUserInfo userInfo)
        {
            Guid userId = _principalRepository.Value.Query().Where(p => p.Name == userInfo.UserName).Select(p => p.ID).SingleOrDefault();
            if (userId == default(Guid))
            {
                _logger.Error("There is no principal with the given username '" + userInfo.UserName + "' in Common.Principal.");
                throw new ClientException("There is no principal with the given username.");
            }
        }

        private IList<PermissionValue> GetUsersPermissions(IList<Claim> requiredClaims, IList<Guid> userAllRoles)
        {
            var claimNames = requiredClaims.Select(claim => claim.Resource + "." + claim.Right).ToList();
            var claimIndex = new HashSet<Rhetos.Security.Claim>(requiredClaims);

            IList<PermissionValue> userPermissions = _permissionRepository.Value.Query()
                .Where(permission =>
                    userAllRoles.Contains(permission.Role.ID)
                    && claimNames.Contains(permission.Claim.ClaimResource + "." + permission.Claim.ClaimRight)
                    && permission.IsAuthorized != null)
                .Select(permission => new PermissionValue
                    {
                        Claim = new Claim(permission.Claim.ClaimResource, permission.Claim.ClaimRight),
                        IsAuthorized = permission.IsAuthorized.Value
                    })
                .ToList();
            return userPermissions;
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
    }
}
