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

using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Persistence;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlExecuterTransactionTest
    {
        [TestMethod]
        public void SqlExecuterInPersistenceTransaction()
        {
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var sqlExecuter = scope.Resolve<ISqlExecuter>();

                // Initial empty state:
                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));

                // Write using SqlExecuter, read using object model persistence transaction:
                sqlExecuter.ExecuteSql(string.Format(
                    "INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT {0}, 'e0'",
                    SqlUtility.QuoteGuid(ids[0])));
                Assert.AreEqual("e0", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));

                // Write using object model persistence transaction, read using SqlExecuter
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = ids[1], Name = "e1" });
                var sqlReport = new List<string>();
                sqlExecuter.ExecuteReader(string.Format(
                    "SELECT Name FROM TestEntity.BaseEntity WHERE ID IN ({0}, {1})",
                    SqlUtility.QuoteGuid(ids[0]), SqlUtility.QuoteGuid(ids[1])),
                    reader => sqlReport.Add(reader.GetString(0)));
                Assert.AreEqual("e0, e1", TestUtility.DumpSorted(sqlReport));
            }

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                // Empty state after persistence transaction rollback:
                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));
            }
        }

        [TestMethod]
        public void SqlExecuterOutOfPersistenceTransaction()
        {
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var sqlExecuter = scope.Resolve<ISqlExecuter>();

                // Initial empty state:
                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));

                // Write using SqlExecuter in it's own transaction, unrelated to the main scope's transaction that will be rolled back (by default).
                sqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestEntity.BaseEntity",
                        string.Format(
                            "INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT {0}, 'e0'",
                            SqlUtility.QuoteGuid(ids[0]))
                    },
                    useTransaction: false);
                Assert.AreEqual("e0", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));

                // Write using object model persistence transaction, read using SqlExecuter.ExecuteReader in same transaction.
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = ids[1], Name = "e1" });
                var sqlReport = new List<string>();
                sqlExecuter.ExecuteReader(string.Format(
                    "SELECT Name FROM TestEntity.BaseEntity WHERE ID IN ({0}, {1})",
                    SqlUtility.QuoteGuid(ids[0]), SqlUtility.QuoteGuid(ids[1])),
                    reader => sqlReport.Add(reader.GetString(0)));
                Assert.AreEqual("e0, e1", TestUtility.DumpSorted(sqlReport));
            }

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                // After persistence transaction rollback, a record should remain from ExecuteSql with "useTransaction: false"
                Assert.AreEqual("e0", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));
            }
        }
    }
}
