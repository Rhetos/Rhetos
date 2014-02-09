/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using System.Collections.Generic;
using System.Linq;
using Rhetos.Utilities;
using Rhetos.TestCommon;
using Rhetos.Security;

namespace Rhetos.AspNetFormsAuth.Test
{
    [TestClass]
    public class AspNetFormsAuthorizationProviderTest
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
            public IRole Derived { get; set; }
            public IRole InheritsFrom { get; set; }
        }

        class MockPermission : IPermission
        {
            public Guid ID { get; set; }
            public IRole Role { get; set; }
            public ICommonClaim Claim { get; set; }
            public bool? IsAuthorized { get; set; }
            public Guid? RoleID { get; set; }
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
                new MockRoleInheritsRole { Derived = roles[0], InheritsFrom = roles[1] } };

            var claims = new Claim[] {
                new Claim("c0", "c0ri"),
                new Claim("c1", "c1ri"),
                new Claim("c2", "c2ri") };

            var commonClaims = claims.Select(c => new MockClaim { ClaimResource = c.Resource, ClaimRight = c.Right }).ToArray<ICommonClaim>();

            var permissions = new IPermission[] {
                new MockPermission { Role = roles[0], Claim = commonClaims[0], IsAuthorized = true },
                new MockPermission { Role = roles[1], Claim = commonClaims[1], IsAuthorized = true },
                new MockPermission { Role = roles[0], Claim = commonClaims[2], IsAuthorized = true },
                new MockPermission { Role = roles[1], Claim = commonClaims[2], IsAuthorized = false }};

            var provider = new AspNetFormsAuthorizationProvider(
                InitRepos(principals),
                InitRepos(principalRoles),
                InitRepos(roleRoles),
                InitRepos(permissions),
                new ConsoleLogProvider());

            Assert.AreEqual("True, True, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));
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
                new MockRoleInheritsRole { Derived = roles[0], InheritsFrom = roles[1] } };

            var claims = new Claim[] {
                new Claim("a.b", "c"),
                new Claim("a", "b.c"),
                new Claim("a2.b", "c"),
                new Claim("a2", "b.c"),
                new Claim("a4.b", "c"),
                new Claim("a4", "b.c"),
                new Claim("a6", "b"),
                new Claim("A6", "B"),
                new Claim("a8", "b"),
                new Claim("A8", "B")};

            var commonClaims = claims.Select(c => new MockClaim { ClaimResource = c.Resource, ClaimRight = c.Right }).ToArray<ICommonClaim>();

            var permissions = new IPermission[] {
                new MockPermission { Role = roles[0], Claim = commonClaims[0], IsAuthorized = true },
                new MockPermission { Role = roles[1], Claim = commonClaims[1], IsAuthorized = true },
                new MockPermission { Role = roles[0], Claim = commonClaims[2], IsAuthorized = true },
                new MockPermission { Role = roles[1], Claim = commonClaims[3], IsAuthorized = false },
                new MockPermission { Role = roles[0], Claim = commonClaims[4], IsAuthorized = true},
                new MockPermission { Role = roles[0], Claim = commonClaims[6], IsAuthorized = true },
                new MockPermission { Role = roles[1], Claim = commonClaims[8], IsAuthorized = true }};

            var provider = new AspNetFormsAuthorizationProvider(
                InitRepos(principals),
                InitRepos(principalRoles),
                InitRepos(roleRoles),
                InitRepos(permissions),
                new ConsoleLogProvider());

            Assert.AreEqual("True, True, True, False, True, False, True, True, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr0"), claims)));
            Assert.AreEqual("False, True, False, False, False, False, False, False, True, True", TestUtility.Dump(provider.GetAuthorizations(new TestUserInfo("pr1"), claims)));
        }
    }
}
