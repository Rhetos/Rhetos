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
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Web.Security;
using WebMatrix.WebData;

namespace Rhetos.AspNetFormsAuth
{
    public class LoginParameters
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// "PersistCookie" parameter may be presented to users as the "Remember me" checkbox.
        /// </summary>
        public bool PersistCookie { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new UserException("Empty username is not allowed.");

            if (string.IsNullOrWhiteSpace(Password))
                throw new UserException("Empty password is not allowed.");
        }
    }

    public class SetPasswordParameters
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new UserException("Empty username is not allowed.");

            if (string.IsNullOrWhiteSpace(Password))
                throw new UserException("Empty password is not allowed.");
        }
    }

    public class ChangeMyPasswordParameters
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(OldPassword))
                throw new UserException("Empty old password is not allowed.");

            if (string.IsNullOrWhiteSpace(NewPassword))
                throw new UserException("Empty new password is not allowed.");
        }
    }

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AuthenticationService
    {
        private readonly ILogger _logger;
        private readonly IAuthorizationManager _authorizationManager;

        public AuthenticationService(ILogProvider logProvider, IAuthorizationManager authorizationManager)
        {
            _logger = logProvider.GetLogger("AspNetFormsAuth.AuthenticationService");
            _authorizationManager = authorizationManager;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Login", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool Login(LoginParameters loginData)
        {
            _logger.Trace(() => "Login: " + loginData.UserName);
            loginData.Validate();

            bool success;
            try
            {
                success = WebSecurity.Login(loginData.UserName, loginData.Password, loginData.PersistCookie);
            }
            catch (Exception ex)
            {
                _logger.Info(() => "WebSecurity.Login for " + loginData.UserName + " failed: " + ex);
                success = false;
            }
                
            if (!success)
                _logger.Trace(() => "Login failed: " + loginData.UserName);
            return success;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Logout", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void Logout()
        {
            _logger.Trace(() => "Logout: " + WebSecurity.CurrentUserName);
            WebSecurity.Logout();
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/SetPassword", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void SetPassword(SetPasswordParameters setPasswordData)
        {
            _logger.Trace(() => "SetPassword: " + setPasswordData.UserName);

            bool allowSetPassword = _authorizationManager.GetAuthorizations(new[] { AuthenticationServiceClaims.SetPasswordClaim }).Single();
            if (!allowSetPassword)
                throw new UserException(string.Format(
                    "You are not authorized for action '{0}' on resource '{1}', user '{2}'.",
                    AuthenticationServiceClaims.SetPasswordClaim.Right, AuthenticationServiceClaims.SetPasswordClaim.Resource, WebSecurity.CurrentUserName));

            setPasswordData.Validate();

            try
            {
                WebSecurity.CreateAccount(setPasswordData.UserName, setPasswordData.Password);
                _logger.Trace("Password successfully initialized.");
            }
            catch (MembershipCreateUserException ex)
            {
                if (ex.Message != "The username is already in use.")
                    throw;

                var token = WebSecurity.GeneratePasswordResetToken(setPasswordData.UserName);
                var changed = WebSecurity.ResetPassword(token, setPasswordData.Password);
                if (!changed)
                    throw new UserException("Cannot change password.", "WebSecurity.ResetPassword returned 'false'.");

                _logger.Trace("Password successfully changed.");
            }
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ChangeMyPassword", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool ChangeMyPassword(ChangeMyPasswordParameters changeMyPasswordData)
        {
            _logger.Trace(() => "ChangeMyPassword: " + WebSecurity.CurrentUserName);
            changeMyPasswordData.Validate();
            bool success;
            try
            {
                success = WebSecurity.ChangePassword(WebSecurity.CurrentUserName, changeMyPasswordData.OldPassword, changeMyPasswordData.NewPassword);
                if (!success)
                    _logger.Trace(() => "ChangeMyPassword failed: " + WebSecurity.CurrentUserName);
            }
            catch (Exception ex)
            {
                // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                _logger.Trace(() => "ChangeMyPassword failed: " + WebSecurity.CurrentUserName + ", " + ex);
                success = false;
            }
            return success;
        }
    }
}
