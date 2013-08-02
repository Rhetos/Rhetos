﻿/*
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
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DataStructureExtendsTest
    {
        [TestMethod]
        public void QueryableExtenstionHasBase()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var secondDescription = repository.TestExtension.SqlQueryableExtenson1.Query().Where(item => item.Base.i == 2).Select(item => item.info).Single();
                Assert.AreEqual("2-b", secondDescription);
            }
        }

        [TestMethod]
        public void TableConstraints()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        @"DELETE FROM TestExtension.EntityExtension1",
                        @"DELETE FROM TestExtension.Old1",
                        @"INSERT INTO TestExtension.Old1 (ID, i, s) SELECT ID = '5D089327-97EF-418D-A7DF-783D3873A5B4', i = 1, s = 'a'",
                        @"INSERT INTO TestExtension.Old1 (ID, i, s) SELECT ID = 'DB97EA5F-FB8C-408F-B35B-AD6642C593D7', i = 2, s = 'b'",
                        @"INSERT INTO TestExtension.EntityExtension1 (ID, info) SELECT ID = '5D089327-97EF-418D-A7DF-783D3873A5B4', info = '1-a'",
                        @"INSERT INTO TestExtension.EntityExtension1 (ID, info) SELECT ID = 'DB97EA5F-FB8C-408F-B35B-AD6642C593D7', info = '2-b'"
                    });
                var repository = new Common.DomRepository(executionContext);

                // Test querying:
                var secondDescription = repository.TestExtension.EntityExtension1.Query().Where(item => item.Base.i == 2).Select(item => item.info).Single();
                Assert.AreEqual("2-b", secondDescription);

                // Test FK:
                string error = null;
                try
                {
                    executionContext.SqlExecuter.ExecuteSql(new[]
                        {
                            @"INSERT INTO TestExtension.EntityExtension1 (ID, info) SELECT ID = NEWID(), info = '3-c'"
                        });
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                TestUtility.AssertContains(error, "Old1", "Foreign key should prevent inserting extension record without base record.");

                // Test cascade delete:
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        @"DELETE FROM TestExtension.Old1 WHERE i = 2"
                    });
                Assert.AreEqual(1, repository.TestExtension.EntityExtension1.Query().Count(), "'On delete cascade' should delete one extension record.");
            }
        }

        [TestMethod]
        public void NavigationFromBaseToExtension_Query()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var secondBaseQuery = repository.TestExtension.SqlQueryableBase1.Query().Where(baseItem => baseItem.Extension_SqlQueryableExtenson1.info == "2-b");
                var secondBaseItem = secondBaseQuery.Single();
                Assert.AreEqual(2, secondBaseItem.i);
            }
        }

        [TestMethod]
        public void NavigationFromBaseToExtension_LazyLoadReference()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var secondBaseItem = repository.TestExtension.SqlQueryableBase1.Query().Where(baseItem => baseItem.i == 2).Single();
                Assert.AreEqual("2-b", secondBaseItem.Extension_SqlQueryableExtenson1.info);
            }
        }

        [TestMethod]
        public void MissingExtensionRecord()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestExtension.SimpleBase",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id1 + "', 'b1')",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id2 + "', 'b2missing')",
                    "INSERT INTO TestExtension.SimpleExtension (ID, Name) VALUES ('" + id1 + "', 'e1')"
                });
                var repository = new Common.DomRepository(executionContext);

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
            }
        }

        [TestMethod]
        public void LazyLoadExtensions()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestExtension.SimpleBase",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id1 + "', 'b1')",
                    "INSERT INTO TestExtension.SimpleBase (ID, Name) VALUES ('" + id2 + "', 'b2missing')",
                    "INSERT INTO TestExtension.SimpleExtension (ID, Name) VALUES ('" + id1 + "', 'e1')"
                });
                var repository = new Common.DomRepository(executionContext);

                var all = repository.TestExtension.SimpleBase.All();
                Assert.AreEqual("b1, b2missing", TestUtility.DumpSorted(all, item => item.Name),
                    "InvalidExtension should not fail because there is not need to load those records.");

                foreach (var item in all)
                    item.Name += "X";

                repository.TestExtension.SimpleBase.Update(all);
                repository.TestExtension.SimpleBase.Insert(new[] { new TestExtension.SimpleBase { Name = "b3" }});

                executionContext.NHibernateSession.Clear();
                Assert.AreEqual("b1X, b2missingX, b3", TestUtility.DumpSorted(repository.TestExtension.SimpleBase.All(), item => item.Name),
                    "InvalidExtension should not fail because there is no need to load those records.");

                var s = repository.TestExtension.SimpleBase.Query().First();
                Assert.IsNotNull(s.Extension_InvalidExtension.ID);
                Exception actualException = null;
                try
                {
                    Assert.IsNotNull(s.Extension_InvalidExtension.Data);
                }
                catch (Exception ex)
                {
                    actualException = ex;
                }
                Assert.IsNotNull(actualException, "Lazy loading a property that intentionally throws an exception");
                TestUtility.AssertContains(actualException.InnerException.Message, "divide by zero");
            }
        }
    }
}
