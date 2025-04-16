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
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using System;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RhetosTestContainerTest
    {
        private const string TestNamePrefix = "RhetosTestContainerTest_";

        [TestMethod]
        public void CommitOnDisposeTest()
        {
            var id = Guid.NewGuid();
#pragma warning disable CS0618 // Type or member is obsolete
            using (var rhetos = new RhetosTestContainer($"{TestAppSettings.TestAppName}.dll", true))
            {
#pragma warning restore CS0618
                var repository = rhetos.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id, Name = TestNamePrefix + "e1" });
            }

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                Assert.IsTrue(repository.TestEntity.BaseEntity.Query().Any(x => x.ID == id));
            }
        }

        [TestMethod]
        public void RollbackOnDisposeTest()
        {
            var id = Guid.NewGuid();
#pragma warning disable CS0618 // Type or member is obsolete
            using (var rhetos = new RhetosTestContainer($"{TestAppSettings.TestAppName}.dll", false))
            {
#pragma warning restore CS0618
                var repository = rhetos.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id, Name = TestNamePrefix + "e2" });
            }

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                Assert.IsFalse(repository.TestEntity.BaseEntity.Query().Any(x => x.ID == id));
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var testItems = context.Repository.TestEntity.BaseEntity.Load(item => item.Name.StartsWith(TestNamePrefix));
                context.Repository.TestEntity.BaseEntity.Delete(testItems);
                scope.CommitAndClose();
            }
        }
    }
}
