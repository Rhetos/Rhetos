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
    /// This is a security principal provider based on WCF and Windows authentication.
    /// </summary>
    public class WcfWindowsUserInfo : IUserInfoAdmin
    {
        #region IUserInfoAdmin implementation

        public bool IsUserRecognized { get { return _isUserRecognized.Value; } }
        public string UserName { get { CheckIfUserRecognized(); return _userName.Value; } }
        public string Workstation { get { CheckIfUserRecognized(); return _workstation.Value; } }
        public string Report() { return UserName + "," + Workstation; }
        public bool IsBuiltInAdministrator => IsUserRecognized && _windowsSecurity.IsBuiltInAdministrator(_windowsIdentity.Value);

        #endregion

        private readonly ILogger _logger;
        private readonly IWindowsSecurity _windowsSecurity;
        private readonly Lazy<bool> _isUserRecognized;
        /// <summary>Format: "domain\user"</summary>
        private readonly Lazy<string> _userName;
        private readonly Lazy<string> _workstation;
        private readonly Lazy<WindowsIdentity> _windowsIdentity;

        public WcfWindowsUserInfo(ILogProvider logProvider, IWindowsSecurity windowsSecurity)
        {
            _logger = logProvider.GetLogger(GetType().Name);

            _isUserRecognized = new Lazy<bool>(() => InitIsUserRecognized());
            _userName = new Lazy<string>(() => InitUserName());
            _workstation = new Lazy<string>(() => windowsSecurity.GetClientWorkstation());
            _windowsIdentity = new Lazy<WindowsIdentity>(() => InitWindowsIdentity());
            _windowsSecurity = windowsSecurity;
        }

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new ClientException("User is not authenticated.")
                {
                    HttpStatusCode = HttpStatusCode.Unauthorized
                };
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
                    var windowsIdentity = new WindowsIdentity(userName); // This will throw an exception if the active directory server is not accessible.
                    _logger.Trace(() => "Using new WindowsIdentity(name): " + Report(windowsIdentity));
                    return windowsIdentity;
                }
                catch (Exception ex)
                {
                    _logger.Trace(ex.ToString);
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
    }
}