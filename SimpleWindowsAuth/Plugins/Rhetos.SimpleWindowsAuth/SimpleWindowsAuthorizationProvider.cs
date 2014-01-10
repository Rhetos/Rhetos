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

namespace Rhetos.SimpleWindowsAuth
{
    [Export(typeof(IAuthorizationProvider))]
    public class SimpleWindowsAuthorizationProvider : IAuthorizationProvider
    {
        private readonly Lazy<IPermissionLoader> _permissionLoader;

        public SimpleWindowsAuthorizationProvider(
            Lazy<IPermissionLoader> permissionLoader)
        {
            _permissionLoader = permissionLoader;
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            IList<string> userMembership = ((WcfWindowsUserInfo)userInfo).GetIdentityMembership();
            IList<IPermissionBrowse> userPermissions = _permissionLoader.Value.LoadPermissions(requiredClaims, userMembership);

            HashSet<string> hasClaims = new HashSet<string>();
            foreach (IPermissionBrowse permission in userPermissions)
                if (permission.IsAuthorized.Value)
                    hasClaims.Add(permission.ClaimResource + "." + permission.ClaimRight);
            foreach (IPermissionBrowse permission in userPermissions)
                if (!permission.IsAuthorized.Value)
                    hasClaims.Remove(permission.ClaimResource + "." + permission.ClaimRight);

            return requiredClaims.Select(requiredClaim => hasClaims.Contains(requiredClaim.FullName)).ToArray();
        }
    }
}
