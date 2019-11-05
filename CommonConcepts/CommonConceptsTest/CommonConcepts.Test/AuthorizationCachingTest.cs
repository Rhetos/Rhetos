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
using Rhetos.Security;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CommonConcepts.Test
{
    [TestClass]
    public class AuthorizationCachingTest
    {
        private static readonly string UserPrefix = Environment.UserDomainName + "\\TestCaching";
        private static readonly string User1Name = UserPrefix + Guid.NewGuid();
        private static readonly string User2Name = UserPrefix + Guid.NewGuid();
        private static readonly string RolePrefix = "TestCaching";
        private static readonly string Role1Name = RolePrefix + Guid.NewGuid();
        private static readonly string Role2Name = RolePrefix + Guid.NewGuid();
        private static readonly Claim Claim1 = new Claim("Common.Log", "Read");
        private static readonly Claim Claim2 = new Claim("Common.Log", "New");
        private static readonly Claim Claim3 = new Claim("Common.Log", "Remove");

        [TestInitialize]
        public void InsertTestPermissions()
        {
            DeleteTestPermissions();
            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var r = container.Resolve<Common.DomRepository>();

                var principal1 = context.InsertPrincipalOrReadId(User1Name);
                context.InsertPrincipalOrReadId(User2Name);

                var role1 = new Common.Role { Name = Role1Name };
                var role2 = new Common.Role { Name = Role2Name };
                r.Common.Role.Insert(role1, role2);

                r.Common.PrincipalHasRole.Insert(new Common.PrincipalHasRole { PrincipalID = principal1.ID, RoleID = role1.ID });
                r.Common.RoleInheritsRole.Insert(new Common.RoleInheritsRole { UsersFromID = role1.ID, PermissionsFromID = role2.ID });

                var claim1 = r.Common.Claim.Load(c => c.ClaimResource == Claim1.Resource && c.ClaimRight == Claim1.Right).Single();
                var claim2 = r.Common.Claim.Load(c => c.ClaimResource == Claim2.Resource && c.ClaimRight == Claim2.Right).Single();
                r.Common.PrincipalPermission.Insert(new Common.PrincipalPermission { PrincipalID = principal1.ID, ClaimID = claim1.ID, IsAuthorized = true });
                r.Common.RolePermission.Insert(new Common.RolePermission { RoleID = role2.ID, ClaimID = claim2.ID, IsAuthorized = true });
            }
        }

        [TestCleanup]
        public void DeleteTestPermissions()
        {
            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                var c = container.Resolve<Common.ExecutionContext>();
                var r = container.Resolve<Common.DomRepository>();
                var oldData = c.GenericPrincipal().Query(p => p.Name.StartsWith(UserPrefix));
                c.GenericPrincipal().Delete(oldData);
                r.Common.Role.Delete(r.Common.Role.Load(p => p.Name.StartsWith(RolePrefix)));
            }
        }

        [TestMethod]
        public void SimpleCaching()
        {
            Assert.AreEqual("",
                TestPermissionsCachingOnChange(repository => { }, // No change in permissions.
                    new[] { true, true, false })); // There should be no cache misses.
        }

        [TestMethod]
        public void UpdatePrincipal()
        {
            Assert.AreEqual("Principal, PrincipalPermissions, PrincipalRoles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var currentPrincipal = context.GenericPrincipal().Load(p => p.Name == User1Name).Single();
                        currentPrincipal.Name = currentPrincipal.Name.ToUpper();
                        context.GenericPrincipal().Update(currentPrincipal);
                    },
                    new[] { true, true, false })); // Some permissions should have been cleared from cache, some remaining.
        }

        [TestMethod]
        public void UpdateOtherPrincipal()
        {
            Assert.AreEqual("",
                TestPermissionsCachingOnChange(context =>
                    {
                        var otherPrincipal = context.GenericPrincipal().Load(p => p.Name == User2Name).Single();
                        otherPrincipal.Name = otherPrincipal.Name.ToUpper();
                        context.GenericPrincipal().Update(otherPrincipal);
                    },
                    new[] { true, true, false })); // Updated principal is not used, should not affect cached permissions.
        }

        [TestMethod]
        public void DeleteInsertPrincipal()
        {
            Assert.AreEqual("Principal, PrincipalPermissions, PrincipalRoles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var currentPrincipal = context.GenericPrincipal().Load(p => p.Name == User1Name).Single();
                        context.GenericPrincipal().Delete(currentPrincipal);
                        context.GenericPrincipal().Insert(currentPrincipal);
                    },
                    new[] { false, false, false }));
        }

        [TestMethod]
        public void UpdatePrincipalPermission()
        {
            Assert.AreEqual("Principal, PrincipalPermissions, PrincipalRoles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var currentPrincipal = context.GenericPrincipal().Load(p => p.Name == User1Name).Single();
                        var claim1 = common.Claim.Load(c => c.ClaimResource == Claim1.Resource && c.ClaimRight == Claim1.Right).Single();
                        var permission1 = common.PrincipalPermission.Load(pp => pp.PrincipalID == currentPrincipal.ID && pp.ClaimID == claim1.ID).Single();
                        permission1.IsAuthorized = false;
                        common.PrincipalPermission.Update(permission1);
                    },
                    new[] { false, true, false }));
        }

        [TestMethod]
        public void DeletePrincipalPermission()
        {
            Assert.AreEqual("Principal, PrincipalPermissions, PrincipalRoles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var currentPrincipal = context.GenericPrincipal().Load(p => p.Name == User1Name).Single();
                        var claim1 = common.Claim.Load(c => c.ClaimResource == Claim1.Resource && c.ClaimRight == Claim1.Right).Single();
                        var permission1 = common.PrincipalPermission.Load(pp => pp.PrincipalID == currentPrincipal.ID && pp.ClaimID == claim1.ID).Single();
                        common.PrincipalPermission.Delete(permission1);
                    },
                    new[] { false, true, false }));
        }

        [TestMethod]
        public void InsertPrincipalPermission()
        {
            Assert.AreEqual("Principal, PrincipalPermissions, PrincipalRoles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var currentPrincipal = context.GenericPrincipal().Load(p => p.Name == User1Name).Single();
                        var claim3 = common.Claim.Load(c => c.ClaimResource == Claim3.Resource && c.ClaimRight == Claim3.Right).Single();
                        var permission3 = new Common.PrincipalPermission { PrincipalID = currentPrincipal.ID, ClaimID = claim3.ID, IsAuthorized = true };
                        common.PrincipalPermission.Insert(permission3);
                    },
                    new[] { true, true, true }));
        }

        [TestMethod]
        public void DeletePrincipalHasRole()
        {
            Assert.AreEqual("Principal, PrincipalPermissions, PrincipalRoles",
               TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var principalHasRole1 = common.PrincipalHasRole
                            .Query(phr => phr.Principal.Name == User1Name && phr.Role.Name == Role1Name)
                            .ToSimple().Single();
                        common.PrincipalHasRole.Delete(principalHasRole1);
                    },
                    new[] { true, false, false }));
        }

        [TestMethod]
        public void UpdateRole()
        {
            Assert.AreEqual("RolePermissions, RoleRoles, Roles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var role2 = common.Role.Load(r => r.Name == Role2Name).Single();
                        role2.Name = role2.Name.ToUpper();
                        common.Role.Update(role2);
                    },
                    new[] { true, true, false }));
        }

        [TestMethod]
        public void InsertUnusedRole()
        {
            Assert.AreEqual("Roles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var role3 = new Common.Role { Name = RolePrefix + Guid.NewGuid().ToString() };
                        common.Role.Insert(role3);
                    },
                    new[] { true, true, false }));
        }

        [TestMethod]
        public void InsertOrUpdateSystemRole()
        {
            AuthorizationDataCache.ClearCache();
            Assert.AreEqual("Roles, SystemRoles",
                TestPermissionsCachingOnChange(
                    context =>
                    {
                        var systemRole = context.Repository.Common.Role.Load(r => r.Name == SystemRole.AllPrincipals.ToString()).Single();
                        Console.WriteLine($"Updating system role {systemRole.ID}.");
                        systemRole.Name += "x";
                        context.Repository.Common.Role.Update(systemRole);
                    },
                    new[] { true, true, false },
                    context =>
                    {
                        if (!context.Repository.Common.Role.Query().Any(r => r.Name == SystemRole.AllPrincipals.ToString()))
                            context.Repository.Common.Role.Insert(new Common.Role { Name = SystemRole.AllPrincipals.ToString() });
                    }));
        }

        [TestMethod]
        public void InsertRoleWithPermissions()
        {
            Assert.AreEqual("RolePermissions, RolePermissions, RoleRoles, RoleRoles, Roles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var role3 = new Common.Role { Name = RolePrefix + Guid.NewGuid().ToString() };
                        common.Role.Insert(role3);
                        var claim1 = common.Claim.Load(c => c.ClaimResource == Claim1.Resource && c.ClaimRight == Claim1.Right).Single();
                        common.RolePermission.Insert(new Common.RolePermission { RoleID = role3.ID, ClaimID = claim1.ID, IsAuthorized = false });
                        var role1 = common.Role.Load(r => r.Name == Role1Name).Single();
                        common.RoleInheritsRole.Insert(new Common.RoleInheritsRole { UsersFromID = role1.ID, PermissionsFromID = role3.ID });
                    },
                    new[] { false, true, false })); // Denied permission on claim1.
        }

        [TestMethod]
        public void DeleteRoleInheritsRole()
        {
            Assert.AreEqual("RolePermissions, RoleRoles, Roles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var role1InheritsRole2 = common.RoleInheritsRole
                            .Query(rir => rir.UsersFrom.Name == Role1Name && rir.PermissionsFrom.Name == Role2Name)
                            .ToSimple().Single();
                        common.RoleInheritsRole.Delete(role1InheritsRole2);
                    },
                    new[] { true, false, false }));
        }

        [TestMethod]
        public void DeleteRolePermissions()
        {
            Assert.AreEqual("RolePermissions, RoleRoles, Roles",
                TestPermissionsCachingOnChange(context =>
                    {
                        var common = context.Repository.Common;
                        var role2Permissions = common.RolePermission.Query(rp => rp.Role.Name == Role2Name).ToSimple().Single();
                        common.RolePermission.Delete(role2Permissions);
                    },
                    new[] { true, false, false }));
        }

        public string TestPermissionsCachingOnChange(Action<Common.ExecutionContext> change, bool[] expectedPermissionsAfterChange, Action<Common.ExecutionContext> init = null)
        {
            using (var container = new RhetosTestContainer(commitChanges: false))
            {
                var log = new List<string>();
                container.AddLogMonitor(log);
                container.AddFakeUser(User1Name);
                
                var context = container.Resolve<Common.ExecutionContext>();
                var authorizationManager = container.Resolve<IAuthorizationManager>();

                Console.WriteLine("== Begin test initialization ==");
                init?.Invoke(context);
                Console.WriteLine("== End test initialization ==");

                AuthorizationDataCache.ClearCache();

                // Get user authorization:

                Assert.AreEqual(
                    TestUtility.Dump(new[] { true, true, false }),
                    TestUtility.Dump(authorizationManager.GetAuthorizations(new[] { Claim1, Claim2, Claim3 })));

                var systemRoles = Enum.GetNames(typeof(SystemRole));
                var systemRolesCount = context.Repository.Common.Role.Query(r => systemRoles.Contains(r.Name)).Count();

                var loadingFullAuthorizationData = new List<string> { "Claims", "Principal", "PrincipalPermissions", "PrincipalRoles", "RolePermissions", "RolePermissions", "RoleRoles", "RoleRoles", "Roles", "SystemRoles" };
                for (int i = 0; i < systemRolesCount; i++)
                {
                    loadingFullAuthorizationData.Add("RolePermissions");
                    loadingFullAuthorizationData.Add("RoleRoles");
                }

                Assert.AreEqual(
                    TestUtility.DumpSorted(loadingFullAuthorizationData),
                    ReportCacheMisses(log, "Initial authorization"), "Initial permission should yield cache misses. See test output log for details.");

                // Modify the permissions. Part of the cache might be invalidated:

                Console.WriteLine("== Begin test change ==");
                change(context);
                Console.WriteLine("== End test change ==");

                // Get user authorization, with partially invalidated cache:

                Assert.AreEqual(
                    TestUtility.Dump(expectedPermissionsAfterChange),
                    TestUtility.Dump(authorizationManager.GetAuthorizations(new[] { Claim1, Claim2, Claim3 })));

                return ReportCacheMisses(log, "Authorization after cache invalidation");
            }
        }

        private string ReportCacheMisses(IList<string> log, string title)
        {
            const string pattern = "AuthorizationDataCache: Cache miss: AuthorizationDataCache.";
            var cacheMisses = string.Join("\r\n",
                log.Select(line => new { line, patternIndex = line.IndexOf(pattern) })
                    .Where(lineInfo => lineInfo.patternIndex >= 0)
                    .Select(lineInfo => lineInfo.line.Substring(lineInfo.patternIndex + pattern.Length)));

            log.Clear();

            Console.WriteLine("== " + title + " ==");
            Console.WriteLine(cacheMisses);
            Console.WriteLine("==");

            var firstWordsRegex = new Regex(@"^\w+", RegexOptions.Multiline);
            var firstWords = firstWordsRegex.Matches(cacheMisses).Cast<Match>().Select(m => m.Value);
            return string.Join(", ", firstWords.OrderBy(x => x));
        }
    }
}
