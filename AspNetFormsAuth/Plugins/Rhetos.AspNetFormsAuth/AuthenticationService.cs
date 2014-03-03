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

using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;
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
        public bool IgnorePasswordStrengthPolicy { get; set; }

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

    public class UnlockUserParameters
    {
        public string UserName { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new UserException("Empty username is not allowed.");
        }
    }

    public class GeneratePasswordResetTokenParameters
    {
        public string UserName { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new UserException("Empty username is not allowed.");
        }
    }

    public class ResetPasswordParameters
    {
        public string PasswordResetToken { get; set; }
        public string NewPassword { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(NewPassword))
                throw new UserException("Empty new password is not allowed.");
        }
    }

    // TODO: Delete when stateless session is implemented. A separate PasswordAttemptsLimit class is needed to allow editing of the loaded data (TimeoutInSeconds) that is not bound to the ORM. 
    class PasswordAttemptsLimit : IPasswordAttemptsLimit
    {
        public int? MaxInvalidPasswordAttempts { get; set; }
        public int? TimeoutInSeconds { get; set; }
    }

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AuthenticationService
    {
        private readonly ILogger _logger;
        private readonly Lazy<IAuthorizationManager> _authorizationManager;
        private readonly Lazy<IQueryableRepository<IPasswordStrength>> _passwordStrengthRules;
        private readonly Lazy<IList<PasswordAttemptsLimit>> _passwordAttemptsLimits;
        private readonly Lazy<ISqlExecuter> _sqlExecuter;

        public AuthenticationService(
            ILogProvider logProvider,
            Lazy<IAuthorizationManager> authorizationManager,
            Lazy<IQueryableRepository<IPasswordStrength>> passwordStrengthRules,
            Lazy<IQueryableRepository<IPasswordAttemptsLimit>> passwordAttemptsLimitRepository,
            Lazy<ISqlExecuter> sqlExecuter)
        {
            _logger = logProvider.GetLogger("AspNetFormsAuth.AuthenticationService");
            _authorizationManager = authorizationManager;
            _sqlExecuter = sqlExecuter;

            _passwordStrengthRules = passwordStrengthRules;
            _passwordAttemptsLimits = new Lazy<IList<PasswordAttemptsLimit>>(
                () =>
                {
                    var limits = passwordAttemptsLimitRepository.Value.Query()
                        .Select(l => new PasswordAttemptsLimit { MaxInvalidPasswordAttempts = l.MaxInvalidPasswordAttempts, TimeoutInSeconds = l.TimeoutInSeconds })
                        .ToList();
                    foreach (var limit in limits)
                        if (limit.TimeoutInSeconds == null || limit.TimeoutInSeconds <= 0)
                            limit.TimeoutInSeconds = int.MaxValue;
                    return limits;
                });
        }

        private void CheckPermissions(Claim claim)
        {
            bool allowed = _authorizationManager.Value.GetAuthorizations(new[] { claim }).Single();
            if (!allowed)
                throw new UserException(string.Format(
                    "You are not authorized for action '{0}' on resource '{1}', user '{2}'.",
                    claim.Right, claim.Resource, WebSecurity.CurrentUserName));
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Login", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool Login(LoginParameters parameters)
        {
            _logger.Trace(() => "Login: " + parameters.UserName);
            parameters.Validate();
            CheckPasswordFailuresBeforeLogin(parameters.UserName);

            return SafeExecute(
                () => WebSecurity.Login(parameters.UserName, parameters.Password, parameters.PersistCookie),
                "Login", parameters.UserName);
        }

        private void CheckPasswordFailuresBeforeLogin(string userName)
        {
            foreach (var limit in _passwordAttemptsLimits.Value.OrderByDescending(l => l.TimeoutInSeconds))
            {
                int maxAttempts = limit.MaxInvalidPasswordAttempts ?? 0;
                if (maxAttempts > 0)
                    if (WebSecurity.IsAccountLockedOut(userName, maxAttempts - 1, limit.TimeoutInSeconds.Value)) // MaxInvalidPasswordAttempts value is corrected by 1 to work correctly.
                    {
                        _logger.Trace(() => "Account locked out: " + userName + ", attempts "
                            + WebSecurity.GetPasswordFailuresSinceLastSuccess(userName) + "/" + limit.MaxInvalidPasswordAttempts
                            + ", timeout " + limit.TimeoutInSeconds + ".");

                        string userMessage = "Your account is temporarily locked out because of too many failed login attempts.";
                        int timeoutMinutes = (int)Math.Ceiling((double)limit.TimeoutInSeconds / 60);
                        if (timeoutMinutes == 1)
                            userMessage += " Please try again in a minute or contact your system administrator.";
                        else if (timeoutMinutes > 1 && timeoutMinutes <= 300)
                            userMessage += " Please try again in " + timeoutMinutes + " minutes or contact your system administrator.";
                        else
                            userMessage += " Please contact your system administrator.";

                        throw new UserException(userMessage);
                    }
            }
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Logout", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void Logout()
        {
            _logger.Trace(() => "Logout: " + WebSecurity.CurrentUserName);

            SafeExecute(() => WebSecurity.Logout(), "Logout", WebSecurity.CurrentUserName);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/SetPassword", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void SetPassword(SetPasswordParameters parameters)
        {
            _logger.Trace(() => "SetPassword: " + parameters.UserName);
            CheckPermissions(AuthenticationServiceClaims.SetPasswordClaim);
            parameters.Validate();
            if (parameters.IgnorePasswordStrengthPolicy)
                CheckPermissions(AuthenticationServiceClaims.IgnorePasswordStrengthPolicyClaim);
            else
                CheckPasswordStrength(parameters.Password);

            if (!WebSecurity.UserExists(parameters.UserName))
                throw new UserException("User '" + parameters.UserName + "' is not registered.");

            if (!IsAccountCreated(parameters.UserName))
            {
                WebSecurity.CreateAccount(parameters.UserName, parameters.Password);
                _logger.Trace("Password successfully initialized.");
            }
            else
            {
                var token = WebSecurity.GeneratePasswordResetToken(parameters.UserName);
                var changed = WebSecurity.ResetPassword(token, parameters.Password);
                if (!changed)
                    throw new UserException("Cannot change password.", "WebSecurity.ResetPassword returned 'false'.");
                _logger.Trace("Password successfully changed.");
            }
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ChangeMyPassword", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool ChangeMyPassword(ChangeMyPasswordParameters parameters)
        {
            _logger.Trace(() => "ChangeMyPassword: " + WebSecurity.CurrentUserName);
            parameters.Validate();
            CheckPasswordStrength(parameters.NewPassword);

            return SafeExecute(
                () => WebSecurity.ChangePassword(WebSecurity.CurrentUserName, parameters.OldPassword, parameters.NewPassword),
                "ChangeMyPassword", WebSecurity.CurrentUserName);
        }

        private void CheckPasswordStrength(string password)
        {
            foreach (var rule in _passwordStrengthRules.Value.Query().ToList())
            {
                var regex = new Regex(rule.RegularExpression);
                if (!regex.IsMatch(password))
                {
                    _logger.Trace("CheckPasswordStrength failed on regular expression '" + rule.RegularExpression + "'.");
                    throw new UserException(rule.RuleDescription);
                }
            }
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/UnlockUser", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void UnlockUser(UnlockUserParameters parameters)
        {
            _logger.Trace(() => "UnlockUser: " + parameters.UserName);
            CheckPermissions(AuthenticationServiceClaims.UnlockUserClaim);
            parameters.Validate();

            string sql = string.Format(UnlockUserSql, SqlUtility.QuoteText(parameters.UserName));
            _sqlExecuter.Value.ExecuteSql(new[] { sql });
        }

        private const string UnlockUserSql = @"UPDATE
                wm
            SET
                PasswordFailuresSinceLastSuccess = 0
            FROM
                webpages_Membership wm
                INNER JOIN Common.Principal p ON p.AspNetUserId = wm.UserId
            WHERE
                p.Name = {0}";

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/GeneratePasswordResetToken", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public string GeneratePasswordResetToken(GeneratePasswordResetTokenParameters parameters)
        {
            _logger.Trace(() => "GeneratePasswordResetToken: " + parameters.UserName);
            CheckPermissions(AuthenticationServiceClaims.GeneratePasswordResetTokenClaim);
            parameters.Validate();

            var id = WebSecurity.GetUserId(parameters.UserName);

            if (!IsAccountCreated(parameters.UserName))
            {
                _logger.Trace(() => "GeneratePasswordResetToken creating account: " + parameters.UserName);
                WebSecurity.CreateAccount(parameters.UserName, Guid.NewGuid().ToString());
            }
                
            return WebSecurity.GeneratePasswordResetToken(parameters.UserName);
        }

        /// <summary>
        /// This methos is similar to SetPassword, but the difference is in access permissions.
        /// ResetPassword allows anonymous access, while SetPassword needs a specific authorization.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ResetPassword", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool ResetPassword(ResetPasswordParameters parameters)
        {
            _logger.Trace(() => "ResetPassword: " + parameters.PasswordResetToken);
            parameters.Validate();
            CheckPasswordStrength(parameters.NewPassword);

            return SafeExecute(
                () => WebSecurity.ResetPassword(parameters.PasswordResetToken, parameters.NewPassword),
                "ResetPassword", parameters.PasswordResetToken);
        }

        //==================================================

        bool IsAccountCreated(string userName)
        {
            string sql = string.Format(IsAccountCreatedSql, SqlUtility.QuoteText(userName));
            bool exists = false;
            _sqlExecuter.Value.ExecuteReader(sql, reader => exists = true);
            return exists;
        }

        const string IsAccountCreatedSql =
            @"SELECT TOP 1 1 FROM Common.Principal cp
                INNER JOIN webpages_Membership wm
                    ON wm.UserId = cp.AspNetUserId
            WHERE cp.Name = {0}";

        bool SafeExecute(Func<bool> action, string actionName, string context)
        {
            bool success;
            try
            {
                success = action();
                if (!success)
                    _logger.Trace(() => actionName + " failed: " + context);
            }
            catch (Exception ex)
            {
                success = false;
                _logger.Info(() => actionName + " failed: " + context + ", " + ex);
            }
            return success;
        }

        bool SafeExecute(Action action, string actionName, string context)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Info(() => actionName + " failed: " + context + ", " + ex);
                return false;
            }
        }
    }
}
