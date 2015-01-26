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

using Rhetos.Utilities;
using System;
using System.Security.Principal;

namespace Rhetos.Security
{
    /// <summary>
    /// Used for Rhetos system utilities (DeployPackages.exe, e.g.) when web authentication is not applicable.
    /// </summary>
    public class ProcessUserInfo : IWindowsUserInfo
    {
        private Lazy<WindowsIdentity> _currentUser;

        public ProcessUserInfo()
        {
            _currentUser = new Lazy<WindowsIdentity>(() => WindowsIdentity.GetCurrent());
        }

        #region IWindowsUserInfo interface implementation

        public bool IsUserRecognized
        {
            get { return _currentUser.Value != null; }
        }

        /// <summary>Format: "domain\user"</summary>
        public string UserName
        {
            get { CheckIfUserRecognized(); return _currentUser.Value.Name; }
        }

        public string Workstation
        {
            get { CheckIfUserRecognized(); return System.Environment.MachineName; }
        }

        public WindowsIdentity WindowsIdentity
        {
            get { CheckIfUserRecognized(); return _currentUser.Value; }
        }

        public string Report()
        {
            return UserName + "," + Workstation;
        }

        #endregion

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new ClientException("User is not authenticated.");
        }
    }
}