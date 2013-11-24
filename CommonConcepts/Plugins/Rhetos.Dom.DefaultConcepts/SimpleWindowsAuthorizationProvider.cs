/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Security;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IAuthorizationProvider))]
    public class SimpleWindowsAuthorizationProvider : IAuthorizationProvider
    {
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly Lazy<IPermissionLoader> _permissionLoader;

        public SimpleWindowsAuthorizationProvider(
            IDomainObjectModel domainObjectModel,
            Lazy<IPermissionLoader> permissionLoader)
        {
            _domainObjectModel = domainObjectModel;
            _permissionLoader = permissionLoader;
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            IList<string> membership = ((WcfWindowsUserInfo)userInfo).GetIdentityMembership();

            // Force-load domain object model:
            var objectModel = _domainObjectModel.ObjectModel;

            IList<IPermission> permissions = _permissionLoader.Value.LoadPermissions(requiredClaims, membership);

            HashSet<string> hasClaims = new HashSet<string>();
            foreach (IPermission permission in permissions)
                if (permission.IsAuthorized.Value)
                    hasClaims.Add(permission.ClaimResource + "." + permission.ClaimRight);
            foreach (IPermission permission in permissions)
                if (!permission.IsAuthorized.Value)
                    hasClaims.Remove(permission.ClaimResource + "." + permission.ClaimRight);

            return requiredClaims.Select(requiredClaim => hasClaims.Contains(requiredClaim.FullName)).ToArray();
        }
    }
}
