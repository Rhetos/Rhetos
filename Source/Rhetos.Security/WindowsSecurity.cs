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
    /// A utility class for common WCF and Active directory operations.
    /// </summary>
    public class WindowsSecurity : IWindowsSecurity
    {
        private ILogger _logger;
        private ILogger _performanceLogger;
        private readonly RhetosAppOptions _rhetosAppOptions;

        public WindowsSecurity(ILogProvider logProvider, RhetosAppOptions rhetosAppOptions)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance");
            _rhetosAppOptions = rhetosAppOptions;
        }

        public string GetClientWorkstation()
        {
            var stopwatch = Stopwatch.StartNew();
            RemoteEndpointMessageProperty endpointInfo;
            try
            {
                var endpointName = RemoteEndpointMessageProperty.Name;
                var currentContext = OperationContext.Current;
                endpointInfo = currentContext?.IncomingMessageProperties[endpointName] as RemoteEndpointMessageProperty;
            }
            catch (Exception ex)
            {
                _logger.Error("Cannot obtain client host address: " + ex);
                return "Cannot obtain client host address";
            }

            if (endpointInfo == null)
            {
                _logger.Error("Cannot obtain client host address: OperationContext RemoteEndpointMessageProperty is null.");
                return "Cannot obtain client host address";
            }

            _logger.Trace(() => "RemoteEndpointMessageProperty: address " + endpointInfo.Address + ", port " + endpointInfo.Port);

            string name = null;

            if (_rhetosAppOptions.Security__LookupClientHostname)
            {
                name = GetNameFromAddress(endpointInfo.Address, endpointInfo.Port);

                if (string.IsNullOrEmpty(name))
                {
                    string ipv4 = IPv4FromIPv6.Match(endpointInfo.Address).Groups[1].Value;
                    if (!string.IsNullOrEmpty(ipv4))
                    {
                        _logger.Trace(() => "Extracted IPv4 address: " + ipv4);
                        name = GetNameFromAddress(ipv4);
                    }
                }
            }

            if (string.IsNullOrEmpty(name))
                name = endpointInfo.Address + " port " + endpointInfo.Port;

            _performanceLogger.Write(stopwatch, "DomainPrincipalProvider.GetClientWorkstation");
            _logger.Trace(() => "Workstation: " + name + ".");
            return name;
        }

        private static readonly Regex IPv4FromIPv6 = new Regex(@":(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})$", RegexOptions.Compiled);

        private string GetNameFromAddress(string address, int? port = null)
        {
            try
            {
                var clientAddress = IPAddress.Parse(address);
                var dnsEntry = Dns.GetHostEntry(clientAddress);
                if (IPAddress.IsLoopback(clientAddress) || dnsEntry.AddressList.Contains(clientAddress))
                    return dnsEntry.HostName;

                // It seems that "nbtstat.exe -A <ipaddress>" can return up to date values while 'nslookup.exe <ipaddress>' and Dns.GetHostEntry(ipaddress) return old values.
                _logger.Trace(() => "Cannot obtain client host name. DSN lookup returned a DSN entry that does not match given IP address. Check if 'nslookup.exe " + clientAddress + "' returns up-to-date values.");
                if (port.HasValue)
                    return address + " port " + port;
                return address;
            }
            catch (Exception ex)
            {
                _logger.Trace(() => "Cannot obtain client host name for " + address + ". " + ex);
                return null;
            }
        }

        public bool IsBuiltInAdministrator(WindowsIdentity windowsIdentity)
        {
            // WARNING: When making any changes to this function, please make sure that it works correctly when the process is run on IIS with the "ApplicationPoolIdentity".

            WindowsPrincipal principal = new WindowsPrincipal(windowsIdentity);

            // Returns true if user is a local administrator AND the process is running under elevated privileges.
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Queries Active Directory server to retrieve the user's Windows domain groups.
        /// Throws an exception if the username does not have the current domain prefix.
        /// Returns null if the user is not found on Active Directory (returns empty list is the user exists, but has no membership records).
        /// </summary>
        public IEnumerable<string> GetIdentityMembership(string username)
        {
            var stopwatch = Stopwatch.StartNew();

            string accountName = RemoveDomainPrefix(username);

            // Search user's domain groups:

            var userNestedMembership = new List<string>();

            DirectoryEntry domainConnection = new DirectoryEntry("LDAP://" + Environment.UserDomainName);
            DirectorySearcher searcher = new DirectorySearcher(domainConnection);
            searcher.Filter = "(samAccountName=" + accountName + ")";
            searcher.PropertiesToLoad.Add("name");

            SearchResult searchResult = null;
            try
            {
                searchResult = searcher.FindOne();
            }
            catch (Exception ex)
            {
                throw new FrameworkException("Active Directory server is not available. To run Rhetos under IISExpress without AD: a) IISExpress must be run as administrator, b) user connecting to Rhetos service must be local administrator, c) 'BuiltinAdminOverride' must be set to 'True' in config file.", ex);
            }

            if (searchResult != null)
            {
                _logger.Trace("Found Active Directory entry: " + searchResult.Path);

                userNestedMembership.Add(accountName);

                DirectoryEntry theUser = searchResult.GetDirectoryEntry();
                theUser.RefreshCache(new[] { "tokenGroups" });

                foreach (byte[] resultBytes in theUser.Properties["tokenGroups"])
                {
                    // Search domain group's name and displayName:

                    var mySID = new SecurityIdentifier(resultBytes, 0);

                    _logger.Trace(() => string.Format("User '{0}' is a member of group with objectSid '{1}'.", accountName, mySID.Value));

                    DirectorySearcher sidSearcher = new DirectorySearcher(domainConnection);
                    sidSearcher.Filter = "(objectSid=" + mySID.Value + ")";
                    sidSearcher.PropertiesToLoad.Add("name");
                    sidSearcher.PropertiesToLoad.Add("displayname");

                    SearchResult sidResult = sidSearcher.FindOne();
                    if (sidResult != null)
                    {
                        string name = sidResult.Properties["name"][0].ToString();
                        userNestedMembership.Add(name);
                        _logger.Trace(() => string.Format("Added membership to group with name '{0}' for user '{1}'.", name, accountName));

                        var displayNameProperty = sidResult.Properties["displayname"];
                        if (displayNameProperty.Count > 0)
                        {
                            string displayName = displayNameProperty[0].ToString();
                            if (!string.Equals(name, displayName))
                            {
                                userNestedMembership.Add(displayName);
                                _logger.Trace(() => string.Format("Added membership to group with display name '{0}' for user '{1}'.", displayName, accountName));
                            }
                        }
                    }
                    else
                        _logger.Trace(() => string.Format("Cannot find the active directory entry for user's '{0}' parent group with objectSid '{1}'.", accountName, mySID.Value));
                }
            }
            else
                _logger.Trace(() => string.Format("Account name '{0}' not found on Active Directory for domain '{1}'.", accountName, Environment.UserDomainName));

            _performanceLogger.Write(stopwatch, "DomainPrincipalProvider.GetIdentityMembership() done.");
            return userNestedMembership;
        }

        /// <summary>
        /// Throws an exception if the username does not have the current domain prefix.
        /// </summary>
        private string RemoveDomainPrefix(string username)
        {
            _logger.Trace(() => "Domain: " + Environment.UserDomainName);

            var domainPrefix = Environment.UserDomainName + "\\";

            if (!username.StartsWith(domainPrefix, StringComparison.OrdinalIgnoreCase))
            {
                const string msg = "The user is not authenticated in current domain.";
                _logger.Trace(() => msg + " Identity: '" + username + "', domain: '" + Environment.UserDomainName + "'.");
                throw new ClientException(msg);
            }

            string usernameWithoutDomain = username.Substring(domainPrefix.Length);
            _logger.Trace(() => "Identity without domain: " + usernameWithoutDomain);
            return usernameWithoutDomain;
        }
    }
}