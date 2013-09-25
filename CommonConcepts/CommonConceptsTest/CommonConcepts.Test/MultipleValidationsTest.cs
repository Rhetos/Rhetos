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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestMultipleLock;
using Rhetos.Utilities;
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MultipleValidationsTest
    {

        [TestMethod]
        public void UpdateLockedData()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = new Common.DomRepository(executionContext);
                var pd = repository.TestMultipleLock.PassDependency.All().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependency = pd, UserName = "test", Pass = "1.a" };

                TestUtility.ShouldFail(() => repository.TestMultipleLock.Simple.Insert(new[] { s1 }), "Password length", "Pass is too short.");
            }
        }

        [TestMethod]
        public void UpdateLockedData2()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = new Common.DomRepository(executionContext);
                var pd = repository.TestMultipleLock.PassDependency.All().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependency = pd, UserName = "test", Pass = "123467" };
                TestUtility.ShouldFail(() => repository.TestMultipleLock.Simple.Insert(new[] { s1 }), "Password contains only numeric", "Pass is not valid.");
                executionContext.NHibernateSession.Clear();
            }
        }


        [TestMethod]
        public void UpdateLockedData3()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = new Common.DomRepository(executionContext);
                var pd = repository.TestMultipleLock.PassDependency.All().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependency = pd, UserName = "test", Pass = "123467..;aaaas" };
                repository.TestMultipleLock.Simple.Insert(new[] { s1 });
                executionContext.NHibernateSession.Clear();
            }
        }


        [TestMethod]
        public void UpdateLockedData4()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = new Common.DomRepository(executionContext);
                var pd = repository.TestMultipleLock.PassDependency.All().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependency = pd, UserName = "test", Pass = "123467..;atestaaas" };
                TestUtility.ShouldFail(() => repository.TestMultipleLock.Simple.Insert(new[] { s1 }), "Pass cannot contain value of user.", "Pass cannot contain UserName.");
                executionContext.NHibernateSession.Clear();
            }
        }
    }
}
