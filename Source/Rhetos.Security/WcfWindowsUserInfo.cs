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

using System;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Rhetos.Utilities;
using Rhetos.Logging;
using System.Text.RegularExpressions;
using System.Security.Principal;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;

namespace Rhetos.Security
{
    /// <summary>
    /// This is a security principal provider (IUserInfo) based on WCF and Windows authentication.
    /// </summary>
    public class WcfWindowsUserInfo : IUserInfo
    {
        public bool IsUserRecognized { get { return _isUserRecognized.Value; } }
        public string UserName { get { CheckIfUserRecognized(); return _userName.Value; } }
        public string Workstation { get { CheckIfUserRecognized(); return _workstation.Value; } }
        public WindowsIdentity WindowsIdentity { get { CheckIfUserRecognized(); return _windowsIdentity.Value; } }

        private ILogger _logger;
        private ILogger _performanceLogger;

        private Lazy<bool> _isUserRecognized;
        private Lazy<string> _userName;
        private Lazy<string> _workstation;
        private Lazy<WindowsIdentity> _windowsIdentity;
        private Lazy<string> _accountName;

        public WcfWindowsUserInfo(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("WcfWindowsUserInfo");
            _performanceLogger = logProvider.GetLogger("Performance");

            _isUserRecognized = new Lazy<bool>(() => InitIsUserRecognized());
            _userName = new Lazy<string>(() => InitUserName());
            _workstation = new Lazy<string>(() => WcfUtility.InitClientWorkstation(_logger));
            _windowsIdentity = new Lazy<WindowsIdentity>(() => InitWindowsIdentity());
            _accountName = new Lazy<string>(() => InitAccountName());
        }

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new UserException("User is not authenticated.");
        }

        private bool InitIsUserRecognized()
        {
            if (ServiceSecurityContext.Current == null)
            {
                _logger.Trace("User identity not provided, ServiceSecurityContext.Current is null.");
                return false;
            }
            if (ServiceSecurityContext.Current.WindowsIdentity == null)
            {
                _logger.Trace("User identity not provided, ServiceSecurityContext.Current.WindowsIdentity is null.");
                return false;
            }
            return true;
        }

        private string InitUserName()
        {
            string name = ServiceSecurityContext.Current.WindowsIdentity.Name;
            _logger.Trace(() => "User: " + name + ".");
            return name;
        }

        private WindowsIdentity InitWindowsIdentity()
        {
            _logger.Trace(() => "ServiceSecurityContext.Current.WindowsIdentity: " + Report(ServiceSecurityContext.Current.WindowsIdentity));

            // WindowsIdentity.GetCurrent() and ServiceSecurityContext.Current.WindowsIdentity in some scenarios
            // return the same UserName, but different number of system claims. The first one returns if the current user (running the process)
            // is an admin, the other one sometimes doesn't (even if UAC is turned off). Could not find the underlying rules, but it
            // seems that running the application from Visual Studio affects the behavior (among other factors).
            // The first function always returns AuthenticationType=Kerebros, the other always returns Negotiation.

            // Fix the Identity when a Windows domain is used.
            if (_userName.Value.StartsWith(Environment.UserDomainName + @"\", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string userName = _userName.Value.Substring(_userName.Value.IndexOf(@"\") + 1);
                    var windowsIdentity = new WindowsIdentity(userName); // This will throw an exception if the active directory server is not accessable.
                    _logger.Trace(() => "Using new WindowsIdentity(name): " + Report(windowsIdentity));
                    return windowsIdentity;
                }
                catch (Exception ex)
                {
                    _logger.Trace(() => ex.ToString());
                }
            }

            // Fix the Identity when a developer runs the server using its own account (with or without a Windows domain).
            if (_userName.Value == WindowsIdentity.GetCurrent().Name)
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();
                _logger.Trace(() => "Using WindowsIdentity.GetCurrent: " + Report(windowsIdentity));
                return windowsIdentity;
            }

            {
                var windowsIdentity = ServiceSecurityContext.Current.WindowsIdentity;
                _logger.Trace(() => "Using ServiceSecurityContext.Current.WindowsIdentity.");
                return windowsIdentity;
            }
        }

        private static string Report(WindowsIdentity wi)
        {
            string authenticationType;
            try
            {
                authenticationType = wi.AuthenticationType;
            }
            catch
            {
                authenticationType = "unknown authentication type";
            }
            return wi.Name + ", " + authenticationType + ", LocalAdmin=" + new WindowsPrincipal(wi).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private string InitAccountName()
        {
            _logger.Trace(() => "Domain: " + Environment.UserDomainName);

            var domainPrefix = Environment.UserDomainName + "\\";

            if (!_userName.Value.StartsWith(domainPrefix, StringComparison.OrdinalIgnoreCase))
            {
                const string msg = "Current identity is not authenticated in current domain.";
                _logger.Trace(() => msg + " Identity: '" + _userName.Value + "', domain: '" + Environment.UserDomainName + "'.");
                throw new FrameworkException(msg);
            }

            var name = _userName.Value.Substring(domainPrefix.Length);
            _logger.Trace(() => "Identity without domain: " + name);
            return name;
        }

        public bool IsBuiltInAdministrator()
        {
            // WARNING: When making any changes to this function, please make sure that it works correctly when the process is run on IIS with the "ApplicationPoolIdentity".
            if (!_isUserRecognized.Value)
                return false;
            WindowsPrincipal principal = new WindowsPrincipal(_windowsIdentity.Value);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public IList<string> GetIdentityMembership()
        {
            CheckIfUserRecognized();
            var stopwatch = Stopwatch.StartNew();

            // Search user's domain groups:

            var userNestedMembership = new List<string>();

            DirectoryEntry domainConnection = new DirectoryEntry("LDAP://" + Environment.UserDomainName);
            DirectorySearcher searcher = new DirectorySearcher(domainConnection);
            searcher.Filter = "(samAccountName=" + _accountName.Value + ")";
            searcher.PropertiesToLoad.Add("name");

            SearchResult searchResult = searcher.FindOne();
            if (searchResult != null)
            {
                _logger.Trace("Found Active Directory entry: " + searchResult.Path);

                userNestedMembership.Add(_accountName.Value);

                DirectoryEntry theUser = searchResult.GetDirectoryEntry();
                theUser.RefreshCache(new[] { "tokenGroups" });

                foreach (byte[] resultBytes in theUser.Properties["tokenGroups"])
                {
                    // Search domain group's name and displayName:

                    var mySID = new SecurityIdentifier(resultBytes, 0);

                    _logger.Trace(() => string.Format("User '{0}' is a member of group with objectSid '{1}'.", _accountName.Value, mySID.Value));

                    DirectorySearcher sidSearcher = new DirectorySearcher(domainConnection);
                    sidSearcher.Filter = "(objectSid=" + mySID.Value + ")";
                    sidSearcher.PropertiesToLoad.Add("name");
                    sidSearcher.PropertiesToLoad.Add("displayname");

                    SearchResult sidResult = sidSearcher.FindOne();
                    if (sidResult != null)
                    {
                        string name = sidResult.Properties["name"][0].ToString();
                        userNestedMembership.Add(name);
                        _logger.Trace(() => string.Format("Added membership to group with name '{0}' for user '{1}'.", name, _accountName.Value));

                        var displayNameProperty = sidResult.Properties["displayname"];
                        if (displayNameProperty.Count > 0)
                        {
                            string displayName = displayNameProperty[0].ToString();
                            if (!string.Equals(name, displayName))
                            {
                                userNestedMembership.Add(displayName);
                                _logger.Trace(() => string.Format("Added membership to group with display name '{0}' for user '{1}'.", displayName, _accountName.Value));
                            }
                        }
                    }
                    else
                        _logger.Trace(() => string.Format("Cannot find active directory enity for user's '{0}' parent group with objectSid '{1}'.", _accountName.Value, mySID.Value));
                }
            }
            else
                _logger.Trace(() => string.Format("Account name '{0}' not found on Active Directory for domain '{1}'.", _accountName.Value, Environment.UserDomainName));

            _performanceLogger.Write(stopwatch, "DomainPrincipalProvider.GetIdentityMembership() done.");
            return userNestedMembership;
        }
    }
}