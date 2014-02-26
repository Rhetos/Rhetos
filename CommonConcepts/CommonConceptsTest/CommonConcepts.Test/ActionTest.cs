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

namespace CommonConcepts.Test
{
    [TestClass]
    public class ActionTest
    {
        [TestMethod]
        public void ThrowException()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                TestUtility.ShouldFail(
                    () => repository.TestAction.ThrowException.Execute(new TestAction.ThrowException { Message = "abcd" }),
                    "abcd");
            }
        }

        [TestMethod]
        public void UseExecutionContext()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                Assert.IsTrue(executionContext.UserInfo.UserName.Length > 0);
                var repository = new Common.DomRepository(executionContext);
                TestUtility.ShouldFail(
                    () => repository.TestAction.UEC.Execute(new TestAction.UEC { }),
                    "User " + executionContext.UserInfo.UserName);
            }
        }

        [TestMethod]
        public void UseObjectsWithCalculatedExtension()
        {
            using (var executionContext = new CommonTestExecutionContext(true))
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestAction.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var itemA = new TestAction.Simple { Name = "testA" };
                var itemB = new TestAction.Simple { Name = "testB" };
                repository.TestAction.Simple.Insert(new[] { itemA, itemB });
                executionContext.NHibernateSession.Clear();
                repository.TestAction.RemoveAFromAllSimpleEntities.Execute(new TestAction.RemoveAFromAllSimpleEntities { });

                executionContext.NHibernateSession.Clear();

                repository.TestAction.Simple.Insert(new[] { new TestAction.Simple { Name = "testA" } });
                executionContext.NHibernateSession.Clear();
                repository.TestAction.RemoveAFromAllSimpleEntities.Execute(new TestAction.RemoveAFromAllSimpleEntities { });
            }
        }
    }
}
