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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Deployment.Test
{
    [TestClass]
    public class ConnectionTestingTest
    {
        [TestMethod]
        public void InvalidConnectionStringFormat()
        {
            string invalidConnectionString = "<ENTER_CONNECTION_STRING_HERE>";

            var ex = TestUtility.ShouldFail<ArgumentException>(
                () => ConnectionTesting.ValidateDbConnection(invalidConnectionString, null),
                "Database connection string has invalid format",
                "ConnectionStrings:RhetosConnectionString");

            TestUtility.AssertNotContains(
                ex.ToString(),
                new[] { invalidConnectionString },
                "The connection string should not be reported in the error message or error log, because it could contain a password.");
        }
    }
}
