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
using System.Text;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class CommonAuthorizationProviderTest
    {
        public static IQueryableRepository<T> InitRepos<T>(IList<T> items)
            where T : class, IEntity
        {
            return new MockRepos<T>(items);
        }

        private class MockRepos<T> : IQueryableRepository<T>
            where T : class, IEntity
        {
            IList<T> _items;
            public MockRepos(IList<T> items) { _items = items; }
            public IQueryable<T> Query() { return _items.AsQueryable(); }
            public IQueryable<T> Query(object parameter, Type parameterType)
            {
                if (parameterType == typeof(FilterAll))
                    return Query();
                throw new NotImplementedException();
            }
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

        private class MockPrincipalRoleRepos : IQueryableRepository<IPrincipalHasRole>, IRepository
        {
            IList<MockPrincipalHasRole> _items;
            public MockPrincipalRoleRepos(IList<IPrincipalHasRole> items) { _items = items.Cast<MockPrincipalHasRole>().ToList(); }
            public IQueryable<IPrincipalHasRole> Query(object parameter, Type parameterType)
            {
                {
                    var principal = parameter as IPrincipal;
                    if (principal != null)
                        return _items.AsQueryable().Where(item => item.Principal.Name == principal.Name);
                }
                throw new NotImplementedException();
            }
        }

        class MockPrincipal : IPrincipal
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
        }

        class MockPrincipalHasRole : IPrincipalHasRole
        {
            public MockPrincipalHasRole(IPrincipal principal, IRole role)
            {
                ID = Guid.NewGuid();
                Principal = principal;
                Role = role;
                PrincipalID = principal.ID;
                RoleID = role.ID;
            }
            public Guid ID { get; set; }
            public IPrincipal Principal { get; set; }
            public IRole Role { get; set; }
            public Guid? PrincipalID { get; set; }
            public Guid? RoleID { get; set; }
        }

        class MockRoleInheritsRole : IRoleInheritsRole
        {
            public MockRoleInheritsRole(IRole usersFrom, IRole permissionsFrom)
            {
                ID = Guid.NewGuid();
                UsersFrom = usersFrom;
                PermissionsFrom = permissionsFrom;
                UsersFromID = usersFrom.ID;
                PermissionsFromID = permissionsFrom.ID;
            }
            public Guid ID { get; set; }
            public IRole UsersFrom { get; set; }
            public IRole PermissionsFrom { get; set; }
            public Guid? UsersFromID { get; set; }
            public Guid? PermissionsFromID { get; set; }
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
            public MockPrincipalPermission(IPrincipal principal, ICommonClaim claim, bool? isAuthorized)
            {
                ID = Guid.NewGuid();
                Principal = principal;
                PrincipalID = principal.ID;
                Claim = claim;
                ClaimID = claim.ID;
                IsAuthorized = isAuthorized;
            }
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
                ID = new Guid((resource.ToLower() + ".///." + right.ToLower()).GetHashCode(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                ClaimResource = resource;
                ClaimRight = right;
                Active = active;
            }
            public Guid ID { get; set; }
            public string ClaimResource { get; set; }
            public string ClaimRight { get; set; }
            public bool? Active { get; set; }
        }

        private class AuthorizationContext
        {
            public List<string> Log = new List<string>();
            public ConsoleLogProvider ConsoleLogProvider;
            public AuthorizationDataLoader AuthorizationDataLoader;
            public AuthorizationDataCache AuthorizationDataCache;
            public CommonAuthorizationProvider CommonAuthorizationProvider;
        };

        private AuthorizationContext NewAuthorizationContext(IPrincipal[] principals, IRole[] roles, IPrincipalHasRole[] principalRoles, IRoleInheritsRole[] roleRoles, ICommonClaim[] commonClaims, IRolePermission[] rolePermissions, IPrincipalPermission[] principalPermissions,
            bool useCache, double authorizationCacheExpirationSeconds)
        {
            var context = new AuthorizationContext();
            context.ConsoleLogProvider = new ConsoleLogProvider((eventType, eventName, message) =>
                {
                    context.Log.Add("[" + eventType + "] " + (eventName != null ? (eventName + ": ") : "") + message());
                });
            context.AuthorizationDataLoader = new AuthorizationDataLoader(
                context.ConsoleLogProvider,
                new RhetosAppOptions(),
                new MockRepositories(principalRoles, principals, roleRoles, principalPermissions, rolePermissions, roles, commonClaims),
                new Lazy<GenericRepository<IPrincipal>>(() => new TestGenericRepository<IPrincipal, IPrincipal>(principals)));

            if (useCache)
                context.AuthorizationDataCache = new AuthorizationDataCache(
                    context.ConsoleLogProvider, 
                    new RhetosAppOptions() { AuthorizationCacheExpirationSeconds = authorizationCacheExpirationSeconds }, 
                    new Lazy<AuthorizationDataLoader>(() => context.AuthorizationDataLoader));
            else
                context.AuthorizationDataCache = null;

            context.CommonAuthorizationProvider = new CommonAuthorizationProvider(context.ConsoleLogProvider,
                (IAuthorizationData)context.AuthorizationDataCache ?? context.AuthorizationDataLoader);
            return context;
        }

        private string ReportCacheMisses(List<string> log)
        {
            const string pattern = "AuthorizationDataCache: Cache miss: AuthorizationDataCache.";
            return string.Join("\r\n",
                log.Select(line => new { line, patternIndex = line.IndexOf(pattern) })
                    .Where(lineInfo => lineInfo.patternIndex >= 0)
                    .Select(lineInfo => lineInfo.line.Substring(lineInfo.patternIndex + pattern.Length)));
        }

        [TestMethod]
        public void SimpleTest_Reader()
        {
            var log = SimpleTest(false, 0);
            Assert.AreEqual("", ReportCacheMisses(log));
        }

        [TestMethod]
        public void SimpleTest_Cache()
        {
            var expiration = (double)60 * 60 * 24 * 365; // Very long expiration time for simpler debugging.

            AuthorizationDataCache.ClearCache();

            var log1 = SimpleTest(true, expiration);
            Assert.AreEqual(@"Principal.pr0.
PrincipalRoles.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
SystemRoles.
RoleRoles.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RoleRoles.44495e07-8d14-4db9-bd79-c0c3e8407feb.
Claims.
PrincipalPermissions.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.44495e07-8d14-4db9-bd79-c0c3e8407feb.
Roles.
Principal.pr1.
PrincipalRoles.pr1.22295e07-8d14-4db9-bd79-c0c3e8407feb.
RoleRoles.55595e07-8d14-4db9-bd79-c0c3e8407feb.
PrincipalPermissions.pr1.22295e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.55595e07-8d14-4db9-bd79-c0c3e8407feb.", ReportCacheMisses(log1));

            Console.WriteLine("Reusing cache");

            var log2 = SimpleTest(true, expiration);
            Assert.AreEqual("", ReportCacheMisses(log2));
        }

        public List<string> SimpleTest(bool useCache, double authorizationCacheExpirationSeconds)
        {
            var principals = new IPrincipal[] {
                new MockPrincipal { ID = new Guid("11195e07-8d14-4db9-bd79-c0c3e8407feb"), Name = "pr0" },
                new MockPrincipal { ID = new Guid("22295e07-8d14-4db9-bd79-c0c3e8407feb"), Name = "pr1" } };

            var roles = new IRole[] {
                new MockRole { ID = new Guid("33395e07-8d14-4db9-bd79-c0c3e8407feb"), Name = "r0" },
                new MockRole { ID = new Guid("44495e07-8d14-4db9-bd79-c0c3e8407feb"), Name = "r1" },
                new MockRole { ID = new Guid("55595e07-8d14-4db9-bd79-c0c3e8407feb"), Name = "r2" }};

            var principalRoles = new IPrincipalHasRole[] {
                new MockPrincipalHasRole(principals[0], roles[0]),
                new MockPrincipalHasRole(principals[1], roles[1]),
                new MockPrincipalHasRole(principals[1], roles[2]) };

            var roleRoles = new IRoleInheritsRole[] {
                new MockRoleInheritsRole(usersFrom: roles[0], permissionsFrom: roles[1]) };

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
                new MockPrincipalPermission(principals[0], commonClaims[5], true),
                new MockPrincipalPermission(principals[0], commonClaims[6], false),
            };

            var authorizationContext = NewAuthorizationContext(principals, roles, principalRoles, roleRoles, commonClaims, rolePermissions, principalPermissions,
                useCache, authorizationCacheExpirationSeconds);
            var provider = authorizationContext.CommonAuthorizationProvider;

            Assert.AreEqual("True, True, False, False, False, True, False, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, True, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));

            return authorizationContext.Log;
        }

        [TestMethod]
        public void SimilarClaimsTestWithReader()
        {
            var log = SimilarClaimsTest(false, 0);
            Assert.AreEqual("", ReportCacheMisses(log));
        }

        [TestMethod]
        public void SimilarClaimsTestWithCache()
        {
            var expiration = (double)60 * 60 * 24 * 365; // Very long expiration time for simpler debugging.

            AuthorizationDataCache.ClearCache();

            var log1 = SimilarClaimsTest(true, expiration);
            Assert.AreEqual(@"Principal.pr0.
PrincipalRoles.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
SystemRoles.
RoleRoles.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RoleRoles.44495e07-8d14-4db9-bd79-c0c3e8407feb.
Claims.
PrincipalPermissions.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.44495e07-8d14-4db9-bd79-c0c3e8407feb.
Roles.
Principal.pr1.
PrincipalRoles.pr1.22295e07-8d14-4db9-bd79-c0c3e8407feb.
PrincipalPermissions.pr1.22295e07-8d14-4db9-bd79-c0c3e8407feb.", ReportCacheMisses(log1));

            Console.WriteLine("Reusing cache");

            var log2 = SimilarClaimsTest(true, expiration);
            Assert.AreEqual("", ReportCacheMisses(log2));
        }

        public List<string> SimilarClaimsTest(bool useCache, double authorizationCacheExpirationSeconds)
        {
            var principals = new IPrincipal[] {
                new MockPrincipal { ID = new Guid("11195E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "pr0" },
                new MockPrincipal { ID = new Guid("22295E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "pr1" } };

            var roles = new IRole[] {
                new MockRole { ID = new Guid("33395E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "r0" },
                new MockRole { ID = new Guid("44495E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "r1" } };

            var principalRoles = new IPrincipalHasRole[] {
                new MockPrincipalHasRole(principals[0], roles[0]),
                new MockPrincipalHasRole(principals[1], roles[1]) };

            var roleRoles = new IRoleInheritsRole[] {
                new MockRoleInheritsRole(usersFrom: roles[0], permissionsFrom: roles[1]) };

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

            var authorizationContext = NewAuthorizationContext(principals, roles, principalRoles, roleRoles, commonClaims, rolePermissions, principalPermissions,
                useCache, authorizationCacheExpirationSeconds);
            var provider = authorizationContext.CommonAuthorizationProvider;

            Assert.AreEqual("True, True, True, False, True, False, True, True, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, False, False, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));

            return authorizationContext.Log;
        }

        [TestMethod]
        public void ClearCachePrincipalsRoles()
        {
            var expiration = (double)60 * 60 * 24 * 365; // Very long expiration time for simpler debugging.

            AuthorizationDataCache.ClearCache();

            var log1 = ClearCachePrincipalsRoles_GetAuthorization(expiration);
            Assert.AreEqual(@"Principal.pr0.
PrincipalRoles.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
SystemRoles.
RoleRoles.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RoleRoles.44495e07-8d14-4db9-bd79-c0c3e8407feb.
RoleRoles.55595e07-8d14-4db9-bd79-c0c3e8407feb.
Claims.
PrincipalPermissions.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.55595e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.44495e07-8d14-4db9-bd79-c0c3e8407feb.
Roles.
Principal.pr1.
PrincipalRoles.pr1.22295e07-8d14-4db9-bd79-c0c3e8407feb.
PrincipalPermissions.pr1.22295e07-8d14-4db9-bd79-c0c3e8407feb.", ReportCacheMisses(log1));

            Console.WriteLine("Reusing some parts of cache.");

            var log2 = ClearCachePrincipalsRoles_GetAuthorization(expiration);
            Assert.AreEqual(@"Principal.pr0.
PrincipalRoles.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
RoleRoles.33395e07-8d14-4db9-bd79-c0c3e8407feb.
PrincipalPermissions.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.33395e07-8d14-4db9-bd79-c0c3e8407feb.
Roles.", ReportCacheMisses(log2));

            var log3 = ClearCachePrincipalsRoles_GetAuthorization(expiration, editSystemRole: true);
            Assert.AreEqual(@"Principal.pr0.
PrincipalRoles.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
SystemRoles.
RoleRoles.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RoleRoles.55595e07-8d14-4db9-bd79-c0c3e8407feb.
PrincipalPermissions.pr0.11195e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.33395e07-8d14-4db9-bd79-c0c3e8407feb.
RolePermissions.55595e07-8d14-4db9-bd79-c0c3e8407feb.
Roles.", ReportCacheMisses(log3));
        }

        public List<string> ClearCachePrincipalsRoles_GetAuthorization(double authorizationCacheExpirationSeconds, bool editSystemRole = false)
        {
            var principals = new IPrincipal[] {
                new MockPrincipal { ID = new Guid("11195E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "pr0" },
                new MockPrincipal { ID = new Guid("22295E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "pr1" } };

            var roles = new IRole[] {
                new MockRole { ID = new Guid("33395E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "r0" },
                new MockRole { ID = new Guid("44495E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = "r1" },
                new MockRole { ID = new Guid("55595E07-8D14-4DB9-BD79-C0C3E8407FEB"), Name = SystemRole.AllPrincipals.ToString() } };

            var principalRoles = new IPrincipalHasRole[] {
                new MockPrincipalHasRole(principals[0], roles[0]),
                new MockPrincipalHasRole(principals[1], roles[1]) };

            var roleRoles = new IRoleInheritsRole[] {
                new MockRoleInheritsRole(usersFrom: roles[0], permissionsFrom: roles[1]) };

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
                new MockPrincipalPermission(principals[0], commonClaims[5], true),
                new MockPrincipalPermission(principals[0], commonClaims[6], false),
            };

            var authorizationContext = NewAuthorizationContext(principals, roles, principalRoles, roleRoles, commonClaims, rolePermissions, principalPermissions,
                true, authorizationCacheExpirationSeconds);

            var cache = authorizationContext.AuthorizationDataCache;
            cache.ClearCachePrincipals(new[] { principals[0] });
            cache.ClearCacheRoles(new[] { roles[0].ID });
            if (editSystemRole)
                cache.ClearCacheRoles(new[] { roles[2].ID });

            var provider = authorizationContext.CommonAuthorizationProvider;
            Assert.AreEqual("True, True, False, False, False, True, False, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, True, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));

            return authorizationContext.Log;
        }
    }
}
