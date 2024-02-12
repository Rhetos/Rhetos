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
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DataStructureExtendsTest
    {
        [TestMethod]
        public void QueryableExtensionHasBase()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var secondDescription = repository.TestExtension.SqlQueryableExtenson1.Query().Where(item => item.Base.i == 2).Select(item => item.info).Single();
                Assert.AreEqual("2-b", secondDescription);
            }
        }

        [TestMethod]
        public void TableConstraints()
        {
            using (var scope = TestScope.Create())
            {
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                var repository = scope.Resolve<Common.DomRepository>();

                sqlExecuter.ExecuteSql(new[]
                    {
                        @"DELETE FROM TestExtension.EntityExtension1",
                        @"DELETE FROM TestExtension.Old1",
                        @"INSERT INTO TestExtension.Old1 (ID, i, s) SELECT ID = '5D089327-97EF-418D-A7DF-783D3873A5B4', i = 1, s = 'a'",
                        @"INSERT INTO TestExtension.Old1 (ID, i, s) SELECT ID = 'DB97EA5F-FB8C-408F-B35B-AD6642C593D7', i = 2, s = 'b'",
                        @"INSERT INTO TestExtension.EntityExtension1 (ID, info) SELECT ID = '5D089327-97EF-418D-A7DF-783D3873A5B4', info = '1-a'",
                        @"INSERT INTO TestExtension.EntityExtension1 (ID, info) SELECT ID = 'DB97EA5F-FB8C-408F-B35B-AD6642C593D7', info = '2-b'"
                    });

                // Test querying:
                var secondDescription = repository.TestExtension.EntityExtension1.Query().Where(item => item.Base.i == 2).Select(item => item.info).Single();
                Assert.AreEqual("2-b", secondDescription);

                // Test FK:
                TestUtility.ShouldFail(
                    () => sqlExecuter.ExecuteSql(@"INSERT INTO TestExtension.EntityExtension1 (ID, info) SELECT ID = NEWID(), info = '3-c'"),
                     "Old1", "The INSERT statement conflicted with the FOREIGN KEY constraint");

                // Test cascade delete in database:
                // (the release should ship with "CommonConcepts.Legacy.CascadeDeleteInDatabase" set to "False")
                TestUtility.ShouldFail(
                    () => sqlExecuter.ExecuteSql(@"DELETE FROM TestExtension.Old1 WHERE i = 2"),
                    "EntityExtension1", "The DELETE statement conflicted with the REFERENCE constraint");

                Assert.AreEqual(2, repository.TestExtension.Old1.Query().Count(), "Testing the Old1 setup to avoid false-positive.");
                Assert.AreEqual(2, repository.TestExtension.EntityExtension1.Query().Count(), "Testing the EntityExtension1 setup to avoid false-positive.");
                repository.TestExtension.Legacy1.Delete(new TestExtension.Legacy1 { ID = new Guid("DB97EA5F-FB8C-408F-B35B-AD6642C593D7") });
                Assert.AreEqual(1, repository.TestExtension.Old1.Query().Count(), "Old1 should have been deleted as it is a source for Legacy1.");
                Assert.AreEqual(1, repository.TestExtension.EntityExtension1.Query().Count(), "'On delete cascade' should delete one extension record.");
            }
        }

        [TestMethod]
        public void NavigationFromBaseToExtension_Query()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var secondBaseQuery = repository.TestExtension.SqlQueryableBase1.Query().Where(baseItem => baseItem.Extension_SqlQueryableExtenson1.info == "2-b");
                var secondBaseItem = secondBaseQuery.Single();
                Assert.AreEqual(2, secondBaseItem.i);
            }
        }

        [TestMethod]
        public void NavigationFromBaseToExtension_LazyLoadReference()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var secondBaseItem = repository.TestExtension.SqlQueryableBase1.Query().Where(baseItem => baseItem.i == 2).Single();
                Assert.AreEqual("2-b", secondBaseItem.Extension_SqlQueryableExtenson1.info);
            }
        }

        [TestMethod]
        public void MissingExtensionRecord()
        {
            using (var scope = TestScope.Create())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestExtension.SimpleBase",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id1 + "', 'b1')",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id2 + "', 'b2missing')",
                    "INSERT INTO TestExtension.SimpleExtension (ID, Name) VALUES ('" + id1 + "', 'e1')"
                });
                var repository = scope.Resolve<Common.DomRepository>();

                Assert.AreEqual("b1 e1 b1Sql, b2missing <null> <null>", TestUtility.DumpSorted(
                    repository.TestExtension.SimpleBase.Query().Select(item => new
                    {
                        baseName = item.Name,
                        simpleExt = item.Extension_SimpleExtension.Name,
                        sqlExt = item.Extension_MissingExtensionSql.Name
                    }).ToArray(),
                    data => data.baseName + " " + (data.simpleExt ?? "<null>") + " " + (data.sqlExt ?? "<null>")));

                Assert.AreEqual("b1 b1Cs", TestUtility.DumpSorted(
                    repository.TestExtension.MissingExtensionCs.Query().Select(item => item.Base.Name + " " + item.Name)));

                var b2missing = repository.TestExtension.SimpleBase.Load(new[] { id2 }).Single();
                b2missing.Name += "x";
                repository.TestExtension.SimpleBase.Update(new[] { b2missing });

                Assert.AreEqual("b1 e1 b1Sql, b2missingx <null> <null>", TestUtility.DumpSorted(
                    repository.TestExtension.SimpleBase.Query().Select(item => new
                    {
                        baseName = item.Name,
                        simpleExt = item.Extension_SimpleExtension.Name,
                        sqlExt = item.Extension_MissingExtensionSql.Name
                    }).ToArray(),
                    data => data.baseName + " " + (data.simpleExt ?? "<null>") + " " + (data.sqlExt ?? "<null>")));

                Assert.AreEqual("b1 b1Cs", TestUtility.DumpSorted(
                    repository.TestExtension.MissingExtensionCs.Query().Select(item => item.Base.Name + " " + item.Name)));
            }
        }

        [TestMethod]
        public void LazyLoadExtensions()
        {
            using (var scope = TestScope.Create())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestExtension.SimpleBase",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id1 + "', 'b1')",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id2 + "', 'b2missing')",
                    "INSERT INTO TestExtension.SimpleExtension (ID, Name) VALUES ('" + id1 + "', 'e1')"
                });
                var repository = scope.Resolve<Common.DomRepository>();

                var all = repository.TestExtension.SimpleBase.Load();
                Assert.AreEqual("b1, b2missing", TestUtility.DumpSorted(all, item => item.Name),
                    "InvalidExtension should not fail because there is no need to load those records.");

                foreach (var item in all)
                    item.Name += "X";

                repository.TestExtension.SimpleBase.Update(all);
                repository.TestExtension.SimpleBase.Insert([new TestExtension.SimpleBase { Name = "b3" }]);

                Assert.AreEqual("b1X, b2missingX, b3", TestUtility.DumpSorted(repository.TestExtension.SimpleBase.Load(), item => item.Name),
                    "InvalidExtension should not fail because there is no need to load those records.");

                Assert.IsNotNull(repository.TestExtension.SimpleBase.Query().Select(item => item.Extension_InvalidExtension.ID).First());

                var s = repository.TestExtension.SimpleBase.Query().First();

                var actualException = TestUtility.ShouldFail(() => Console.WriteLine(s.Extension_InvalidExtension.Data));

                var expectedExceptionType = typeof(Common.EntityFrameworkContext).BaseType.Namespace switch
                {
                    "Microsoft.EntityFrameworkCore" => "System.Data.Entity.Core.EntityCommandExecutionException",
                    "System.Data.Entity"            => "System.Data.Entity.Core.EntityCommandExecutionException",
                    _ => throw new NotImplementedException()
                };

                Assert.AreEqual(expectedExceptionType, actualException.GetType().ToString());
                TestUtility.AssertContains(actualException.InnerException.Message, "divide by zero");
            }
        }
    }
}
