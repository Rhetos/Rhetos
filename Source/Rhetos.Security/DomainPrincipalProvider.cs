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
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;
using System.ServiceModel;
using Rhetos.Logging;
using System.Text;

namespace Rhetos.Security
{
    public class DomainPrincipalProvider : IPrincipalProvider
    {
        public string Identity { get; private set; }
        private WindowsIdentity _windowsIdentity;

        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public DomainPrincipalProvider(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("DomainPrincipalProvider");
            _performanceLogger = logProvider.GetLogger("Performance");

            var id = GetIdentity(_logger);
            Identity = id.Item1;
            _windowsIdentity = id.Item2;
        }

        private static Tuple<string, WindowsIdentity> GetIdentity(ILogger logger)
        {
            if (ServiceSecurityContext.Current == null)
            {
                logger.Trace(() => "Identity not provided, ServiceSecurityContext.Current == null.");
                return Tuple.Create<string, WindowsIdentity>("<not provided>", null);
            }

            var serviceSecurityContextIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            var identityName = serviceSecurityContextIdentity.Name;

            logger.Trace(() => "ServiceSecurityContext.Current.WindowsIdentity: " + Report(serviceSecurityContextIdentity));

            // WindowsIdentity.GetCurrent() and ServiceSecurityContext.Current.WindowsIdentity in some scenarios
            // return the same UserName, but different number of system claims. The first one returns if the current user (running the process)
            // is an admin, the other one sometimes doesn't (even if UAC is turned off). Could not find the underlying rules, but it
            // seems that running the application from Visual Studio affects the behavior (among other factors).
            // The first function always returns AuthenticationType=Kerebros, the other always returns Negotiation.

            // Fix the Identity when a Windows domain is used.
            if (identityName.StartsWith(Environment.UserDomainName + @"\", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string userName = identityName.Substring(identityName.IndexOf(@"\") + 1);
                    var windowsIdentity = new WindowsIdentity(userName); // This will throw an exception if the active directory server is not accessable.
                    logger.Trace(() => "Using new WindowsIdentity(name): " + Report(windowsIdentity));
                    return Tuple.Create(identityName, windowsIdentity);
                }
                catch (Exception ex)
                {
                    logger.Trace(() => ex.ToString());
                }
            }

            // Fix the Identity when a developer runs the server using its own account (with or without a Windows domain).
            if (identityName == WindowsIdentity.GetCurrent().Name)
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();
                logger.Trace(() => "Using WindowsIdentity.GetCurrent: " + Report(windowsIdentity));
                return Tuple.Create(identityName, windowsIdentity);
            }

            {
                var windowsIdentity = serviceSecurityContextIdentity;
                logger.Trace(() => "Using ServiceSecurityContext.Current.WindowsIdentity.");
                return Tuple.Create(identityName, windowsIdentity);
            }
        }

        private static string Report(WindowsIdentity wi)
        {
            string authenticationType = "unknown authentication type";
            try
            {
                authenticationType = wi.AuthenticationType;
            }
            catch
            {
            }
            return wi.Name + ", " + authenticationType + ", LocalAdmin=" + new WindowsPrincipal(wi).IsInRole(WindowsBuiltInRole.Administrator);
        }

        public bool IsBuiltInAdministrator()
        {
            // WARNING: When making any changes to this function, please make sure that it works correctly when the process is run on IIS with the "ApplicationPoolIdentity".
            WindowsPrincipal principal = new WindowsPrincipal(_windowsIdentity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private string _domainName;
        private string _userName;

        private void InitializeUserNameAndDomainName()
        {
            if (_userName == null)
            {
                _domainName = Environment.UserDomainName;
                _logger.Trace(() => "Domain: " + _domainName);

                if (!Identity.StartsWith(_domainName + "\\", StringComparison.OrdinalIgnoreCase))
                {
                    const string msg = "Current identity is not authenticated in current domain.";
                    _logger.Trace(() => msg + " Identity: '" + Identity + "', domain: '" + _domainName + "'.");
                    throw new FrameworkException(msg);
                }

                _userName = Identity.Substring(Identity.IndexOf("\\") + 1);
                _logger.Trace(() => "Identity without domain: " + _userName);
            }
        }

        public IEnumerable<string> GetIdentityMembership()
        {
            var stopwatch = Stopwatch.StartNew();

            InitializeUserNameAndDomainName();

            // Search user's domain groups:

            var userNestedMembership = new List<string>();

            DirectoryEntry domainConnection = new DirectoryEntry("LDAP://" + _domainName);
            DirectorySearcher searcher = new DirectorySearcher(domainConnection);
            searcher.Filter = "(samAccountName=" + _userName + ")";
            searcher.PropertiesToLoad.Add("name");

            SearchResult searchResult = searcher.FindOne();
            if (searchResult != null)
            {
                _logger.Trace("Found Active Directory entry: " + searchResult.Path);

                userNestedMembership.Add(_userName);

                DirectoryEntry theUser = searchResult.GetDirectoryEntry();
                theUser.RefreshCache(new [] { "tokenGroups" });

                foreach (byte[] resultBytes in theUser.Properties["tokenGroups"])
                {
                    // Search domain group's name and displayName:

                    var mySID = new SecurityIdentifier(resultBytes, 0);

                    _logger.Trace(() => string.Format("User '{0}' is a member of group with objectSid '{1}'.", _userName, mySID.Value));

                    DirectorySearcher sidSearcher = new DirectorySearcher(domainConnection);
                    sidSearcher.Filter = "(objectSid=" + mySID.Value + ")";
                    sidSearcher.PropertiesToLoad.Add("name");
                    sidSearcher.PropertiesToLoad.Add("displayname");

                    SearchResult sidResult = sidSearcher.FindOne();
                    if (sidResult != null)
                    {
                        string name = sidResult.Properties["name"][0].ToString();
                        userNestedMembership.Add(name);
                        _logger.Trace(() => string.Format("Added membership to group with name '{0}' for user '{1}'.", name, _userName));

                        var displayNameProperty = sidResult.Properties["displayname"];
                        if (displayNameProperty.Count > 0)
                        {
                            string displayName = displayNameProperty[0].ToString();
                            if (!string.Equals(name, displayName))
                            {
                                userNestedMembership.Add(displayName);
                                _logger.Trace(() => string.Format("Added membership to group with display name '{0}' for user '{1}'.", displayName, _userName));
                            }
                        }
                    }
                    else
                        _logger.Trace(() => string.Format("Cannot find active directory enity for user's '{0}' parent group with objectSid '{1}'.", _userName, mySID.Value));
                }
            }
            else
                _logger.Trace(() => string.Format("Account name '{0}' not found on Active Directory for domain '{1}'.", _userName, _domainName));

            _performanceLogger.Write(stopwatch, "DomainPrincipalProvider.GetIdentityMembership() done.");
            return userNestedMembership;
        }
    }
}