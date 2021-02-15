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

using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Persistence;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class UnitOfWorkScopeTest
    {
        [TestMethod]
        public void ExplicitCommitOnDispose()
        {
            var id1 = Guid.NewGuid();

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                scope.CommitOnDispose();
            }

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void ExplicitCommitAndClose()
        {
            var id1 = Guid.NewGuid();

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                scope.CommitAndClose();
            }

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void RollbackByDefault()
        {
            var id1 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
                {
                    using (var scope = RhetosProcessHelper.CreateScope())
                    {
                        var context = scope.Resolve<Common.ExecutionContext>();
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                        throw new FrameworkException(nameof(RollbackByDefault)); // The exception that is not handled within transaction scope.
#pragma warning disable CS0162 // Unreachable code detected
                        scope.CommitOnDispose();
#pragma warning restore CS0162 // Unreachable code detected
                    }
                },
                nameof(RollbackByDefault));

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        /// <summary>
        /// This is not an intended usage of UnitOfWorkScope because CommitOnDispose should be called at the end of the using block.
        /// Here an unhandled exception incorrectly commits the transaction, but currently the framework allows it.
        /// </summary>
        [TestMethod]
        public void EarlyCommitOnDispose()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
                {
                    using (var scope = RhetosProcessHelper.CreateScope())
                    {
                        var context = scope.Resolve<Common.ExecutionContext>();
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                        scope.CommitOnDispose(); // CommitOnDispose is incorrectly placed at this position.
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id2, Name = TestNamePrefix + Guid.NewGuid() });
                        throw new FrameworkException(nameof(EarlyCommitOnDispose)); // The exception is not handled within transaction scope to discard the transaction.
                    }
                },
                nameof(EarlyCommitOnDispose));

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                // The transaction is committed because of incorrect implementation pattern above.
                var ids = new[] { id1, id2 };
                Assert.AreEqual(
                    TestUtility.DumpSorted(ids),
                    TestUtility.DumpSorted(context.Repository.TestEntity.BaseEntity.Load(ids), item => item.ID));
            }
        }

        /// <summary>
        /// This is not an intended usage of UnitOfWorkScope because CommitAndClose should be called at the end of the using block.
        /// Here an unhandled exception incorrectly commits the transaction, but currently the framework allows it.
        /// </summary>
        [TestMethod]
        public void EarlyCommitAndClose()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
                {
                    using (var scope = RhetosProcessHelper.CreateScope())
                    {
                        var context = scope.Resolve<Common.ExecutionContext>();
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                        scope.CommitAndClose(); // CommitAndClose is incorrectly placed at this position.
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id2, Name = TestNamePrefix + Guid.NewGuid() });
                    }
                },
                "disposed persistence transaction");

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                // Only operations before CommitAndClose are committed.
                var ids = new[] { id1, id2 };
                Assert.AreEqual(
                    id1.ToString(),
                    TestUtility.DumpSorted(context.Repository.TestEntity.BaseEntity.Load(ids), item => item.ID));
            }
        }

        [TestMethod]
        public void CommitAndCloseWithDiscard()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
            {
                using (var scope = RhetosProcessHelper.CreateScope())
                {
                    var context = scope.Resolve<Common.ExecutionContext>();
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                    scope.Resolve<IPersistenceTransaction>().DiscardChanges();
                    scope.CommitAndClose();
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id2, Name = TestNamePrefix + Guid.NewGuid() });
                }
            },
                "disposed persistence transaction"); // CommitAndClose should close the transaction, even if it was discarded.

            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                // CommitAndClose should rollback the transaction.
                var ids = new[] { id1, id2 };
                Assert.AreEqual(
                    "",
                    TestUtility.DumpSorted(context.Repository.TestEntity.BaseEntity.Load(ids), item => item.ID));
            }
        }

        [TestMethod]
        public void IndependentTransactions()
        {
            const int threadCount = 2;

            int initialCount;
            using (var scope = RhetosProcessHelper.CreateScope())
            {
                RhetosProcessHelper.CheckForParallelism(scope.Resolve<ISqlExecuter>(), threadCount);

                var context = scope.Resolve<Common.ExecutionContext>();
                initialCount = context.Repository.TestEntity.BaseEntity.Query().Count();
            }

            var id1 = Guid.NewGuid();

            Parallel.For(0, threadCount, thread =>
            {
                using (var scope = RhetosProcessHelper.CreateScope())
                {
                    var context = scope.Resolve<Common.ExecutionContext>();

                    Assert.AreEqual(initialCount, context.Repository.TestEntity.BaseEntity.Query().Count());
                    Thread.Sleep(100);
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() }); // Each thread uses the same ID to make sure only one thread can run this code at same time.
                    Assert.AreEqual(initialCount + 1, context.Repository.TestEntity.BaseEntity.Query().Count());
                }
            });
        }

        private const string TestNamePrefix = "RhetosUnitOfWorkScopeTest_";

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var scope = RhetosProcessHelper.CreateScope())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var testItems = context.Repository.TestEntity.BaseEntity.Load(item => item.Name.StartsWith(TestNamePrefix));
                context.Repository.TestEntity.BaseEntity.Delete(testItems);
                scope.CommitAndClose();
            }
        }
    }
}
