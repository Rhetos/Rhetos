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
using Rhetos.Extensibility;
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
    #region Service parameters

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
                throw new UserException("Empty UserName is not allowed.");

            if (string.IsNullOrWhiteSpace(Password))
                throw new UserException("Empty Password is not allowed.");
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
                throw new UserException("Empty UserName is not allowed.");

            if (string.IsNullOrWhiteSpace(Password))
                throw new UserException("Empty Password is not allowed.");
        }
    }

    public class ChangeMyPasswordParameters
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(OldPassword))
                throw new UserException("Empty OldPassword is not allowed.");

            if (string.IsNullOrWhiteSpace(NewPassword))
                throw new UserException("Empty NewPassword is not allowed.");
        }
    }

    public class UnlockUserParameters
    {
        public string UserName { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new UserException("Empty UserName is not allowed.");
        }
    }

    public class GeneratePasswordResetTokenParameters
    {
        public string UserName { get; set; }

        public const int DefaultTokenExpirationInMinutes = 1440;

        /// <summary>
        /// Optional. If not set (0 value), the DefaultTokenExpirationInMinutes will be used.
        /// </summary>
        public int TokenExpirationInMinutesFromNow { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new UserException("Empty UserName is not allowed.");
        }
    }

    public class SendPasswordResetTokenParameters
    {
        public string UserName { get; set; }

        /// <summary>
        /// Used for future ISendPasswordResetToken extensibility.
        /// For example, AdditionalClientInfo may contain answers to security questions, preferred method of communication or similar user provided information.
        /// </summary>
        public Dictionary<string, string> AdditionalClientInfo { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName))
                throw new UserException("Empty UserName is not allowed.");
        }
    }

    public class ResetPasswordParameters
    {
        public string PasswordResetToken { get; set; }
        public string NewPassword { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(PasswordResetToken))
                throw new UserException("Empty PasswordResetToken is not allowed.");

            if (string.IsNullOrWhiteSpace(NewPassword))
                throw new UserException("Empty NewPassword is not allowed.");
        }
    }

    #endregion

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AuthenticationService
    {
        private readonly ILogger _logger;
        private readonly Lazy<IAuthorizationManager> _authorizationManager;
        private readonly Lazy<IEnumerable<IPasswordStrength>> _passwordStrengthRules;
        private readonly Lazy<IEnumerable<IPasswordAttemptsLimit>> _passwordAttemptsLimits;
        private readonly Lazy<ISqlExecuter> _sqlExecuter;
        private readonly Lazy<ISendPasswordResetToken> _sendPasswordResetTokenPlugin;
        private readonly ILocalizer _localizer;

        public AuthenticationService(
            ILogProvider logProvider,
            Lazy<IAuthorizationManager> authorizationManager,
            GenericRepositories repositories,
            Lazy<ISqlExecuter> sqlExecuter,
            Lazy<IEnumerable<ISendPasswordResetToken>> sendPasswordResetTokenPlugins,
            ILocalizer localizer)
        {
            _logger = logProvider.GetLogger("AspNetFormsAuth.AuthenticationService");
            _authorizationManager = authorizationManager;
            _sqlExecuter = sqlExecuter;
            _sendPasswordResetTokenPlugin = new Lazy<ISendPasswordResetToken>(() => SinglePlugin(sendPasswordResetTokenPlugins));

            _passwordStrengthRules = new Lazy<IEnumerable<IPasswordStrength>>(() => repositories.Load<IPasswordStrength>());
            _passwordAttemptsLimits = new Lazy<IEnumerable<IPasswordAttemptsLimit>>(() =>
                {
                    var limits = repositories.Load<IPasswordAttemptsLimit>();
                    foreach (var limit in limits)
                        if (limit.TimeoutInSeconds == null || limit.TimeoutInSeconds <= 0)
                            limit.TimeoutInSeconds = int.MaxValue;
                    return limits;
                });
            _localizer = localizer;
        }

        private ISendPasswordResetToken SinglePlugin(Lazy<IEnumerable<ISendPasswordResetToken>> plugins)
        {
            if (plugins.Value.Count() == 0)
                throw new UserException("Sending the password reset token is not enabled on this server (the required plugin is not registered).");

            if (plugins.Value.Count() > 1)
                throw new FrameworkException("There is more than one plugin registered for sending the password reset token: "
                    + string.Join(", ", plugins.Value.Select(plugin => plugin.GetType().FullName)) + ".");

            return plugins.Value.Single();
        }

        private void CheckPermissions(Claim claim)
        {
            bool allowed = _authorizationManager.Value.GetAuthorizations(new[] { claim }).Single();
            if (!allowed)
                throw new UserException(
                    "You are not authorized for action '{0}' on resource '{1}', user '{2}'. The required security claim is not set.",
                    new[] { claim.Right, claim.Resource, WebSecurity.CurrentUserName },
                    null, null);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Login", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool Login(LoginParameters parameters)
        {
            if (parameters == null)
                throw new ClientException("It is not allowed to call this authentication service method with no parameters provided.");
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

                        string localizedMessage = _localizer["Your account is temporarily locked out because of too many failed login attempts."];
                        int timeoutMinutes = (int)Math.Ceiling((double)limit.TimeoutInSeconds / 60);
                        if (timeoutMinutes == 1)
                            localizedMessage += " " + _localizer["Please try again in a minute or contact your system administrator."];
                        else if (timeoutMinutes > 1 && timeoutMinutes <= 300)
                            localizedMessage += " " + _localizer["Please try again in {0} minutes or contact your system administrator.", timeoutMinutes];
                        else
                            localizedMessage += " " + _localizer["Please contact your system administrator."];

                        throw new UserException(localizedMessage);
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
            if (parameters == null)
                throw new ClientException("It is not allowed to call this authentication service method with no parameters provided.");
            _logger.Trace(() => "SetPassword: " + parameters.UserName);
            CheckPermissions(AuthenticationServiceClaims.SetPasswordClaim);
            parameters.Validate();
            if (parameters.IgnorePasswordStrengthPolicy)
                CheckPermissions(AuthenticationServiceClaims.IgnorePasswordStrengthPolicyClaim);
            else
                CheckPasswordStrength(parameters.Password);

            if (!WebSecurity.UserExists(parameters.UserName))
                throw new UserException("User '{0}' is not registered.", new[] { parameters.UserName }, null, null); // Providing this information is not a security issue, because this method requires admin credentials (SetPasswordClaim).

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
            if (parameters == null)
                throw new ClientException("It is not allowed to call this authentication service method with no parameters provided.");
            _logger.Trace(() => "ChangeMyPassword: " + WebSecurity.CurrentUserName);
            parameters.Validate();
            CheckPasswordStrength(parameters.NewPassword);

            return SafeExecute(
                () => WebSecurity.ChangePassword(WebSecurity.CurrentUserName, parameters.OldPassword, parameters.NewPassword),
                "ChangeMyPassword", WebSecurity.CurrentUserName);
        }

        private void CheckPasswordStrength(string password)
        {
            foreach (var rule in _passwordStrengthRules.Value)
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
            if (parameters == null)
                throw new ClientException("It is not allowed to call this authentication service method with no parameters provided.");
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
            if (parameters == null)
                throw new ClientException("It is not allowed to call this authentication service method with no parameters provided.");
            _logger.Trace(() => "GeneratePasswordResetToken: " + parameters.UserName);
            CheckPermissions(AuthenticationServiceClaims.GeneratePasswordResetTokenClaim);
            parameters.Validate();

            return GeneratePasswordResetTokenInternal(parameters);
        }

        private string GeneratePasswordResetTokenInternal(GeneratePasswordResetTokenParameters parameters)
        {
            if (!WebSecurity.UserExists(parameters.UserName)) // Providing this information is not a security issue, because this method requires admin credentials (GeneratePasswordResetTokenClaim).
                throw new UserException("User '{0}' is not registered.", new[] { parameters.UserName }, null, null);

            if (!IsAccountCreated(parameters.UserName))
            {
                _logger.Trace(() => "GeneratePasswordResetTokenInternal creating security account: " + parameters.UserName);
                WebSecurity.CreateAccount(parameters.UserName, Guid.NewGuid().ToString());
            }

            return parameters.TokenExpirationInMinutesFromNow != 0
                ? WebSecurity.GeneratePasswordResetToken(parameters.UserName, parameters.TokenExpirationInMinutesFromNow)
                : WebSecurity.GeneratePasswordResetToken(parameters.UserName, GeneratePasswordResetTokenParameters.DefaultTokenExpirationInMinutes);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/SendPasswordResetToken", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public void SendPasswordResetToken(SendPasswordResetTokenParameters parameters)
        {
            if (parameters == null)
                throw new ClientException("It is not allowed to call this authentication service method with no parameters provided.");
            _logger.Trace("SendPasswordResetToken " + parameters.UserName);
            parameters.Validate();

            const string logErrorFormat = "SendPasswordResetToken failed for {0}: {1}";

            try
            {
                string passwordResetToken;
                try
                {
                    var tokenParameters = new GeneratePasswordResetTokenParameters
                    {
                        UserName = parameters.UserName,
                        TokenExpirationInMinutesFromNow = Int32.Parse(ConfigUtility.GetAppSetting("AspNetFormsAuth.SendPasswordResetToken.ExpirationInMinutes") ?? "1440")
                    };
                    passwordResetToken = GeneratePasswordResetTokenInternal(tokenParameters);
                }
                // Providing an error information to the client might be a security issue, because this method allows anonymous access.
                catch (UserException ex)
                {
                    _logger.Trace(logErrorFormat, parameters.UserName, ex);
                    return;
                }
                catch (ClientException ex)
                {
                    _logger.Info(logErrorFormat, parameters.UserName, ex);
                    return;
                }

                // The plugin may choose it's own client error messages (UserException and ClientException will not be suppressed).
                _sendPasswordResetTokenPlugin.Value.SendPasswordResetToken(parameters.UserName, parameters.AdditionalClientInfo, passwordResetToken);
            }
            catch (Exception ex)
            {
                if (ex is UserException || ex is ClientException)
                    ExceptionsUtility.Rethrow(ex);

                // Don't return an internal error to the client. Log it and return a generic error message:
                _logger.Error(logErrorFormat, parameters.UserName, ex);
                throw new FrameworkException(FrameworkException.GetInternalServerErrorMessage(_localizer, ex));
            }
        }

        /// <summary>
        /// This method is similar to SetPassword, but there is a difference in access permissions.
        /// ResetPassword allows anonymous access, while SetPassword needs a specific authorization.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ResetPassword", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public bool ResetPassword(ResetPasswordParameters parameters)
        {
            if (parameters == null)
                throw new ClientException("It is not allowed to call this authentication service method with no parameters provided.");
            _logger.Trace("ResetPassword");
            parameters.Validate();
            CheckPasswordStrength(parameters.NewPassword);

            int userId = WebSecurity.GetUserIdFromPasswordResetToken(parameters.PasswordResetToken);
            SimpleMembershipProvider provider = (SimpleMembershipProvider)Membership.Provider;
            string userName = provider.GetUserNameFromId(userId);
            _logger.Trace(() => "ResetPassword " + userName);

            bool successfulReset = SafeExecute(
                () => WebSecurity.ResetPassword(parameters.PasswordResetToken, parameters.NewPassword),
                "ResetPassword", userName);

            if (successfulReset && !string.IsNullOrEmpty(userName))
                SafeExecute( // Login does not need to be successful for this function to return true.
                    () => { Login(new LoginParameters { UserName = userName, Password = parameters.NewPassword, PersistCookie = false }); },
                    "Login after ResetPassword", userName);

            return successfulReset;
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
