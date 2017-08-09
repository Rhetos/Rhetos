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
using TestMultipleLock;
using Rhetos.Utilities;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MultipleValidationsTest
    {

        [TestMethod]
        public void UpdateLockedData()
        {
            using (var container = new RhetosTestContainer())
            {
                var id1 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = container.Resolve<Common.DomRepository>();
                var pd = repository.TestMultipleLock.PassDependency.Load().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependencyID = pd.ID, UserName = "test", Pass = "1.a" };

                TestUtility.ShouldFail(() => repository.TestMultipleLock.Simple.Insert(new[] { s1 }), "Pass is too short.");
            }
        }

        [TestMethod]
        public void UpdateLockedData2()
        {
            using (var container = new RhetosTestContainer())
            {
                var id1 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = container.Resolve<Common.DomRepository>();
                var pd = repository.TestMultipleLock.PassDependency.Load().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependencyID = pd.ID, UserName = "test", Pass = "123467" };
                TestUtility.ShouldFail(() => repository.TestMultipleLock.Simple.Insert(new[] { s1 }), "Pass is not valid.");
            }
        }


        [TestMethod]
        public void UpdateLockedData3()
        {
            using (var container = new RhetosTestContainer())
            {
                var id1 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = container.Resolve<Common.DomRepository>();
                var pd = repository.TestMultipleLock.PassDependency.Load().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependencyID = pd.ID, UserName = "test", Pass = "123467..;aaaas" };
                repository.TestMultipleLock.Simple.Insert(new[] { s1 });
            }
        }


        [TestMethod]
        public void UpdateLockedData4()
        {
            using (var container = new RhetosTestContainer())
            {
                var id1 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestMultipleLock.Simple",
                    "DELETE FROM TestMultipleLock.PassDependency",
                    "INSERT INTO TestMultipleLock.PassDependency (ID, MinPassLength) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 5)"});
                var repository = container.Resolve<Common.DomRepository>();
                var pd = repository.TestMultipleLock.PassDependency.Load().SingleOrDefault();
                var s1 = new TestMultipleLock.Simple { ID = Guid.NewGuid(), PassDependencyID = pd.ID, UserName = "test", Pass = "123467..;atestaaas" };
                TestUtility.ShouldFail(() => repository.TestMultipleLock.Simple.Insert(new[] { s1 }), "Pass cannot contain UserName.");
            }
        }
    }
}
