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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ActionTest
    {
        [TestMethod]
        public void InsertAndThrowException()
        {
            var item1ID = Guid.NewGuid();
            var item2ID = Guid.NewGuid();
            using (var container = new RhetosTestContainer(true))
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestAction.ToInsert.Insert(new TestAction.ToInsert { ID = item1ID });

                var exception = TestUtility.ShouldFail<ApplicationException>(
                    () => repository.TestAction.InsertAndThrowException.Execute(new TestAction.InsertAndThrowException { Message = "abcd", ItmemID = item2ID }),
                    "abcd");
                var exceptionOrigin = exception.StackTrace.Substring(0, exception.StackTrace.IndexOf(Environment.NewLine));
                Assert.IsTrue(exceptionOrigin.Contains("InsertAndThrowException_Repository.<Execute>b__"));
            }

            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var toInsertEntityCount = repository.TestAction.ToInsert.Query(x => x.ID == item1ID || x.ID == item2ID).Count();
                Assert.AreEqual(0, toInsertEntityCount);
            }
        }

        [TestMethod]
        public void UseExecutionContext()
        {
            using (var container = new RhetosTestContainer())
            {
                var executionContext = container.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(container.Resolve<IUserInfo>().UserName.Length > 0);
                var repository = container.Resolve<Common.DomRepository>();
                TestUtility.ShouldFail(
                    () => repository.TestAction.UEC.Execute(new TestAction.UEC { }),
                    "User " + container.Resolve<IUserInfo>().UserName);
            }
        }

        [TestMethod]
        public void UseObjectsWithCalculatedExtension()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestAction.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                var itemA = new TestAction.Simple { Name = "testA" };
                var itemB = new TestAction.Simple { Name = "testB" };
                repository.TestAction.Simple.Insert(new[] { itemA, itemB });
                repository.TestAction.RemoveAFromAllSimpleEntities.Execute(new TestAction.RemoveAFromAllSimpleEntities { });

                repository.TestAction.Simple.Insert(new[] { new TestAction.Simple { Name = "testA" } });
                repository.TestAction.RemoveAFromAllSimpleEntities.Execute(new TestAction.RemoveAFromAllSimpleEntities { });
            }
        }

        [TestMethod]
        public void BeforeAction()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var username = container.Resolve<IUserInfo>().UserName;

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestAction.TestBefore.Execute(new TestAction.TestBefore { S = "abc" }),
                    "[Test] abc X " + username);
            }
        }
    }
}
