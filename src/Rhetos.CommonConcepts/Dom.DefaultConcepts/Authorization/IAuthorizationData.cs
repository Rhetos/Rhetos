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

using Rhetos.Security;
using System;
using System.Collections.Generic;

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IAuthorizationData
    {
        PrincipalInfo GetPrincipal(string username);

        IEnumerable<Guid> GetPrincipalRoles(IPrincipal principal);

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        IEnumerable<PrincipalPermissionInfo> GetPrincipalPermissions(IPrincipal principal, IEnumerable<Guid> claimIds = null);

        /// <summary>
        /// The function may return more items than required.
        /// Note that the result will not include roles that do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        IDictionary<Guid, string> GetRoles(IEnumerable<Guid> roleIds = null);

        IDictionary<SystemRole, Guid> GetSystemRoles();

        IEnumerable<Guid> GetRoleRoles(Guid roleId);

        /// <summary>
        /// The function may return permissions for more claims than required.
        /// </summary>
        IEnumerable<RolePermissionInfo> GetRolePermissions(IEnumerable<Guid> roleIds, IEnumerable<Guid> claimIds = null);

        /// <summary>
        /// The function may return more items than required.
        /// Note that the result will not include claims that are inactive or do not exist, and that the order of returned items might not match the parameter.
        /// </summary>
        IDictionary<Claim, ClaimInfo> GetClaims(IEnumerable<Claim> requiredClaims = null);
    }

    public enum SystemRole { AllPrincipals, Anonymous };
}
