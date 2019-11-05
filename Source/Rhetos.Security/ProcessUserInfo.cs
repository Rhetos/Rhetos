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
    public class ProcessUserInfo : IUserInfoAdmin
    {
        private readonly Lazy<WindowsIdentity> _currentUser;
        private readonly IWindowsSecurity _windowsSecurity;

        public ProcessUserInfo(IWindowsSecurity windowsSecurity)
        {
            _currentUser = new Lazy<WindowsIdentity>(() => WindowsIdentity.GetCurrent());
            _windowsSecurity = windowsSecurity;
        }

        #region IUserInfoAdmin interface implementation

        public bool IsUserRecognized => _currentUser.Value != null;

        /// <summary>Format: "domain\user"</summary>
        public string UserName
        {
            get { CheckIfUserRecognized(); return _currentUser.Value.Name; }
        }

        public string Workstation
        {
            get { CheckIfUserRecognized(); return Environment.MachineName; }
        }

        public string Report() => UserName + "," + Workstation;

        public bool IsBuiltInAdministrator => IsUserRecognized && _windowsSecurity.IsBuiltInAdministrator(_currentUser.Value);

        #endregion

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new ClientException("User is not authenticated.");
        }
    }
}