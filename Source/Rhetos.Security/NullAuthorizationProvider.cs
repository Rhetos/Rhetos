﻿/*
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

using System.Collections.Generic;
using System.Linq;
using Rhetos.Utilities;
using Rhetos.Logging;

namespace Rhetos.Security
{
    public class NullAuthorizationProvider : IAuthorizationProvider
    {
        private readonly ILogger _logger;

        public NullAuthorizationProvider(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("NullAuthorizationProvider");
        }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            _logger.Warning("There is no authorization package installed. Deploy the necessary Rhetos package (for example, CommonConcepts).");

            return requiredClaims.Select(c => false).ToList();
        }
    }
}