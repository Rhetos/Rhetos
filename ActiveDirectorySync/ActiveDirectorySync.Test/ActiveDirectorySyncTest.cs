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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActiveDirectorySync.Test.Helpers;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;

namespace ActiveDirectorySync.Test
{
    [TestClass]
    public class ActiveDirectorySyncTest
    {
        [TestMethod]
        public void TestMock()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };

                var ws = container.Resolve<Rhetos.Security.IWindowsSecurity>();
                Assert.AreEqual("r1, r12", TestUtility.DumpSorted(ws.GetIdentityMembership(u1.Name)), "u1 active directory groups");
            }
        }

        private string ReportMembership(MockWindowsSecutiryRhetosContainer container)
        {
            var membership = container.Resolve<GenericRepository<Common.Queryable.Common_PrincipalHasRole>>();
            return TestUtility.DumpSorted(membership.Query(), phr => phr.Principal.Name + "-" + phr.Role.Name);
        }

        [TestMethod]
        public void ComputeOnInsertPrincipal()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");

                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var u3 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u3" };
                var u5 = new Common.Principal { ID = Guid.NewGuid(), Name = "u5" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };
                var r25 = new Common.Role { ID = Guid.NewGuid(), Name = "r25" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                Assert.AreEqual(@"OS\r1, OS\r2, r25", TestUtility.DumpSorted(roles.Query(), role => role.Name), "roles created");

                principals.Insert(u1, u2, u3, u5);
                Assert.AreEqual(@"OS\u1, OS\u2, OS\u3, u5", TestUtility.DumpSorted(principals.Query(), principal => principal.Name), "principals created");

                // Recompute membership on insert domain users:

                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2", ReportMembership(container), "auto-membership on insert");

                // Inserting non-domain users and roles:

                membership.Insert(new[] {
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } },
                    checkUserPermissions: true);
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2, OS\u2-r25, u5-r25", ReportMembership(container), "non-domain users and roles");
            }
        }

        [TestMethod]
        public void ComputeOnUpdatePrincipal()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");

                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var u3 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u3" };
                var u5 = new Common.Principal { ID = Guid.NewGuid(), Name = "u5" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };
                var r25 = new Common.Role { ID = Guid.NewGuid(), Name = "r25" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Recompute membership on update principal (domain users only):

                u2.Name = "u2x";
                principals.Update(u2);
                Assert.AreEqual(@"OS\u1-OS\r1, u2x-OS\r2, u2x-r25, u5-r25", ReportMembership(container), "auto-membership on update ignore non-domain users");
                u2.Name = System.Environment.UserDomainName + @"\" + "u2x";
                principals.Update(u2);
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2x-r25, u5-r25", ReportMembership(container), "auto-membership on update domain users");
                u2.Name = System.Environment.UserDomainName + @"\" + "u2";
                principals.Update(u2);
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2, OS\u2-r25, u5-r25", ReportMembership(container), "auto-membership on update domain users 2");
            }
        }

        [TestMethod]
        public void CommonFilters()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");

                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var u3 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u3" };
                var u5 = new Common.Principal { ID = Guid.NewGuid(), Name = "u5" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };
                var r25 = new Common.Role { ID = Guid.NewGuid(), Name = "r25" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = repository.Common.PrincipalHasRole;

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Common filters:

                var filter1 = new Common.ActiveDirectoryAllUsersParameter();
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2",
                    TestUtility.DumpSorted(membership.Query(membership.Load(filter1).Select(m => m.ID)),
                    phr => phr.Principal.Name + "-" + phr.Role.Name),
                    "filter ActiveDirectoryAllUsersParameter");
                var filter2 = new[] { u1.Name, u2.Name }.Select(name => new Common.ActiveDirectoryUserParameter { UserName = name }).ToArray();
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2",
                    TestUtility.DumpSorted(membership.Query(membership.Load(filter2).Select(m => m.ID)),
                    phr => phr.Principal.Name + "-" + phr.Role.Name),
                    "filter ActiveDirectoryUserParameter");
            }
        }

        [TestMethod]
        public void UserShouldNotUpdateDomainMembership()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");

                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = repository.Common.PrincipalHasRole;

                roles.Insert(r1, r2);
                principals.Insert(u1, u2);

                // The user should not update domain users/groups membership:

                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2", ReportMembership(container));

                var u2r2 = membership.Query(m => m.Principal.Name.Contains(@"\u2")).Single();
                membership.Delete(new[] { u2r2 }, checkUserPermissions: false);
                Assert.AreEqual(@"OS\u1-OS\r1", ReportMembership(container));

                var u1r1 = membership.Query(m => m.Principal.Name.Contains(@"\u1")).Single();
                TestUtility.ShouldFail(
                    () => membership.Delete(new[] { u1r1 }, checkUserPermissions: true),
                    @"It is not allowed to remove the user membership here, because role OS\r1 is synchronized with an Active Directory group");
            }
        }

        [TestMethod]
        public void RecomputeMembership()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");

                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var u3 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u3" };
                var u5 = new Common.Principal { ID = Guid.NewGuid(), Name = "u5" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };
                var r25 = new Common.Role { ID = Guid.NewGuid(), Name = "r25" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = repository.Common.PrincipalHasRole;

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Recompute membership relations:

                var u1r1 = membership.Query(m => m.Principal.Name.Contains(@"\u1")).Single();
                membership.Delete(new[] { u1r1 }, checkUserPermissions: false);
                Assert.AreEqual(@"OS\u2-OS\r2, OS\u2-r25, u5-r25", ReportMembership(container), "modified membership");

                repository.Common.PrincipalHasRole.RecomputeFromActiveDirectoryPrincipalHasRole();
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2, OS\u2-r25, u5-r25", ReportMembership(container), "recomputed membership");
            }
        }

        [TestMethod]
        public void ComputeOnUpdateRole()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");
                
                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var u3 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u3" };
                var u5 = new Common.Principal { ID = Guid.NewGuid(), Name = "u5" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };
                var r25 = new Common.Role { ID = Guid.NewGuid(), Name = "r25" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Recompute membership on modified role should remove obsolete memebers:

                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2, OS\u2-r25, u5-r25", ReportMembership(container), "initial membership");

                r2.Name = System.Environment.UserDomainName + @"\" + "r2x";
                roles.Update(r2);
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-r25, u5-r25", ReportMembership(container), "recomputed membership after rename role");

                // New role members will not be added automatically, to avoid performance penalty:
                // (the membership will be added on the principal's authorization check)

                r2.Name = System.Environment.UserDomainName + @"\" + "r2";
                roles.Update(r2);
                // This is not reqested feature, this assert simply describes currently implemented behaviour:
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-r25, u5-r25", ReportMembership(container));
            }
        }

        [TestMethod]
        public void RoleInheritance()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");
                
                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var u3 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u3" };
                var u5 = new Common.Principal { ID = Guid.NewGuid(), Name = "u5" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };
                var r25 = new Common.Role { ID = Guid.NewGuid(), Name = "r25" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Modify role inheritance:

                repository.Common.RoleInheritsRole.Insert(new[] { new Common.RoleInheritsRole {
                    UsersFromID = r1.ID, PermissionsFromID = r25.ID } });

                TestUtility.ShouldFail(() => repository.Common.RoleInheritsRole.Insert(new[] { new Common.RoleInheritsRole {
                    UsersFromID = r25.ID, PermissionsFromID = r2.ID } }), "UserException",
                    "It is not allowed to add users or user groups here because this role is synchronized with an Active Directory group.",
                    "Please change the user membership on Active Directory instead.");
            }
        }

        [TestMethod]
        public void ComputeOnAuthorization()
        {
            using (var container = new MockWindowsSecutiryRhetosContainer("u1-r1 u1-r12 u2-r12 u2-r2", commitChanges: false))
            {
                container.Resolve<ISqlExecuter>()
                    .ExecuteSql("DELETE FROM Common.Principal; DELETE FROM Common.Role");
                
                // Insert test data:

                var u1 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u1" };
                var u2 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u2" };
                var u3 = new Common.Principal { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "u3" };
                var u5 = new Common.Principal { ID = Guid.NewGuid(), Name = "u5" };
                var r1 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r1" };
                var r2 = new Common.Role { ID = Guid.NewGuid(), Name = System.Environment.UserDomainName + @"\" + "r2" };
                var r25 = new Common.Role { ID = Guid.NewGuid(), Name = "r25" };

                var repository = container.Resolve<Common.DomRepository>();
                var roles = container.Resolve<GenericRepository<Common.Role>>();
                var principals = container.Resolve<GenericRepository<Common.Principal>>();
                var membership = container.Resolve<GenericRepository<Common.PrincipalHasRole>>();

                roles.Insert(r1, r2, r25);
                principals.Insert(u1, u2, u3, u5);
                membership.Insert(new[] { // Non-domain users and roles.
                    new Common.PrincipalHasRole { PrincipalID = u2.ID, RoleID = r25.ID },
                    new Common.PrincipalHasRole { PrincipalID = u5.ID, RoleID = r25.ID } });

                // Recompute membership on authorization:

                var authorizationProvider = container.Resolve<CommonAuthorizationProvider>();

                membership.Delete(membership.Load());
                Assert.AreEqual(@"", ReportMembership(container), "membership deleted");

                authorizationProvider.GetUsersRoles(u1);
                Assert.AreEqual(@"OS\u1-OS\r1", ReportMembership(container), "membership recomputed on authorization u1");

                authorizationProvider.GetUsersRoles(u2);
                Assert.AreEqual(@"OS\u1-OS\r1, OS\u2-OS\r2", ReportMembership(container), "membership recomputed on authorization u2");
            }
        }
    }
}
