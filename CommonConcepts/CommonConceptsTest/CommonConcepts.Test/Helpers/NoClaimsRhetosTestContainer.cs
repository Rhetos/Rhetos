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

using Autofac;
using Rhetos.Configuration.Autofac;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonConcepts.Test.Helpers
{
    /// <summary>
    /// Overrides deployed IAuthorizationProvider (windows or forms authorization) to turn off checking user security claims.
    /// Not related to row permissions.
    /// </summary>
    public class NoClaimsRhetosTestContainer : RhetosTestContainer
    {
        public NoClaimsRhetosTestContainer(bool commitChanges = false)
            : base(commitChanges: commitChanges)
        {
            this._initializeSession += builder =>
                builder.RegisterType<IgnoreAuthorizationProvider>().As<IAuthorizationProvider>();
        }
    }

    public class IgnoreAuthorizationProvider : IAuthorizationProvider
    {
        public IgnoreAuthorizationProvider() { }

        public IList<bool> GetAuthorizations(IUserInfo userInfo, IList<Claim> requiredClaims)
        {
            return requiredClaims.Select(c => true).ToList();
        }
    }
}
