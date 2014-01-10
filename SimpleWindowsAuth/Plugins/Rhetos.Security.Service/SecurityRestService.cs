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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Net;
using Rhetos;
using Rhetos.Processing.DefaultCommands;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dom;

namespace Rhetos.Security.Service
{
    [System.ServiceModel.Activation.AspNetCompatibilityRequirements(RequirementsMode = System.ServiceModel.Activation.AspNetCompatibilityRequirementsMode.Allowed)]
    public class SecurityRestService : ISecurityRestService
    {
        private readonly RestImpl _restImpl;

        public SecurityRestService(RestImpl restImpl)
        {
            _restImpl = restImpl;
        }

        public List<Principal> GetPrincipalsJson()
        {
            return _restImpl.GetPrincipals();
        }

        public void AddPrincipal(string name)
        {
            _restImpl.CreatePrincipal(name);
        }

        public void UpdatePrincipalName(string id, string name)
        {
            _restImpl.UpdatePrincipalName(new Guid(id), name);
        }

        public void DeletePrincipal(string id)
        {
            _restImpl.DeletePrincipal(new Guid(id));
        }

        public List<Claim> GetClaimsJson()
        {
            return _restImpl.GetClaims();
        }

        public List<Permission> GetPrincipalsPermissionsJson(string id)
        {
            return _restImpl.GetPermissions(new Guid(id));
        }

        public void ApplyPrincipalPermission(string principalId, string claimId, string isAuthorized)
        {
            _restImpl.ApplyPermissionChange(new Guid(principalId), new Guid(claimId), isAuthorized);
        }
    }
}
