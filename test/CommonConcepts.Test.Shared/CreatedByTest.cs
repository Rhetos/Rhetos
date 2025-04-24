﻿/*
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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class CreatedByTest
    {
        [TestMethod]
        public void SetCurrentUser()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var repository = scope.Resolve<Common.DomRepository>();

                string currentUserName = scope.Resolve<IUserInfo>().UserName;
                Assert.IsTrue(!string.IsNullOrWhiteSpace(currentUserName));
                var currentPrincipal = context.InsertPrincipalOrReadId(currentUserName);

                var testItem1 = new TestCreatedBy.Simple { ID = Guid.NewGuid(), Name = "test1" };
                var testItem2 = new TestCreatedBy.Simple { ID = Guid.NewGuid(), Name = "test2" };
                repository.TestCreatedBy.Simple.Insert(testItem1, testItem2);

                Assert.AreEqual(
                    "test1 " + currentUserName + ", test2 " + currentUserName,
                    TestUtility.DumpSorted(repository.TestCreatedBy.Simple
                        .Query(new[] { testItem1.ID, testItem2.ID })
                        .Select(item => item.Name + " " + item.Author.Name)));
            }
        }

        [TestMethod]
        public void LeavePredefinedUser()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var repository = scope.Resolve<Common.DomRepository>();

                string currentUserName = scope.Resolve<IUserInfo>().UserName;
                Assert.IsTrue(!string.IsNullOrWhiteSpace(currentUserName));
                var currentPrincipal = context.InsertPrincipalOrReadId(currentUserName);

                string otherUserName = "otherUser-" + Guid.NewGuid();
                var otherPrincipal = context.InsertPrincipalOrReadId(otherUserName);

                var testItem1 = new TestCreatedBy.Simple { ID = Guid.NewGuid(), Name = "test1", AuthorID = otherPrincipal.ID };
                var testItem2 = new TestCreatedBy.Simple { ID = Guid.NewGuid(), Name = "test2" };
                repository.TestCreatedBy.Simple.Insert(testItem1, testItem2);

                Assert.AreEqual(
                    "test1 " + otherUserName + ", test2 " + currentUserName,
                    TestUtility.DumpSorted(repository.TestCreatedBy.Simple
                        .Query(new[] { testItem1.ID, testItem2.ID })
                        .Select(item => item.Name + " " + item.Author.Name)));
            }
        }

        [TestMethod]
        public void WorksWithConstraints()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var repository = scope.Resolve<Common.DomRepository>();

                string currentUserName = scope.Resolve<IUserInfo>().UserName;
                Assert.IsTrue(!string.IsNullOrWhiteSpace(currentUserName));
                var currentPrincipal = context.InsertPrincipalOrReadId(currentUserName);

                var testItem1 = new TestCreatedBy.WithConstraints { ID = Guid.NewGuid(), Name = "test1" };
                var testItem2 = new TestCreatedBy.WithConstraints { ID = Guid.NewGuid(), Name = "test2" };
                repository.TestCreatedBy.WithConstraints.Insert(testItem1, testItem2);

                Assert.AreEqual(
                    "test1 " + currentUserName + ", test2 " + currentUserName,
                    TestUtility.DumpSorted(repository.TestCreatedBy.WithConstraints
                        .Query(new[] { testItem1.ID, testItem2.ID })
                        .Select(item => item.Name + " " + item.Author.Name)));
            }
        }

        [TestMethod]
        public void FailsOnDenyUserEdit()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var repository = scope.Resolve<Common.DomRepository>();

                string currentUserName = scope.Resolve<IUserInfo>().UserName;
                Assert.IsTrue(!string.IsNullOrWhiteSpace(currentUserName));
                var currentPrincipal = context.InsertPrincipalOrReadId(currentUserName);

                var testItem1 = new TestCreatedBy.WithConstraints { ID = Guid.NewGuid(), Name = "test1" };
                var testItem2 = new TestCreatedBy.WithConstraints { ID = Guid.NewGuid(), Name = "test2", AuthorID = currentPrincipal.ID };

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestCreatedBy.WithConstraints.Insert(new[] { testItem1, testItem2 }, checkUserPermissions: true),
                    "It is not allowed to directly enter", "Author");
            }
        }

        [TestMethod]
        public void SupportsAnonymous()
        {
            using (var scope = TestScope.Create(builder => builder.RegisterType<AnonUser>().As<IUserInfo>()))
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var testSimple = new TestCreatedBy.Simple();
                repository.TestCreatedBy.Simple.Insert(testSimple);
                Assert.IsNull(repository.TestCreatedBy.Simple.Load(new[] { testSimple.ID }).Single().AuthorID);

                var testWithConstraints = new TestCreatedBy.WithConstraints();
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestCreatedBy.WithConstraints.Insert(testWithConstraints),
                    "required property", "Author");
            }
        }

        private class AnonUser : IUserInfo
        {
            public bool IsUserRecognized => false;
            public string UserName => throw new InvalidOperationException("Application should not try to read UserName if " + nameof(IsUserRecognized) + " is false.");
            public string Workstation => null;
            public string Report() => "<anonymous>";
        }
    }
}
