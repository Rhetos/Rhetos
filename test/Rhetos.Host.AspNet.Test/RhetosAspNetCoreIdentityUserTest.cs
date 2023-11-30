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

using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using System;

namespace Rhetos.Host.AspNet.Test
{
    [TestClass]
    public class RhetosAspNetCoreIdentityUserTest
    {
        [TestMethod]
        public void SimpleUser()
        {
            IHttpContextAccessor httpContextAccessor = new FakeHttpContextAccessor("Bob", "1.2.3.4", 123);
            var userInfo = new RhetosAspNetCoreIdentityUser(httpContextAccessor);
            Assert.AreEqual(
                "IsUserRecognized: True" +
                ", UserName: Bob" +
                ", Workstation: 1.2.3.4 port 123" +
                ", Report: Bob,1.2.3.4 port 123",
                GenerateReport(userInfo));
        }
        
        [TestMethod]
        public void AnonymousUserNull()
        {
            IHttpContextAccessor httpContextAccessor = new FakeHttpContextAccessor(null, null, 0);
            var userInfo = new RhetosAspNetCoreIdentityUser(httpContextAccessor);
            Assert.AreEqual(
                "IsUserRecognized: False" +
                ", UserName: ClientException: This operation is not supported for anonymous user." +
                ", Workstation: " +
                ", Report: <anonymous>,",
                GenerateReport(userInfo));
        }

        [TestMethod]
        public void AnonymousUserEmtpy()
        {
            IHttpContextAccessor httpContextAccessor = new FakeHttpContextAccessor("", "", 0);
            var userInfo = new RhetosAspNetCoreIdentityUser(httpContextAccessor);
            Assert.AreEqual(
                "IsUserRecognized: False" +
                ", UserName: ClientException: This operation is not supported for anonymous user." +
                ", Workstation: " +
                ", Report: <anonymous>,",
                GenerateReport(userInfo));
        }

        private static string GenerateReport(IUserInfo userInfo)
        {
            return
                "IsUserRecognized: " + GetValueOrException(() => userInfo.IsUserRecognized) +
                ", UserName: " + GetValueOrException(() => userInfo.UserName) +
                ", Workstation: " + GetValueOrException(() => userInfo.Workstation) +
                ", Report: " + GetValueOrException(() => userInfo.Report());
        }

        private static string GetValueOrException(Func<object> getter)
        {
            try
            {
                return getter()?.ToString();
            }
            catch (Exception e)
            {
                return $"{e.GetType().Name}: {e.Message}";
            }
        }
    }
}
