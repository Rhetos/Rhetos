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
using System.ServiceModel.Activation;

namespace Rhetos.Security.Service
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceContract]
    public class SecurityRestService
    {
        private readonly RestImpl _restImpl;

        public SecurityRestService(RestImpl restImpl)
        {
            _restImpl = restImpl;
        }

        [OperationContract]
        [WebGet(UriTemplate = "/principals?format=json", ResponseFormat = WebMessageFormat.Json)]
        public List<Principal> GetPrincipalsJson()
        {
            return _restImpl.GetPrincipals();
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/create", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void AddPrincipal(string name)
        {
            _restImpl.CreatePrincipal(name);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/{id}/update", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void UpdatePrincipalName(string id, string name)
        {
            _restImpl.UpdatePrincipalName(new Guid(id), name);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/{id}/delete")]
        public void DeletePrincipal(string id)
        {
            _restImpl.DeletePrincipal(new Guid(id));
        }

        [OperationContract]
        [WebGet(UriTemplate = "/claims?format=json", ResponseFormat = WebMessageFormat.Json)]
        public List<Claim> GetClaimsJson()
        {
            return _restImpl.GetClaims();
        }

        [OperationContract]
        [WebGet(UriTemplate = "/principals/{id}/permissions?format=json", ResponseFormat = WebMessageFormat.Json)]
        public List<Permission> GetPrincipalsPermissionsJson(string id)
        {
            return _restImpl.GetPermissions(new Guid(id));
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/{principalId}/permissions", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void ApplyPrincipalPermission(string principalId, string claimId, string isAuthorized)
        {
            _restImpl.ApplyPermissionChange(new Guid(principalId), new Guid(claimId), isAuthorized);
        }
    }
}
