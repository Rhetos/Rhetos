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
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using WebMatrix.WebData;

namespace Rhetos.AspNetFormsAuth
{
    public class LoginData
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool PersistCookie { get; set; }
    }

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AuthenticationService
    {
        private readonly ILogger _logger;

        public AuthenticationService(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("AspNetFormsAuth.AuthenticationService");
        }

        /// <summary>
        /// "PersistCookie" parameter may be represented to users as the "Remember me" checkbox.
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Login", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool Login(LoginData loginData)
        {
            // TODO: Check password policy (length, strength, ...)

            bool success = WebSecurity.Login(loginData.UserName, loginData.Password, loginData.PersistCookie);

            _logger.Trace(() => "User '" + loginData.UserName + "' login " + (success ? "successful." : "failed."));
            return success;
        }
    }
}
