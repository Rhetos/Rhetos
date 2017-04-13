using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Rhetos.Security
{
    public class WindowAuthUserInfo : IWindowsUserInfo
    {
        #region IWindowsUserInfo implementation

        public bool IsUserRecognized { get { return _isUserRecognized.Value; } }
        public string UserName { get { CheckIfUserRecognized(); return _userName.Value; } }
        public string Workstation { get { CheckIfUserRecognized(); return _workstation.Value; } }
        public WindowsIdentity WindowsIdentity { get { CheckIfUserRecognized(); return _windowsIdentity.Value; } }
        public string Report() { return UserName + "," + Workstation; }

        #endregion

        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        private Lazy<bool> _isUserRecognized;
        /// <summary>Format: "domain\user"</summary>
        private Lazy<string> _userName;
        private Lazy<string> _workstation;
        private Lazy<WindowsIdentity> _windowsIdentity;

        public WindowAuthUserInfo(ILogProvider logProvider, IWindowsSecurity windowsSecurity)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance");

            _isUserRecognized = new Lazy<bool>(() => InitIsUserRecognized());
            _userName = new Lazy<string>(() => InitUserName());
            _workstation = new Lazy<string>(() => windowsSecurity.GetClientWorkstation());
            _windowsIdentity = new Lazy<WindowsIdentity>(() => InitWindowsIdentity());
        }

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new ClientException("User is not authenticated.");
        }

        private bool InitIsUserRecognized()
        {
            if (HttpContext.Current == null)
            {
                _logger.Trace("User identity not provided, HttpContext.Current is null.");
                return false;
            }
            if (HttpContext.Current.User.Identity == null)
            {
                _logger.Trace("User identity not provided, ServiceSecurityContext.Current.WindowsIdentity is null.");
                return false;
            }
            return true;
        }

        private string InitUserName()
        {
            string name = HttpContext.Current.User.Identity.Name;
            _logger.Trace(() => "User: " + name + ".");
            return name;
        }

        private WindowsIdentity InitWindowsIdentity()
        {
            _logger.Trace(() => "HttpContext.Current.User.Identity: " + Report((WindowsIdentity)HttpContext.Current.User.Identity));

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
                var windowsIdentity = (WindowsIdentity)HttpContext.Current.User.Identity;
                _logger.Trace(() => "Using HttpContext.Current.User.Identity.");
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
