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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Security;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class CommonAuthorizationProviderTest
    {
        private Lazy<IQueryableRepository<T>> InitRepos<T>(IList<T> items)
        {
            return new Lazy<IQueryableRepository<T>>(() => new MockRepos<T>(items));
        }

        private class MockRepos<T> : IQueryableRepository<T>
        {
            IList<T> _items;
            public MockRepos(IList<T> items) { _items = items; }
            public IQueryable<T> Query() { return _items.AsQueryable(); }
        }

        private class MockRepositories : INamedPlugins<IRepository>
        {
            MockPrincipalRoleRepos r1;
            
            public MockRepositories (IList<IPrincipalHasRole> items)
	        {
                r1 = new MockPrincipalRoleRepos(items);
	        }
        
            public IEnumerable<IRepository> GetPlugins(string name)
            {
 	            return new IRepository[] { r1 };
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
            public Guid ID { get; set; }
            public string ClaimResource { get; set; }
            public string ClaimRight { get; set; }
            public bool? Active { get; set; }
        }

        [TestMethod]
        public void SimpleTest()
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
            };

            var commonClaims = claims.Select(c => new MockClaim { ClaimResource = c.Resource, ClaimRight = c.Right, Active = true }).ToArray<ICommonClaim>();
            commonClaims[3].Active = false;
            commonClaims[4].Active = null;

            var rolePermissions = new IRolePermission[]
            {
                new MockRolePermission { Role = roles[0], Claim = commonClaims[0], IsAuthorized = true },
                new MockRolePermission { Role = roles[1], Claim = commonClaims[1], IsAuthorized = true }, // Also inherited to role0.
                new MockRolePermission { Role = roles[0], Claim = commonClaims[2], IsAuthorized = true },
                new MockRolePermission { Role = roles[1], Claim = commonClaims[2], IsAuthorized = false }, // Also denied to role0.
                new MockRolePermission { Role = roles[0], Claim = commonClaims[3], IsAuthorized = true }, // Inactive claim.
                new MockRolePermission { Role = roles[0], Claim = commonClaims[4], IsAuthorized = true }, // Inactive claim.
                new MockRolePermission { Role = roles[1], Claim = commonClaims[6], IsAuthorized = true },
            };

            var principalPermissions = new IPrincipalPermission[]
            {
                new MockPrincipalPermission { Principal = principals[0], Claim = commonClaims[5], IsAuthorized = true },
                new MockPrincipalPermission { Principal = principals[0], Claim = commonClaims[6], IsAuthorized = false },
            };

            var provider = new CommonAuthorizationProvider(
                new MockRepositories(principalRoles),
                InitRepos(principals),
                InitRepos(roleRoles),
                InitRepos(rolePermissions),
                InitRepos(principalPermissions),
                InitRepos(roles),
                InitRepos(commonClaims),
                new ConsoleLogProvider());

            Assert.AreEqual("True, True, False, False, False, True, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));
        }

        [TestMethod]
        public void SimilarClaimsTest()
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
                new Claim("a6", "b"),
                new Claim("A6", "B"),
                new Claim("A8", "B"),
                new Claim("a8", "b") };

            var commonClaims = claims.Select(c => new MockClaim { ClaimResource = c.Resource, ClaimRight = c.Right, Active = true }).ToArray<ICommonClaim>();

            var rolePermissions = new IRolePermission[] {
                new MockRolePermission { Role = roles[0], Claim = commonClaims[0], IsAuthorized = true },
                new MockRolePermission { Role = roles[1], Claim = commonClaims[1], IsAuthorized = true },
                new MockRolePermission { Role = roles[0], Claim = commonClaims[2], IsAuthorized = true },
                new MockRolePermission { Role = roles[1], Claim = commonClaims[3], IsAuthorized = false },
                new MockRolePermission { Role = roles[0], Claim = commonClaims[4], IsAuthorized = true},
                new MockRolePermission { Role = roles[0], Claim = commonClaims[6], IsAuthorized = true },
                new MockRolePermission { Role = roles[1], Claim = commonClaims[8], IsAuthorized = true } };

            var principalPermissions = new IPrincipalPermission[] { };

            var provider = new CommonAuthorizationProvider(
                new MockRepositories(principalRoles),
                InitRepos(principals),
                InitRepos(roleRoles),
                InitRepos(rolePermissions),
                InitRepos(principalPermissions),
                InitRepos(roles),
                InitRepos(commonClaims),
                new ConsoleLogProvider());

            Assert.AreEqual("True, True, True, False, True, False, True, True, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, False, False, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));
        }
    }
}
