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

namespace Rhetos.Security.Service
{
    [ServiceContract]
    public interface ISecurityRestService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/principals?format=json", ResponseFormat = WebMessageFormat.Json)]
        List<Principal> GetPrincipalsJson();

        [OperationContract]
        [WebGet(UriTemplate = "/principals?format=xml", ResponseFormat = WebMessageFormat.Xml)]
        List<Principal> GetPrincipalsXml();

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/create", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void AddPrincipal(string name);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/{id}/update", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void UpdatePrincipalName(string id, string name);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/{id}/delete")]
        void DeletePrincipal(string id);

        [OperationContract]
        [WebGet(UriTemplate = "/claims?format=json", ResponseFormat = WebMessageFormat.Json)]
        List<Claim> GetClaimsJson();

        [OperationContract]
        [WebGet(UriTemplate = "/claims?format=xml", ResponseFormat = WebMessageFormat.Xml)]
        List<Claim> GetClaimsXml();

        [OperationContract]
        [WebGet(UriTemplate = "/principals/{id}/permissions?format=json", ResponseFormat = WebMessageFormat.Json)]
        List<Permission> GetPrincipalsPermissionsJson(string id);

        [OperationContract]
        [WebGet(UriTemplate = "/principals/{id}/permissions?format=xml", ResponseFormat = WebMessageFormat.Xml)]
        List<Permission> GetPrincipalsPermissionsXml(string id);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/principals/{principalId}/permissions", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void ApplyPrincipalPermission(string principalId, string claimId, string isAuthorized);
    }
}
