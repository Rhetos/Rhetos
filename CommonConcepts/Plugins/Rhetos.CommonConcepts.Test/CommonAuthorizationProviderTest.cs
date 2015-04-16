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
using Rhetos.CommonConcepts.Test.Mocks;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Security;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class CommonAuthorizationProviderTest
    {
        public static IQueryableRepository<T> InitRepos<T>(IList<T> items)
        {
            return new MockRepos<T>(items);
        }

        private class MockRepos<T> : IQueryableRepository<T>
        {
            IList<T> _items;
            public MockRepos(IList<T> items) { _items = items; }
            public IQueryable<T> Query() { return _items.AsQueryable(); }
        }

        private class MockRepositories : INamedPlugins<IRepository>
        {
            MockPrincipalRoleRepos principalRolesRepos;
            IQueryableRepository<IPrincipal> principalsRepos;
            IQueryableRepository<IRoleInheritsRole> roleRolesRepos;
            IQueryableRepository<IPrincipalPermission> principalPermissionsRepos;
            IQueryableRepository<IRolePermission> rolePermissionsRepos;
            IQueryableRepository<IRole> rolesRepos;
            IQueryableRepository<ICommonClaim> commonClaimsRepos;

            public MockRepositories(IPrincipalHasRole[] principalRoles, IPrincipal[] principals, IRoleInheritsRole[] roleRoles, IPrincipalPermission[] principalPermissions, IRolePermission[] rolePermissions, IRole[] roles, ICommonClaim[] commonClaims)
            {
                principalRolesRepos = new MockPrincipalRoleRepos(principalRoles);
                principalsRepos = CommonAuthorizationProviderTest.InitRepos(principals);
                roleRolesRepos = InitRepos(roleRoles);
                principalPermissionsRepos = InitRepos(principalPermissions);
                rolePermissionsRepos = InitRepos(rolePermissions);
                rolesRepos = InitRepos(roles);
                commonClaimsRepos = InitRepos(commonClaims);
            }

            public IEnumerable<IRepository> GetPlugins(string name)
            {
                if (name == "Common.PrincipalHasRole") return new IRepository[] { principalRolesRepos };
                if (name == "Common.Principal") return new IRepository[] { principalsRepos };
                if (name == "Common.RoleInheritsRole") return new IRepository[] { roleRolesRepos };
                if (name == "Common.PrincipalPermission") return new IRepository[] { principalPermissionsRepos };
                if (name == "Common.RolePermission") return new IRepository[] { rolePermissionsRepos };
                if (name == "Common.Role") return new IRepository[] { rolesRepos };
                if (name == "Common.Claim") return new IRepository[] { commonClaimsRepos };

                return new IRepository[] { };
            }
        }

        private class MockPrincipalRoleRepos : IQueryableRepository<IPrincipalHasRole, IPrincipal>
        {
            IList<IPrincipalHasRole> _items;
            public MockPrincipalRoleRepos(IList<IPrincipalHasRole> items) { _items = items; }
            public IQueryable<IPrincipalHasRole> Query(IPrincipal parameter) { return _items.AsQueryable().Where(item => item.Principal.Name == parameter.Name); }
        }

        class MockPrincipal : IPrincipal
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
        }

        class MockPrincipalHasRole : IPrincipalHasRole
        {
            public Guid ID { get; set; }
            public IPrincipal Principal { get; set; }
            public IRole Role { get; set; }
            public Guid? PrincipalID { get; set; }
            public Guid? RoleID { get; set; }
        }

        class MockRoleInheritsRole : IRoleInheritsRole
        {
            public Guid ID { get; set; }
            public IRole UsersFrom { get; set; }
            public IRole PermissionsFrom { get; set; }
        }

        class MockRolePermission : IRolePermission
        {
            public MockRolePermission(IRole role, ICommonClaim claim, bool? isAuthorized)
            {
                ID = Guid.NewGuid();
                Role = role;
                RoleID = role.ID;
                Claim = claim;
                ClaimID = claim.ID;
                IsAuthorized = isAuthorized;
            }
            public Guid ID { get; set; }
            public IRole Role { get; set; }
            public ICommonClaim Claim { get; set; }
            public bool? IsAuthorized { get; set; }
            public Guid? RoleID { get; set; }
            public Guid? ClaimID { get; set; }
        }

        class MockPrincipalPermission : IPrincipalPermission
        {
            public Guid ID { get; set; }
            public IPrincipal Principal { get; set; }
            public ICommonClaim Claim { get; set; }
            public bool? IsAuthorized { get; set; }
            public Guid? PrincipalID { get; set; }
            public Guid? ClaimID { get; set; }
        }

        class MockRole : IRole
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
        }

        class MockClaim : ICommonClaim
        {
            public MockClaim(string resource, string right, bool? active)
            {
                ID = Guid.NewGuid();
                ClaimResource = resource;
                ClaimRight = right;
                Active = active;
            }
            public Guid ID { get; set; }
            public string ClaimResource { get; set; }
            public string ClaimRight { get; set; }
            public bool? Active { get; set; }
        }

        private CommonAuthorizationProvider NewCommonAuthorizationProviderWithReader(IPrincipal[] principals, IRole[] roles, IPrincipalHasRole[] principalRoles, IRoleInheritsRole[] roleRoles, ICommonClaim[] commonClaims, IRolePermission[] rolePermissions, IPrincipalPermission[] principalPermissions)
        {
            var authorizationDataReader = new AuthorizationDataLoader(
                new ConsoleLogProvider(),
                new MockRepositories(principalRoles, principals, roleRoles, principalPermissions, rolePermissions, roles, commonClaims),
                new Lazy<GenericRepository<IPrincipal>>(() => new TestGenericRepository<IPrincipal, IPrincipal>(principals)));
            var provider = new CommonAuthorizationProvider(new ConsoleLogProvider(), authorizationDataReader);
            return provider;
        }

        private CommonAuthorizationProvider NewCommonAuthorizationProviderWithCache(IPrincipal[] principals, IRole[] roles, IPrincipalHasRole[] principalRoles, IRoleInheritsRole[] roleRoles, ICommonClaim[] commonClaims, IRolePermission[] rolePermissions, IPrincipalPermission[] principalPermissions)
        {
            var authorizationDataReader = new AuthorizationDataLoader(
                new ConsoleLogProvider(),
                new MockRepositories(principalRoles, principals, roleRoles, principalPermissions, rolePermissions, roles, commonClaims),
                new Lazy<GenericRepository<IPrincipal>>(() => new TestGenericRepository<IPrincipal, IPrincipal>(principals)));
            var authorizationCache = new AuthorizationDataCache(new ConsoleLogProvider(),
                new Lazy<AuthorizationDataLoader>(() => authorizationDataReader));
            var provider = new CommonAuthorizationProvider(new ConsoleLogProvider(), authorizationCache);
            return provider;
        }

        [TestMethod]
        public void SimpleTest_Reader()
        {
            SimpleTest(NewCommonAuthorizationProviderWithReader);
        }

        [TestMethod]
        public void SimpleTest_Cache()
        {
            // Avoid loading setitng from web.config.
            var expiration = typeof(AuthorizationDataCache).GetField("_defaultExpirationSeconds", BindingFlags.Static | BindingFlags.NonPublic);
            expiration.SetValue(null, (double?)30);

            AuthorizationDataCache.ClearCache();
            SimpleTest(NewCommonAuthorizationProviderWithCache);
            Console.WriteLine("Reusing cache");
            SimpleTest(NewCommonAuthorizationProviderWithCache);
        }

        public void SimpleTest(Func<IPrincipal[], IRole[], IPrincipalHasRole[], IRoleInheritsRole[], ICommonClaim[], IRolePermission[], IPrincipalPermission[], IAuthorizationProvider> authProviderCreator)
        {
            var principals = new IPrincipal[] {
                new MockPrincipal { ID = Guid.NewGuid(), Name = "pr0" },
                new MockPrincipal { ID = Guid.NewGuid(), Name = "pr1" } };

            var roles = new IRole[] {
                new MockRole { ID = Guid.NewGuid(), Name = "r0" },
                new MockRole { ID = Guid.NewGuid(), Name = "r1" } };

            var principalRoles = new IPrincipalHasRole[] {
                new MockPrincipalHasRole { Principal = principals[0], Role = roles[0] },
                new MockPrincipalHasRole { Principal = principals[1], Role = roles[1] } };

            var roleRoles = new IRoleInheritsRole[] {
                new MockRoleInheritsRole { UsersFrom = roles[0], PermissionsFrom = roles[1] } };

            var claims = new Claim[]
            {
                new Claim("c0", "c0ri"),
                new Claim("c1", "c1ri"),
                new Claim("c2", "c2ri"),
                new Claim("c3", "c3ri"),
                new Claim("c4", "c4ri"),
                new Claim("c5", "c5ri"),
                new Claim("c6", "c6ri"),
                new Claim("c7", "c7ri"),
            };

            var commonClaims = claims
                .Where(c => c.Resource != "c7")
                .Select(c => new MockClaim(c.Resource, c.Right, true))
                .ToArray<ICommonClaim>();
            commonClaims[3].Active = false;
            commonClaims[4].Active = null;

            var rolePermissions = new IRolePermission[]
            {
                new MockRolePermission(roles[0], commonClaims[0], true),
                new MockRolePermission(roles[1], commonClaims[1], true), // Also inherited to role0.
                new MockRolePermission(roles[0], commonClaims[2], true),
                new MockRolePermission(roles[1], commonClaims[2], false), // Also denied to role0.
                new MockRolePermission(roles[0], commonClaims[3], true), // Inactive claim (active = null)
                new MockRolePermission(roles[0], commonClaims[4], true), // Inactive claim.
                new MockRolePermission(roles[1], commonClaims[6], true),
            };

            var principalPermissions = new IPrincipalPermission[]
            {
                new MockPrincipalPermission { Principal = principals[0], Claim = commonClaims[5], IsAuthorized = true },
                new MockPrincipalPermission { Principal = principals[0], Claim = commonClaims[6], IsAuthorized = false },
            };

            var provider = authProviderCreator(principals, roles, principalRoles, roleRoles, commonClaims, rolePermissions, principalPermissions);

            Assert.AreEqual("True, True, False, False, False, True, False, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, True, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));
        }

        [TestMethod]
        public void SimilarClaimsTestWithReader()
        {
            SimilarClaimsTest(NewCommonAuthorizationProviderWithReader);
        }

        [TestMethod]
        public void SimilarClaimsTestWithCache()
        {
            // Avoid loading setitng from web.config.
            var expiration = typeof(AuthorizationDataCache).GetField("_defaultExpirationSeconds", BindingFlags.Static | BindingFlags.NonPublic);
            expiration.SetValue(null, (double?)30);

            AuthorizationDataCache.ClearCache();
            SimilarClaimsTest(NewCommonAuthorizationProviderWithCache);
            Console.WriteLine("Reusing cache");
            SimilarClaimsTest(NewCommonAuthorizationProviderWithCache);
        }

        public void SimilarClaimsTest(Func<IPrincipal[], IRole[], IPrincipalHasRole[], IRoleInheritsRole[], ICommonClaim[], IRolePermission[], IPrincipalPermission[], IAuthorizationProvider> authProviderCreator)
        {
            var principals = new IPrincipal[] {
                new MockPrincipal { ID = Guid.NewGuid(), Name = "pr0" },
                new MockPrincipal { ID = Guid.NewGuid(), Name = "pr1" } };

            var roles = new IRole[] {
                new MockRole { ID = Guid.NewGuid(), Name = "r0" },
                new MockRole { ID = Guid.NewGuid(), Name = "r1" } };

            var principalRoles = new IPrincipalHasRole[] {
                new MockPrincipalHasRole { Principal = principals[0], Role = roles[0] },
                new MockPrincipalHasRole { Principal = principals[1], Role = roles[1] } };

            var roleRoles = new IRoleInheritsRole[] {
                new MockRoleInheritsRole { UsersFrom = roles[0], PermissionsFrom = roles[1] } };

            var claims = new Claim[] {
                new Claim("a.b", "c"),
                new Claim("a", "b.c"),
                new Claim("a2.b", "c"),
                new Claim("a2", "b.c"),
                new Claim("a4.b", "c"),
                new Claim("a4", "b.c"),
                new Claim("a6", "b"), // index 6, commonClaims[6]
                new Claim("A6", "B"), // index 7
                new Claim("A8", "B"), // commonClaims[7]
                new Claim("a8", "b"), // index 9
            };

            var commonClaims = claims
                .Where((claim, index) => index != 7 && index != 9)
                .Select(c => new MockClaim(c.Resource, c.Right, true))
                .ToArray<ICommonClaim>();

            var rolePermissions = new IRolePermission[]
            {
                new MockRolePermission(roles[0], commonClaims[0], true),
                new MockRolePermission(roles[1], commonClaims[1], true),
                new MockRolePermission(roles[0], commonClaims[2], true),
                new MockRolePermission(roles[1], commonClaims[3], false),
                new MockRolePermission(roles[0], commonClaims[4], true),
                new MockRolePermission(roles[0], commonClaims[6], true),
                new MockRolePermission(roles[1], commonClaims[7], true),
            };

            var principalPermissions = new IPrincipalPermission[] { };

            var provider = authProviderCreator(principals, roles, principalRoles, roleRoles, commonClaims, rolePermissions, principalPermissions);

            Assert.AreEqual("True, True, True, False, True, False, True, True, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, False, False, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));
        }
    }
}
