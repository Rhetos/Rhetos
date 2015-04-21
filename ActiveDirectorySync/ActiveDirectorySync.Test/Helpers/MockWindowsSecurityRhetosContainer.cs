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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveDirectorySync.Test.Helpers
{
    public class MockWindowsSecurityRhetosContainer : RhetosTestContainer
    {
        const string RhetosServerFolder = @"..\..\..\..\Source\Rhetos";

        public MockWindowsSecurityRhetosContainer(string userGroupMembership, bool commitChanges = false)
            : base(commitChanges, RhetosServerFolder)
        {
            _initializeSession += builder =>
            {
                builder.RegisterInstance(new MockWindowsSecurity(userGroupMembership)).As<IWindowsSecurity>();
                // Test the CommonAuthorizationProvider even if another security package was deployed:
                builder.RegisterType<CommonAuthorizationProvider>();
            };
        }
    }
}
