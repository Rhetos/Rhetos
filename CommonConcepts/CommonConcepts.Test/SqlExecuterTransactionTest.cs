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
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TestAction.Repositories;

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
                var sqlTransactionBatches = scope.Resolve<ISqlTransactionBatches>();
                var unitOfWorkFactory = scope.Resolve<IUnitOfWorkFactory>();

                // Initial empty state:
                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));

                // Write using SqlExecuter in it's own transaction, unrelated to the main scope's transaction that will be rolled back (by default).
                using (var scope2 = unitOfWorkFactory.CreateScope())
                {
                    var scope2SqlExecuter = scope2.Resolve<ISqlExecuter>();
                    scope2SqlExecuter.ExecuteSql(new[]
                        {
                            "DELETE FROM TestEntity.BaseEntity",
                            $"INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT {SqlUtility.QuoteGuid(ids[0])}, 'e0'"
                        });
                    scope2.CommitAndClose();
                }

                Assert.AreEqual("e0", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Load(ids), item => item.Name));

                // Write using object model persistence transaction, read using SqlExecuter.ExecuteReader in same transaction.
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = ids[1], Name = "e1" });

                var sqlReport = new List<string>();
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteReader(
                    $"SELECT Name FROM TestEntity.BaseEntity WHERE ID IN ({SqlUtility.QuoteGuid(ids[0])}, {SqlUtility.QuoteGuid(ids[1])})",
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

        [TestMethod]
        public void TestAction_OutOfTransaction()
        {
            Guid id = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(
                () =>
                {
                    using (var scope = TestScope.Create())
                    {
                        var repository = scope.Resolve<Common.DomRepository>();
                        var testParameter = new TestAction.OutOfTransaction { ItemId = id };
                        repository.TestAction.OutOfTransaction.Execute(testParameter);
                    }
                },
                OutOfTransaction_Repository.TestName);

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var items = repository.Common.LogReader
                    .Query(log => log.ItemId == id)
                    .Select(log => log.Description)
                    .ToList();

                Assert.AreEqual("2", TestUtility.DumpSorted(items));
            }
        }

        [TestMethod]
        public void TestAction_SeparateTransactionCommitted()
        {
            Guid id = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(
                () =>
                {
                    using (var scope = TestScope.Create())
                    {
                        var repository = scope.Resolve<Common.DomRepository>();

                        var testParameter = new TestAction.SeparateTransaction
                        {
                            ItemId = id,
                            ThrowExceptionInInnerScope = false
                        };

                        repository.TestAction.SeparateTransaction.Execute(testParameter);
                    }
                },
                SeparateTransaction_Repository.TestName);

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var items = repository.Common.LogReader
                    .Query(log => log.ItemId == id)
                    .Select(log => log.Description)
                    .ToList();

                Assert.AreEqual("2", TestUtility.DumpSorted(items));
            }
        }

        [TestMethod]
        public void TestAction_SeparateTransactionRollback()
        {
            Guid id = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(
                () =>
                {
                    using (var scope = TestScope.Create())
                    {
                        var repository = scope.Resolve<Common.DomRepository>();

                        var testParameter = new TestAction.SeparateTransaction
                        {
                            ItemId = id,
                            ThrowExceptionInInnerScope = true
                        };

                        repository.TestAction.SeparateTransaction.Execute(testParameter);
                    }
                },
                SeparateTransaction_Repository.TestName);

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var items = repository.Common.LogReader
                    .Query(log => log.ItemId == id)
                    .Select(log => log.Description)
                    .ToList();

                Assert.AreEqual("", TestUtility.DumpSorted(items));
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var scope = TestScope.Create())
            {
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSqlInterpolated(
                    $"DELETE FROM Common.Log WHERE Action = {OutOfTransaction_Repository.TestName} AND TableName IS NULL");
                sqlExecuter.ExecuteSqlInterpolated(
                    $"DELETE FROM Common.Log WHERE Action = {SeparateTransaction_Repository.TestName} AND TableName IS NULL");
                scope.CommitAndClose();
            }
        }
    }
}
