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

using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Rhetos.AspNetFormsAuth
{
    public class AspNetUserInfo : IUserInfo
    {
        public bool IsUserRecognized { get { return _isUserRecognized.Value; } }
        public string UserName { get { CheckIfUserRecognized(); return _userName.Value; } }
        public string Workstation { get { CheckIfUserRecognized(); return _workstation.Value; } }

        private ILogger _logger;
        private Lazy<bool> _isUserRecognized;
        private Lazy<string> _userName;
        private Lazy<string> _workstation;

        public AspNetUserInfo(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("AspNetUserInfo");

            _isUserRecognized = new Lazy<bool>(() =>
                HttpContext.Current != null
                && HttpContext.Current.User != null
                && HttpContext.Current.User.Identity != null
                && HttpContext.Current.User.Identity.IsAuthenticated);
            _userName = new Lazy<string>(() => HttpContext.Current.User.Identity.Name);
            _workstation = new Lazy<string>(() => WcfUtility.InitClientWorkstation(_logger));
        }

        private void CheckIfUserRecognized()
        {
            if (!IsUserRecognized)
                throw new UserException("User is not authenticated.");
        }
    }
}
