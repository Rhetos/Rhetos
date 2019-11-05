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
using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CommonConcepts.Test
{
    [TestClass]
    public class AuthorizationProvider
    {
        [TestMethod]
        public void SystemRoles()
        {
            string testUserName = "testUser_" + Guid.NewGuid().ToString();
            string allPrincipals = SystemRole.AllPrincipals.ToString();
            string anonymous = SystemRole.Anonymous.ToString();
            AuthorizationDataCache.ClearCache();

            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = context.Repository;

                // Insert test user and roles:

                var testPrincipal = context.InsertPrincipalOrReadId(testUserName);

                var roles = context.Repository.Common.Role;

                if (!roles.Query().Any(r => r.Name == allPrincipals))
                    roles.Insert(new Common.Role { Name = allPrincipals });
                var allPrincipalsRole = roles.Load(r => r.Name == allPrincipals).Single();

                if (!roles.Query().Any(r => r.Name == anonymous))
                    roles.Insert(new Common.Role { Name = anonymous });
                var anonymousRole = roles.Load(r => r.Name == anonymous).Single();

                // Test automatically assigned system roles:

                var authorizationProvider = (CommonAuthorizationProvider)container.Resolve<IAuthorizationProvider>();
                Func<IPrincipal, string[]> getUserRolesNames = principal =>
                    authorizationProvider.GetUsersRoles(principal).Select(id => repository.Common.Role.Load(new[] { id }).Single().Name).ToArray();

                Assert.AreEqual(
                    TestUtility.DumpSorted(new[] { anonymous, allPrincipals }),
                    TestUtility.DumpSorted(getUserRolesNames(testPrincipal)));

                Assert.AreEqual(
                    TestUtility.DumpSorted(new[] { anonymous }),
                    TestUtility.DumpSorted(getUserRolesNames(null)));

                // Test system roles inheritance:

                var allPrincipalsIndirectRole = new Common.Role { Name = "allPrincipalsIndirectRole" };
                var anonymousIndirectRole = new Common.Role { Name = "anonymousIndirectRole" };
                roles.Insert(allPrincipalsIndirectRole, anonymousIndirectRole);
                repository.Common.RoleInheritsRole.Insert(
                    new Common.RoleInheritsRole { UsersFromID = allPrincipalsRole.ID, PermissionsFromID = allPrincipalsIndirectRole.ID },
                    new Common.RoleInheritsRole { UsersFromID = anonymousRole.ID, PermissionsFromID = anonymousIndirectRole.ID } );

                Assert.AreEqual(
                    TestUtility.DumpSorted(new[] { anonymous, anonymousIndirectRole.Name, allPrincipals, allPrincipalsIndirectRole.Name }),
                    TestUtility.DumpSorted(getUserRolesNames(testPrincipal)));

                Assert.AreEqual(
                    TestUtility.DumpSorted(new[] { anonymous, anonymousIndirectRole.Name }),
                    TestUtility.DumpSorted(getUserRolesNames(null)));

                // Remove the system roles:
                // Considering the naming convention, it should be enough to rename the roles.

                allPrincipalsRole.Name += Guid.NewGuid().ToString();
                anonymousRole.Name += Guid.NewGuid().ToString();
                repository.Common.Role.Update(allPrincipalsRole, anonymousRole);

                Assert.AreEqual("", TestUtility.DumpSorted(getUserRolesNames(testPrincipal)));
                Assert.AreEqual("", TestUtility.DumpSorted(getUserRolesNames(null)));
            }
        }
    }
}
