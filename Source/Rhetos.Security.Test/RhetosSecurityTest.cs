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
using Rhetos.Security;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.CommonConcepts.Test
{
    public static class AuthorizationManagerAccessor
    {
        public static List<(string UserName, string HostName)> SplitUserList(string users)
        {
            return TestAccessorHelpers.Invoke<AuthorizationManager>(nameof(SplitUserList), users);
        }
    }

    [TestClass]
    public class RhetosSecurityTest
    {
        [TestMethod]
        public void SplitUserList()
        {
            var tests = new (string Input, string ExpectedOutput)[]
            {
                ("", ""),
                ("a", "a/local"),
                ("a@b", "a@b/local; a/b"),
                ("a,b, c ,a@b,qwer@1234, qwer@1234 ,1@2@3,a@,@b,@", "a/local; b/local; c/local; a@b/local; a/b; qwer@1234/local; qwer/1234; qwer@1234/local; qwer/1234; 1@2@3/local; 1@2/3; a@/local; @b/local; @/local"),
            };

            foreach (var test in tests)
            {
                var users = AuthorizationManagerAccessor.SplitUserList(test.Input);
                string report =
                    string.Join("; ", users.Select(u => $"{u.UserName}/{u.HostName}"))
                    .Replace(Environment.MachineName, "local");
                Assert.AreEqual(test.ExpectedOutput, report);
            }
        }
    }
}
