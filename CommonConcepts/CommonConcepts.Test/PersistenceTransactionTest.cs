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
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace CommonConcepts.Test
{
    [TestClass]
    public class PersistenceTransactionTest
    {
        [TestMethod]
        public void BeforeCloseAfterCloseOnDispose()
        {
            var id1 = Guid.NewGuid();
            var log = new List<string>();

            using (var scope = TestScope.Create())
            {
                var transaction = scope.Resolve<IPersistenceTransaction>();
                transaction.CommitChanges();
                transaction.BeforeClose += () => log.Add("before");
                transaction.AfterClose += () => log.Add("after");

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
            }

            Assert.AreEqual("before, after", TestUtility.Dump(log));

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void BeforeCloseAfterCloseCanceled()
        {
            var id1 = Guid.NewGuid();
            var log = new List<string>();

            using (var scope = TestScope.Create())
            {
                var transaction = scope.Resolve<IPersistenceTransaction>();
                transaction.BeforeClose += () => log.Add("before");
                transaction.AfterClose += () => log.Add("after");

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
            }

            Assert.AreEqual("", TestUtility.Dump(log));

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void BeforeCloseFailed()
        {
            var id1 = Guid.NewGuid();
            var log = new List<string>();
            var systemLog = new List<string>();
            string testName = TestNamePrefix + Guid.NewGuid();

            using (var scope = TestScope.Create(builder =>
                builder.ConfigureLogMonitor(systemLog, EventType.Trace)))
            {
                var transaction = scope.Resolve<IPersistenceTransaction>();
                transaction.BeforeClose += () => log.Add("before1");
                transaction.BeforeClose += () => throw new InvalidOperationException(testName);
                transaction.BeforeClose += () => log.Add("before2");
                transaction.AfterClose += () => log.Add("after");

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = testName });

                TestUtility.ShouldFail<InvalidOperationException>(
                    () => scope.CommitAndClose(),
                    testName);

                TestUtility.AssertContains(
                    string.Join(Environment.NewLine, systemLog),
                    new[] { "Rolling back transaction", "Closing connection" });

                TestUtility.ShouldFail<FrameworkException>(
                    () => Assert.IsNull(transaction.Connection),
                    "Trying to use the Connection property of a disposed persistence transaction.");
            }

            Assert.AreEqual("before1", TestUtility.Dump(log));

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void AfterCloseFailed()
        {
            var id1 = Guid.NewGuid();
            var log = new List<string>();
            var systemLog = new List<string>();
            string testName = TestNamePrefix + Guid.NewGuid();

            using (var scope = TestScope.Create(builder =>
                builder.ConfigureLogMonitor(systemLog, EventType.Trace)))
            {
                var transaction = scope.Resolve<IPersistenceTransaction>();
                transaction.BeforeClose += () => log.Add("before");
                transaction.AfterClose += () => log.Add("after1");
                transaction.AfterClose += () => throw new InvalidOperationException(testName);
                transaction.AfterClose += () => log.Add("after2");

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = testName });

                TestUtility.ShouldFail<InvalidOperationException>(
                    () => scope.CommitAndClose(),
                    testName);

                TestUtility.AssertNotContains(
                    string.Join(Environment.NewLine, systemLog),
                    new[] { "Rolling back transaction" });

                TestUtility.ShouldFail<FrameworkException>(
                    () => Assert.IsNull(transaction.Connection),
                    "Trying to use the Connection property of a disposed persistence transaction.");
            }

            Assert.AreEqual("before, after1", TestUtility.Dump(log));

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void FailedRollback()
        {
            var id1 = Guid.NewGuid();
            var log = new List<string>();
            var systemLog = new List<string>();
            string testName = TestNamePrefix + Guid.NewGuid();

            using (var scope = TestScope.Create(builder =>
                builder.ConfigureLogMonitor(systemLog, EventType.Trace)))
            {
                var transaction = scope.Resolve<IPersistenceTransaction>();
                transaction.BeforeClose += () => log.Add("before");
                transaction.AfterClose += () => log.Add("after");

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = testName });

                var dbTransaction = (DbTransaction)transaction.GetType().GetField("_transaction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transaction);
                dbTransaction.Rollback(); // This will cause error on commit or rollback, when IPersistenceTransaction is Disposed.

                systemLog.Clear();
            }

            Assert.AreEqual("", TestUtility.Dump(log));
            // Failure on rollback should not throw an exception, to allow other cleanup code to be executed. Also, a previously handled database connection error may have triggered the rollback.
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, systemLog),
                new[] { "This SqlTransaction has completed; it is no longer usable.", "Closing connection" });

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void FailedCleanupRollback()
        {
            var id1 = Guid.NewGuid();
            var log = new List<string>();
            var systemLog = new List<string>();
            string testName = TestNamePrefix + Guid.NewGuid();

            using (var scope = TestScope.Create(builder =>
                builder.ConfigureLogMonitor(systemLog, EventType.Trace)))
            {
                var transaction = scope.Resolve<IPersistenceTransaction>();
                transaction.BeforeClose += () => log.Add("before1");
                transaction.BeforeClose += () => throw new InvalidOperationException(testName + "-before");
                transaction.BeforeClose += () => log.Add("before2");
                transaction.AfterClose += () => log.Add("after");

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = testName });

                var dbTransaction = (DbTransaction)transaction.GetType().GetField("_transaction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transaction);
                dbTransaction.Rollback(); // This will cause error on commit or rollback, when IPersistenceTransaction is Disposed.

                systemLog.Clear();
                TestUtility.ShouldFail<InvalidOperationException>(
                    () => scope.CommitAndClose(),
                    testName + "-before");

                TestUtility.AssertContains(
                    string.Join(Environment.NewLine, systemLog),
                    new[] { "Rolling back transaction", "Closing connection" });

                TestUtility.ShouldFail<FrameworkException>(
                    () => Assert.IsNull(transaction.Connection),
                    "Trying to use the Connection property of a disposed persistence transaction.");
            }

            Assert.AreEqual("before1", TestUtility.Dump(log));
            // Failure on rollback should not throw an exception, to allow other cleanup code to be executed. Also, a previously handled database connection error may have triggered the rollback.
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void FailedCommit()
        {
            var id1 = Guid.NewGuid();
            var log = new List<string>();
            var systemLog = new List<string>();
            string testName = TestNamePrefix + Guid.NewGuid();

            using (var scope = TestScope.Create(builder =>
                builder.ConfigureLogMonitor(systemLog, EventType.Trace)))
            {
                var transaction = scope.Resolve<IPersistenceTransaction>();
                transaction.BeforeClose += () => log.Add("before");
                transaction.AfterClose += () => log.Add("after");

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = testName });

                var dbTransaction = (DbTransaction)transaction.GetType().GetField("_transaction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(transaction);
                dbTransaction.Rollback(); // This will cause error on commit or rollback, when IPersistenceTransaction is Disposed.

                systemLog.Clear();

                TestUtility.ShouldFail(
                    () => scope.CommitAndClose(),
                    "This SqlTransaction has completed; it is no longer usable.");

                TestUtility.AssertContains(
                    string.Join(Environment.NewLine, systemLog),
                    new[] { "Closing connection" });

                TestUtility.ShouldFail<FrameworkException>(
                    () => Assert.IsNull(transaction.Connection),
                    "Trying to use the Connection property of a disposed persistence transaction.");
            }

            Assert.AreEqual("before", TestUtility.Dump(log));

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void OriginalErrorReportedOnDispose()
        {
            // This behavior is specifically needed for ApplicationInitialization.ExecuteInitializer method,
            // because it was a common issue on deployment: if an initializer error was related to the database
            // connection, it would not be reported. Instead, the deployment tool would report "failed rollback"
            // exception, that was thrown on IPersistenceTransaction.Dispose().

            var log = new List<string>();

            TestUtility.ShouldFail<FrameworkException>(
                () =>
                {
                    using (var scope = TestScope.Create(builder => builder.ConfigureLogMonitor(log)))
                    {
                        var sqlExecuter = scope.Resolve<ISqlExecuter>();
                        var tran = scope.Resolve<IPersistenceTransaction>();

                        // Opening the transaction.
                        sqlExecuter.ExecuteSql("PRINT 1");

                        // Causing unexpected transaction state, that will result with an error later when IPersistenceTransaction is disposed and it tries to rollback the transaction.
                        ((DbTransaction)tran.GetType().GetField("_transaction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tran)).Rollback();

                        log.Clear();
                        // The "original" error. It is expected to be reported, even if rollback fails after this error.
                        sqlExecuter.ExecuteSql("RAISERROR('ErrorReportingOnDispose-TestError', 16, 10)");
                    }
                },
                // When disposing the scope, the transaction rollback will fail, but that error should not "override" the original exception.
                // The original exception should be reported.
                "ErrorReportingOnDispose-TestError");

            // Making sure that the rollback really failed:
            TestUtility.AssertContains(
                string.Join(Environment.NewLine, log),
                new[] {
                    "Error on rollback, it can be safely ignored.",
                    "InvalidOperationException: This SqlTransaction has completed; it is no longer usable."
                });
        }

        private const string TestNamePrefix = "RhetosPersistenceTransactionTest_";

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
